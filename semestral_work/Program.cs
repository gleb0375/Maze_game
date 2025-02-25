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
            // Для примера создадим камеру: стартовая позиция чуть выше пола (y=2)
            // и немного смещена по Z
            var camera = new Camera(800, 600, new Vector3(0, 2, 5));

            (int width, int height) = AppConfig.GetWindowDimensions();

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
    }
}
