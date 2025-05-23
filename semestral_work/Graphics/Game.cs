using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using Serilog;
using semestral_work.Config;
using semestral_work.Map;
using GLKeys = OpenTK.Windowing.GraphicsLibraryFramework.Keys;
using System.Windows.Forms;

namespace semestral_work.Graphics
{
    internal class Game : GameWindow
    {
        private ParsedMap _map;
        private Camera _camera;

        // Podlaha
        private int _floorVao;
        private int _floorIndexCount;

        // Zdi
        private int _wallVao;
        private List<Matrix4> _wallMatrices;

        // Strop
        private int _ceilingVao;
        private int _ceilingIndexCount;

        // Shader a textury
        private Shader? _shader;
        private int _textureFloor;
        private int _textureWalls;
        private int _textureCeiling;

        // Zamknutí kurzoru myši
        private bool _mouseGrabbed = true;

        // Počítadlo FPS
        private double _accumTime;
        private int _frameCount;
        private int _fps;

        private MinimapRenderer? _minimap;

        private Shader? _appleShader;
        private CollectableManager? _collectables;
        private int _collected;

        /// <summary>
        /// Vytvoří nové herní okno s mapou a kamerou.
        /// </summary>
        public Game(GameWindowSettings gameWindowSettings,
                    NativeWindowSettings nativeWindowSettings,
                    ParsedMap map,
                    Camera camera)
            : base(gameWindowSettings, nativeWindowSettings)
        {
            _map = map;
            _camera = camera;
            _wallMatrices = new List<Matrix4>();

            CursorState = CursorState.Grabbed;
        }

        /// <summary>
        /// Inicializace OpenGL, načtení geometrie, shaderů a textur.
        /// </summary>
        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(Color4.Black);
            Log.Information("Window loaded and OpenGL initialized.");

            GL.Enable(EnableCap.DepthTest);

            // Vytvoření podlahy
            (_floorVao, _floorIndexCount) = PlaneBuilder.CreatePlaneVAO(_map.Rows, _map.Columns);

            // Vytvoření zdí
            _wallVao = BlockBuilder.CreateBlockVAO();

            // Načtení shaderů
            string vertexPath = AppConfig.GetVertexShaderPath();
            string fragmentPath = AppConfig.GetFragmentShaderPath();
            string vertexCode = File.ReadAllText(vertexPath);
            string fragmentCode = File.ReadAllText(fragmentPath);
            _shader = new Shader(vertexCode, fragmentCode);

            // Načtení textur
            string floorPath = AppConfig.GetFloorTexturePath();
            _textureFloor = TextureLoader.LoadTexture(floorPath);

            string wallPath = AppConfig.GetWallTexturePath();
            _textureWalls = TextureLoader.LoadTexture(wallPath);

            string ceilingPath = AppConfig.GetCeilingTexturePath();
            _textureCeiling = TextureLoader.LoadTexture(ceilingPath);

            // Nastavení uniformy pro texturu = 0
            _shader.Use();
            int texLoc = GL.GetUniformLocation(_shader.Handle, "uTexture");
            GL.Uniform1(texLoc, 0);

            // Vytvoření stropu
            (_ceilingVao, _ceilingIndexCount) = PlaneBuilder.CreatePlaneVAO(_map.Rows, _map.Columns);

            // Generování transformačních matic pro zdi
            GenerateWallMatrices();

            Log.Information("Creating MinimapRenderer...");

            string miniVertexPath = AppConfig.GetMiniMapVertexShaderPath();
            string miniFragmentPath = AppConfig.GetMiniMapFragmentShaderPath();
            var miniShader = new Shader(
                 File.ReadAllText(miniVertexPath),
                 File.ReadAllText(miniFragmentPath));

            int miniSize = AppConfig.GetMiniMapSizeInPixels();
            float viewRadius = AppConfig.GetMiniMapViewRadius();
            float arrowSize = AppConfig.GetMiniMapArrowSize();

            _minimap = new MinimapRenderer(_map, _wallMatrices, miniShader, miniSize, viewRadius, arrowSize);

            Log.Information("Initializing Apple shader");

           string appleVertexPath = AppConfig.GetAppleVertexShaderPath();
            string appleFragmentPath = AppConfig.GetAppleFragmentShaderPath();
            _appleShader = new Shader(
                File.ReadAllText(appleVertexPath),
                File.ReadAllText(appleFragmentPath));

            _collectables = new CollectableManager(_map, _appleShader);
        }

        /// <summary>
        /// Vygeneruje transformační matice pro všechny zdi na mapě.
        /// </summary>
        private void GenerateWallMatrices()
        {
            _wallMatrices.Clear();
            for (int row = 0; row < _map.Rows; row++)
            {
                for (int col = 0; col < _map.Columns; col++)
                {
                    if (_map.Cells[row, col] == CellType.Wall)
                    {
                        float x = col * 2.0f + 1.0f;
                        float z = row * 2.0f + 1.0f;
                        var translation = Matrix4.CreateTranslation(x, 1.5f, z);
                        _wallMatrices.Add(translation);
                    }
                }
            }
        }

        /// <summary>
        /// Zpracování změny velikosti okna.
        /// </summary>
        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, e.Width, e.Height);

            _camera.screenWidth = e.Width;
            _camera.screenHeight = e.Height;
        }

        /// <summary>
        /// Zpracování vstupu a aktualizace logiky každý snímek.
        /// </summary>
        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            UpdateFpsCounter(args.Time);

            var input = KeyboardState;
            if (input.IsKeyPressed(GLKeys.Escape))
            {
                _mouseGrabbed = !_mouseGrabbed;
                CursorState = _mouseGrabbed ? CursorState.Grabbed : CursorState.Normal;
            }

            _camera.Update(KeyboardState, MouseState, args);

            _collectables?.Update((float)args.Time);
            _collectables?.TryCollect(_camera.position);
            _collected = _collectables?.CollectedCount ?? 0;
        }

        /// <summary>
        /// Aktualizuje počítadlo FPS a nastaví titulek okna.
        /// </summary>
        private void UpdateFpsCounter(double deltaTime)
        {
            _accumTime += deltaTime;
            _frameCount++;

            if (_accumTime >= 1.0)
            {
                _fps = _frameCount;
                _frameCount = 0;
                _accumTime = 0.0;
                Title = $"Maze Game | FPS: {_fps} | Apples: {_collected}/{_collectables?.TotalCount}";
            }
        }

        /// <summary>
        /// Vykreslování scény každý snímek.
        /// </summary>
        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            if (_shader == null)
            {
                Log.Error("Shader is NULL!");
                throw new InvalidOperationException("Shader not initialized before rendering.");
            }

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            _shader.Use();

            // Parametry světla z kamery
            var (flashPos, flashDir) = _camera.GetFlashlightParams();
            float cutoffDeg = _camera.LightCutoffDeg; 
            float cutoffCos = MathF.Cos(MathHelper.DegreesToRadians(cutoffDeg));
            float range = _camera.LightRange;

            // Získání lokací uniform
            int locLightPos = GL.GetUniformLocation(_shader.Handle, "uLightPos");
            int locLightDir = GL.GetUniformLocation(_shader.Handle, "uLightDir");
            int locSpotCut = GL.GetUniformLocation(_shader.Handle, "uSpotCutoff");
            int locLightRange = GL.GetUniformLocation(_shader.Handle, "uLightRange");

            // Aplikace dat do shaderu
            if (locLightPos >= 0) GL.Uniform3(locLightPos, flashPos);
            if (locLightDir >= 0) GL.Uniform3(locLightDir, flashDir);
            if (locSpotCut >= 0) GL.Uniform1(locSpotCut, cutoffCos);
            if (locLightRange >= 0) GL.Uniform1(locLightRange, range);

            // Matice
            Matrix4 view = _camera.GetViewMatrix();
            Matrix4 proj = _camera.GetProjectionMatrix();
            int uModelLoc = GL.GetUniformLocation(_shader.Handle, "uModel");
            int uMVPLoc = GL.GetUniformLocation(_shader.Handle, "uMVP");

            // Vykreslení podlahy
            Matrix4 floorModel = Matrix4.Identity;
            Matrix4 floorMVP = floorModel * view * proj;
            if (uModelLoc >= 0) GL.UniformMatrix4(uModelLoc, false, ref floorModel);
            if (uMVPLoc >= 0) GL.UniformMatrix4(uMVPLoc, false, ref floorMVP);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _textureFloor);

            GL.BindVertexArray(_floorVao);
            GL.DrawElements(PrimitiveType.Triangles, _floorIndexCount, DrawElementsType.UnsignedInt, 0);

            // Vykreslení stropu
            Matrix4 ceilingModel = Matrix4.CreateTranslation(0f, 3f, 0f);
            Matrix4 ceilingMVP = ceilingModel * view * proj;
            if (uModelLoc >= 0) GL.UniformMatrix4(uModelLoc, false, ref ceilingModel);
            if (uMVPLoc >= 0) GL.UniformMatrix4(uMVPLoc, false, ref ceilingMVP);

            GL.BindTexture(TextureTarget.Texture2D, _textureCeiling);
            GL.BindVertexArray(_ceilingVao);
            GL.DrawElements(PrimitiveType.Triangles, _ceilingIndexCount, DrawElementsType.UnsignedInt, 0);

            // Vykreslení zdí
            GL.BindVertexArray(_wallVao);
            GL.BindTexture(TextureTarget.Texture2D, _textureWalls);

            foreach (Matrix4 mat in _wallMatrices)
            {
                Matrix4 localMat = mat;
                Matrix4 mvp = localMat * view * proj;
                if (uModelLoc >= 0) GL.UniformMatrix4(uModelLoc, false, ref localMat);
                if (uMVPLoc >= 0) GL.UniformMatrix4(uMVPLoc, false, ref mvp);

                GL.DrawElements(PrimitiveType.Triangles, 36, DrawElementsType.UnsignedInt, 0);
            }

            float cutCos = MathF.Cos(MathHelper.DegreesToRadians(_camera.LightCutoffDeg));

            _collectables?.Render(view, proj,
                                  flashPos, flashDir,
                                  cutCos, range,
                                  _camera.position);

            //_collectables?.Render(view, proj, flashPos, flashDir, cutCos, range, _camera.position);

            _minimap!.Render(_camera, Size.X, Size.Y);

            // Výměna framebufferů
            SwapBuffers();
        }

        /// <summary>
        /// Uvolnění prostředků při zavření okna.
        /// </summary>
        protected override void OnUnload()
        {
            base.OnUnload();

            GL.DeleteVertexArray(_floorVao);
            GL.DeleteVertexArray(_wallVao);
            GL.DeleteVertexArray(_ceilingVao);

            _shader?.Dispose();
            _minimap?.Dispose();
            _appleShader?.Dispose();
            _collectables?.Dispose();

            GL.DeleteTexture(_textureFloor);
            GL.DeleteTexture(_textureWalls);
            GL.DeleteTexture(_textureCeiling);
        }
    }
}
