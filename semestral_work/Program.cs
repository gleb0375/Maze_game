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
    /// <summary>
    /// Vstupní bod aplikace. Inicializuje konfiguraci, logování, mapu, kameru a spouští hru.
    /// </summary>
    internal static class Program
    {
        static void Main()
        {
            // Načtení konfigurace z appsettings.json
            AppConfig.Init();

            // Inicializace loggeru (Serilog)
            LoggerSetup.InitializeLogger();

            // zpracování mapy
            ParsedMap map = MapParser.ParseMap();

            // Načtení parametrů z konfigurace
            (int width, int height) = AppConfig.GetWindowDimensions();
            float movementSpeed = AppConfig.GetMovementSpeed();
            float mouseSens = AppConfig.GetMouseSensivity();
            float lightHeight = AppConfig.GetLightHeight();
            float angleDep = AppConfig.GetAngleOfDepression();

            // Získání počáteční pozice hráče ze symbolu '@' na mapě
            Vector3 playerStart = GetPlayerStartPosition(map);

            // Vytvoření kamery
            Camera camera = new Camera(width, height, playerStart, map, movementSpeed, mouseSens, lightHeight, angleDep);

            // Nastavení okna
            var nativeSettings = new NativeWindowSettings()
            {
                ClientSize = new Vector2i(width, height),
                WindowBorder = OpenTK.Windowing.Common.WindowBorder.Resizable,
                Title = "Maze Game"
            };

            // Spuštění instance hry
            using (Game game = new Game(GameWindowSettings.Default, nativeSettings, map, camera))
            {
                game.Run();
            }

            // Korektní ukončení logování
            Log.CloseAndFlush();
        }

        /// <summary>
        /// Vyhledá na mapě pozici hráče (symbol '@') a vrátí souřadnice středu této buňky.
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
                throw new Exception("Na mapě nebyla nalezena startovní pozice hráče '@'.");
            
            float startX = startCol * 2 + 1;
            float startZ = startRow * 2 + 1;
            float cameraHeight = AppConfig.GetCameraHeight();

            return new Vector3(startX, cameraHeight, startZ);
        }
    }
}
