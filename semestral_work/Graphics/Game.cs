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

        // Пол
        private int _floorVao;
        private int _floorIndexCount;

        // Стены
        private int _wallVao;
        private List<Matrix4> _wallMatrices;

        private Shader? _shader;
        private int _textureFloor;
        private int _textureWalls;

        private bool _mouseGrabbed = true;

        // Счётчик FPS
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

            // Начинаем со скрытым курсором
            CursorState = CursorState.Grabbed;
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            GL.ClearColor(Color4.Black);
            Log.Information("Window loaded and OpenGL initialized.");

            GL.Enable(EnableCap.DepthTest);

            // 1) Создаём плоскость (пол)
            (_floorVao, _floorIndexCount) = FloorBuilder.CreateFloorVAO(_map.Rows, _map.Columns);

            // 2) Создаём VAO для стен (куб 2×3×2)
            _wallVao = BlockBuilder.CreateBlockVAO();

            // 3) Загружаем шейдер
            string vertexPath = AppConfig.GetVertexShaderPath();
            string fragmentPath = AppConfig.GetFragmentShaderPath();
            string vertexCode = File.ReadAllText(vertexPath);
            string fragmentCode = File.ReadAllText(fragmentPath);
            _shader = new Shader(vertexCode, fragmentCode);

            // 4) Загружаем текстуры
            string floorPath = AppConfig.GetFloorTexturePath();
            _textureFloor = TextureLoader.LoadTexture(floorPath);

            string wallsPath = AppConfig.GetWallTexturePath();
            _textureWalls = TextureLoader.LoadTexture(wallsPath);

            // Настраиваем uniform
            _shader.Use();
            int uTextureLocation = GL.GetUniformLocation(_shader.Handle, "uTexture");
            GL.Uniform1(uTextureLocation, 0);

            // 5) Генерируем матрицы для стен
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
            Log.Information("Window resized to {Width}x{Height}", e.Width, e.Height);

            _camera.SCREENWIDTH = e.Width;
            _camera.SCREENHEIGHT = e.Height;
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            // 1) Подсчёт FPS
            _accumTime += args.Time;
            _frameCount++;
            if (_accumTime >= 1.0)
            {
                _fps = _frameCount;
                _frameCount = 0;
                _accumTime = 0;

                // Обновляем заголовок
                Title = $"Maze Game | FPS: {_fps}";
            }

            // 2) Клавиша Esc => захват курсора
            var input = KeyboardState;
            if (input.IsKeyPressed(GLKeys.Escape))
            {
                _mouseGrabbed = !_mouseGrabbed;
                CursorState = _mouseGrabbed ? CursorState.Grabbed : CursorState.Normal;
            }

            // 3) Движение камеры
            _camera.Update(KeyboardState, MouseState, args);
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // Матрицы
            var view = _camera.GetViewMatrix();
            var projection = _camera.GetProjectionMatrix();

            // Шейдер
            _shader?.Use();
            int mvpLoc = GL.GetUniformLocation(_shader!.Handle, "uMVP");

            // (1) Рисуем пол
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _textureFloor);

            var floorModel = Matrix4.Identity;
            var floorMVP = floorModel * view * projection;
            GL.UniformMatrix4(mvpLoc, false, ref floorMVP);

            GL.BindVertexArray(_floorVao);
            GL.DrawElements(PrimitiveType.Triangles, _floorIndexCount, DrawElementsType.UnsignedInt, 0);

            // (2) Стены
            GL.BindVertexArray(_wallVao);
            GL.BindTexture(TextureTarget.Texture2D, _textureWalls);

            foreach (var mat in _wallMatrices)
            {
                var mvp = mat * view * projection;
                GL.UniformMatrix4(mvpLoc, false, ref mvp);
                GL.DrawElements(PrimitiveType.Triangles, 36, DrawElementsType.UnsignedInt, 0);
            }

            SwapBuffers();
        }

        protected override void OnUnload()
        {
            base.OnUnload();
            GL.DeleteVertexArray(_floorVao);
            GL.DeleteVertexArray(_wallVao);
            _shader?.Dispose();
            GL.DeleteTexture(_textureFloor);
            GL.DeleteTexture(_textureWalls);
        }
    }
}
