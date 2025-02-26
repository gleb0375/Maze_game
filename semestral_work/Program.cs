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

            // ���� ������� PlayerStart
            int startRow = -1, startCol = -1;
            for (int r = 0; r < map.Rows; r++)
            {
                bool found = false;
                for (int c = 0; c < map.Columns; c++)
                {
                    if (map.Cells[r, c] == CellType.PlayerStart)
                    {
                        startRow = r;
                        startCol = c;
                        found = true;
                        break;
                    }
                }
                if (found) break;
            }

            if (startRow == -1 || startCol == -1)
                throw new Exception("No player start '@' found in map.");

            // ������������ ������� ���������� ������ ���� ������
            float startX = startCol * 2 + 1;
            float startZ = startRow * 2 + 1;
            // ������ �� ������ ���� 1.7 �
            var cameraStartPosition = new Vector3(startX, 1.7f, startZ);

            (int width, int height) = AppConfig.GetWindowDimensions();

            // ������ ������
            var camera = new Camera(width, height, cameraStartPosition, map);


            // ������ ����
            var nativeSettings = new NativeWindowSettings()
            {
                Size = new Vector2i(width, height),
                WindowBorder = OpenTK.Windowing.Common.WindowBorder.Resizable,
                Title = "Maze Game"
            };
            var gameWindowSettings = GameWindowSettings.Default;

            // ��������� Game
            using (var game = new Game(gameWindowSettings, nativeSettings, map, camera))
            {
                game.Run();
            }

            Log.CloseAndFlush();
        }
    }
}
