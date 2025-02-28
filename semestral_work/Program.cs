using Microsoft.Extensions.Configuration;
using semestral_work.Config;
using semestral_work.Map;
using System;
using Serilog;
using semestral_work.Graphics;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;

namespace semestral_work
{
    internal static class Program
    {
        static void Main()
        {
            AppConfig.Init();
            LoggerSetup.InitializeLogger();

            ParsedMap map = MapParser.ParseMap();

            // Получаем стартовую позицию игрока из карты
            Vector3 cameraStartPosition = GetPlayerStartPosition(map);

            (int width, int height) = AppConfig.GetWindowDimensions();

            // Создаём камеру, передавая карту (можно использовать для коллизий и т.д.)
            var camera = new Camera(width, height, cameraStartPosition, map);

            // Настраиваем параметры окна
            var nativeSettings = new NativeWindowSettings()
            {
                Size = new Vector2i(width, height),
                WindowBorder = OpenTK.Windowing.Common.WindowBorder.Resizable,
                Title = "Maze Game"
            };
            var gameWindowSettings = GameWindowSettings.Default;

            using (var game = new Game(gameWindowSettings, nativeSettings, map, camera))
            {
                game.Run();
            }

            Log.CloseAndFlush();
        }

        /// <summary>
        /// Ищет в ParsedMap ячейку PlayerStart и возвращает мировые координаты центра этой клетки.
        /// Если ячейка не найдена, выбрасывает исключение.
        /// </summary>
        private static Vector3 GetPlayerStartPosition(ParsedMap map)
        {
            int startRow = -1, startCol = -1;
            for (int r = 0; r < map.Rows; r++)
            {
                for (int c = 0; c < map.Columns; c++)
                {
                    if (map.Cells[r, c] == CellType.PlayerStart)
                    {
                        startRow = r;
                        startCol = c;
                        break;
                    }
                }
                if (startRow != -1)
                    break;
            }

            if (startRow == -1 || startCol == -1)
                throw new Exception("No player start '@' found in map.");

            // Каждая клетка имеет размер 2 м, центр клетки: (col*2+1, row*2+1)
            float startX = startCol * 2 + 1;
            float startZ = startRow * 2 + 1;
            // Высота глаз – 1.7 м

            float CameraHeight = AppConfig.GetCameraHeight();
            return new Vector3(startX, CameraHeight, startZ);
        }
    }
}
