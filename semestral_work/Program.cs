using Microsoft.Extensions.Configuration;
using semestral_work.Config;
using semestral_work.Map;
using semestral_work.Graphics;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using Serilog;
using System;

namespace semestral_work
{
    internal static class Program
    {
        static void Main()
        {
            AppConfig.Init();
            LoggerSetup.InitializeLogger();

            // Parse the map once
            ParsedMap map = MapParser.ParseMap();

            // Get window dimensions from configuration
            (int width, int height) = AppConfig.GetWindowDimensions();
            float movementSpeed = AppConfig.GetMovementSpeed();
            float mouseSens = AppConfig.GetMouseSensivity();
            float lightHeight = AppConfig.GetLightHeight();
            float angleDep = AppConfig.GetAngleOfDepression();

            // Determine the player's starting position from the map
            Vector3 playerStart = GetPlayerStartPosition(map);

            // Create the camera directly
            Camera camera = new Camera(width, height, playerStart, map, movementSpeed, mouseSens, lightHeight, angleDep);

            // Set up native window settings
            var nativeSettings = new NativeWindowSettings()
            {
                ClientSize = new Vector2i(width, height),
                WindowBorder = OpenTK.Windowing.Common.WindowBorder.Resizable,
                Title = "Maze Game"
            };

            // Create and run the game directly
            using (Game game = new Game(GameWindowSettings.Default, nativeSettings, map, camera))
            {
                game.Run();
            }

            Log.CloseAndFlush();
        }

        /// <summary>
        /// Searches the parsed map for the PlayerStart cell and returns the world coordinates
        /// of the center of that cell. Throws an exception if not found.
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

            // Each cell is 2 meters; the center is at (col * 2 + 1, row * 2 + 1)
            float startX = startCol * 2 + 1;
            float startZ = startRow * 2 + 1;
            float cameraHeight = AppConfig.GetCameraHeight();

            return new Vector3(startX, cameraHeight, startZ);
        }
    }
}
