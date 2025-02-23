using Microsoft.Extensions.Configuration;
using semestral_work.Config;
using semestral_work.Map;
using System;
using System.IO;
using Serilog;

namespace semestral_work
{
    internal static class Program
    {
        static void Main()
        {
            AppConfig.Init();
            LoggerSetup.InitializeLogger();

            try
            {
                ParsedMap map = MapParser.ParseMap();
                Log.Information("Map has been successfully parsed!");

                for (int i = 0; i < map.Rows; i++)
                {
                    for (int j = 0; j < map.Columns; j++)
                    {
                        Console.Write($"{map.Cells[i, j],-15} ");
                    }
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading map");
            }

            Log.CloseAndFlush();
        }
    }
}
