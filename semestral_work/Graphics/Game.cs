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
        private int _wallVao;                // VAO куба 2×3×2
        private List<Matrix4> _wallMatrices; // модельные матрицы для каждой стены

        private Shader? _shader;
        private int _textureFloor;  // Текстура пола
        private int _textureWalls;  // Текстура для стен

        private bool _mouseGrabbed = true;

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

            // 1) Создаём плоскость (пол)
            (_floorVao, _floorIndexCount) = FloorBuilder.CreateFloorVAO(_map.Rows, _map.Columns);

            // 2) Создаём VAO для стен (используем наш куб 2×3×2)
            _wallVao = BlockBuilder.CreateBlockVAO();

            // 3) Считываем шейдеры
            string vertexPath = AppConfig.GetVertexShaderPath();
            string fragmentPath = AppConfig.GetFragmentShaderPath();
            string vertexCode = File.ReadAllText(vertexPath);
            string fragmentCode = File.ReadAllText(fragmentPath);
            _shader = new Shader(vertexCode, fragmentCode);

            // 4) Загружаем текстуры
            //    Пусть пол будет "floor.jpg", а для стен используем "brick.png" (или ту же floor.jpg, если brick нет).
            string floorPath = AppConfig.GetFloorTexturePath();
            _textureFloor = TextureLoader.LoadTexture(floorPath);

            // Допустим, есть другая текстура для стен "brick.png" 
            // Если у вас нет отдельного пути в appsettings, 
            // можно временно захардкодить путь или переиспользовать floorPath.
            string wallsPath = AppConfig.GetWallTexturePath();
            _textureWalls = TextureLoader.LoadTexture(wallsPath);

            // Настраиваем uniform у шейдера
            _shader.Use();
            int uTextureLocation = GL.GetUniformLocation(_shader.Handle, "uTexture");
            GL.Uniform1(uTextureLocation, 0); // Текстурный юнит 0

            // 5) Генерируем модельные матрицы для всех стен
            GenerateWallMatrices();
        }

        /// <summary>
        /// Создаёт матрицы для ячеек, где есть стена (CellType.Wall).
        /// Каждая клетка имеет размеры 2 по X и 2 по Z. Высота куба 3.
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
                        // Координаты для центра клетки
                        float x = col * 2.0f + 1.0f;
                        float z = row * 2.0f + 1.0f;

                        // Поднимаем куб на Y=1.5, чтобы нижняя грань была на Y=0
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

            // Обработка клавиши Esc для переключения режима курсора
            var input = KeyboardState;
            if (input.IsKeyPressed(GLKeys.Escape))
            {
                _mouseGrabbed = !_mouseGrabbed;
                CursorState = _mouseGrabbed ? CursorState.Grabbed : CursorState.Normal;
            }

            // Обновляем камеру
            _camera.Update(KeyboardState, MouseState, args);
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // Матрицы View и Projection из камеры
            Matrix4 view = _camera.GetViewMatrix();
            Matrix4 projection = _camera.GetProjectionMatrix();

            // Активируем шейдер
            _shader?.Use();
            int mvpLoc = GL.GetUniformLocation(_shader!.Handle, "uMVP");

            // 1) Рисуем пол
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _textureFloor);
            var floorModel = Matrix4.Identity; // лежит в y=0 (см. FloorBuilder)
            var floorMVP = floorModel * view * projection;
            GL.UniformMatrix4(mvpLoc, false, ref floorMVP);

            GL.BindVertexArray(_floorVao);
            GL.DrawElements(PrimitiveType.Triangles, _floorIndexCount, DrawElementsType.UnsignedInt, 0);

            // 2) Рисуем стены
            GL.BindVertexArray(_wallVao);
            // Для стен используем другую текстуру (brick.png), 
            // или если brick нет, используем ту же floor
            GL.BindTexture(TextureTarget.Texture2D, _textureWalls);

            foreach (var modelMatrix in _wallMatrices)
            {
                var mvp = modelMatrix * view * projection;
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