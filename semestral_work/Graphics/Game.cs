using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using Serilog;
using semestral_work.Config;
using semestral_work.Map;
using GLKeys = OpenTK.Windowing.GraphicsLibraryFramework.Keys;

namespace semestral_work.Graphics
{
    internal class Game : GameWindow
    {
        private ParsedMap _map;
        private Camera _camera;

        // Floor
        private int _floorVao;
        private int _floorIndexCount;

        // Walls
        private int _wallVao;
        private List<Matrix4> _wallMatrices;

        // Ceiling
        private int _ceilingVao;
        private int _ceilingIndexCount;

        // Shader & Textures
        private Shader? _shader;
        private int _textureFloor;
        private int _textureWalls;
        private int _textureCeiling;

        // Mouse-cursor lock
        private bool _mouseGrabbed = true;

        // FPS counter
        private double _accumTime;
        private int _frameCount;
        private int _fps;

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

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(Color4.Black);
            Log.Information("Window loaded and OpenGL initialized.");

            GL.Enable(EnableCap.DepthTest);

            // 1) Create floor
            (_floorVao, _floorIndexCount) = PlaneBuilder.CreatePlaneVAO(_map.Rows, _map.Columns);

            // 2) Create walls
            _wallVao = BlockBuilder.CreateBlockVAO();

            // 3) Load shaders
            string vertexPath = AppConfig.GetVertexShaderPath();
            string fragmentPath = AppConfig.GetFragmentShaderPath();
            string vertexCode = File.ReadAllText(vertexPath);
            string fragmentCode = File.ReadAllText(fragmentPath);
            _shader = new Shader(vertexCode, fragmentCode);

            // 4) Load textures
            // floor
            string floorPath = AppConfig.GetFloorTexturePath();
            _textureFloor = TextureLoader.LoadTexture(floorPath);

            // walls
            string wallPath = AppConfig.GetWallTexturePath();
            _textureWalls = TextureLoader.LoadTexture(wallPath);

            // ceiling
            string ceilingPath = AppConfig.GetCeilingTexturePath(); // new method
            _textureCeiling = TextureLoader.LoadTexture(ceilingPath);

            // Set the uniform for texture = 0
            _shader.Use();
            int texLoc = GL.GetUniformLocation(_shader.Handle, "uTexture");
            GL.Uniform1(texLoc, 0);

            // 5) Create ceiling (use old builder)
            (_ceilingVao, _ceilingIndexCount) = PlaneBuilder.CreatePlaneVAO(_map.Rows, _map.Columns);

            // 6) Generate matrices for walls
            GenerateWallMatrices();
        }

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
                        // Shift the block up by 1.5 so bottom is at y=0
                        var translation = Matrix4.CreateTranslation(x, 1.5f, z);
                        _wallMatrices.Add(translation);
                    }
                }
            }
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, e.Width, e.Height);

            _camera.screenWidth = e.Width;
            _camera.screenHeight = e.Height;
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            // FPS counter
            _accumTime += args.Time;
            _frameCount++;
            if (_accumTime >= 1.0)
            {
                _fps = _frameCount;
                _frameCount = 0;
                _accumTime = 0.0;
                Title = $"Maze Game | FPS: {_fps}";
            }

            var input = KeyboardState;
            if (input.IsKeyPressed(GLKeys.Escape))
            {
                _mouseGrabbed = !_mouseGrabbed;
                CursorState = _mouseGrabbed ? CursorState.Grabbed : CursorState.Normal;
            }

            _camera.Update(KeyboardState, MouseState, args);
        }

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

            // 1) Spotlight parameters
            var (flashPos, flashDir) = _camera.GetFlashlightParams();
            float cutoffDeg = 20f;
            float cutoffCos = MathF.Cos(MathHelper.DegreesToRadians(cutoffDeg));

            int locLightPos = GL.GetUniformLocation(_shader.Handle, "uLightPos");
            int locLightDir = GL.GetUniformLocation(_shader.Handle, "uLightDir");
            int locSpotCut = GL.GetUniformLocation(_shader.Handle, "uSpotCutoff");

            if (locLightPos >= 0) GL.Uniform3(locLightPos, flashPos);
            if (locLightDir >= 0) GL.Uniform3(locLightDir, flashDir);
            if (locSpotCut >= 0) GL.Uniform1(locSpotCut, cutoffCos);

            // 2) Matrices
            Matrix4 view = _camera.GetViewMatrix();
            Matrix4 proj = _camera.GetProjectionMatrix();

            int uModelLoc = GL.GetUniformLocation(_shader.Handle, "uModel");
            int uMVPLoc = GL.GetUniformLocation(_shader.Handle, "uMVP");

            // -- Render floor --
            Matrix4 floorModel = Matrix4.Identity;
            Matrix4 floorMVP = floorModel * view * proj;
            if (uModelLoc >= 0) GL.UniformMatrix4(uModelLoc, false, ref floorModel);
            if (uMVPLoc >= 0) GL.UniformMatrix4(uMVPLoc, false, ref floorMVP);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _textureFloor);

            GL.BindVertexArray(_floorVao);
            GL.DrawElements(PrimitiveType.Triangles, _floorIndexCount, DrawElementsType.UnsignedInt, 0);

            // -- Render ceiling --
            // we place it at y=3 (if each block is height=3)
            Matrix4 ceilingModel = Matrix4.CreateTranslation(0f, 3f, 0f);
            Matrix4 ceilingMVP = ceilingModel * view * proj;
            if (uModelLoc >= 0) GL.UniformMatrix4(uModelLoc, false, ref ceilingModel);
            if (uMVPLoc >= 0) GL.UniformMatrix4(uMVPLoc, false, ref ceilingMVP);

            GL.BindTexture(TextureTarget.Texture2D, _textureCeiling);
            GL.BindVertexArray(_ceilingVao);
            GL.DrawElements(PrimitiveType.Triangles, _ceilingIndexCount, DrawElementsType.UnsignedInt, 0);

            // -- Render walls --
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

            SwapBuffers();
        }

        protected override void OnUnload()
        {
            base.OnUnload();

            GL.DeleteVertexArray(_floorVao);
            GL.DeleteVertexArray(_wallVao);
            GL.DeleteVertexArray(_ceilingVao); 

            _shader?.Dispose();

            GL.DeleteTexture(_textureFloor);
            GL.DeleteTexture(_textureWalls);
            GL.DeleteTexture(_textureCeiling);
        }
    }
}
