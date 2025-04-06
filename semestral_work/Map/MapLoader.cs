using Microsoft.Extensions.Configuration;
using semestral_work.Config;
using System;
using System.IO;

namespace semestral_work.Map
{
    internal class MapLoader
    {
        /// <summary>
        /// Načte mapu ze souboru specifikovaného v appsettings.json.
        /// Vrací pole znakových řádků.
        /// </summary>
        public static char[][] LoadMap()
        {
            string filePath = AppConfig.GetMapFilePath();

            if (string.IsNullOrEmpty(filePath))
                throw new Exception("Cesta k souboru není uvedena v appsettings.json.");

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Soubor {filePath} nebyl nalezen.");

            string[] lines = File.ReadAllLines(filePath);
            if (lines.Length == 0)
                throw new InvalidDataException("Soubor je prázdný nebo má neplatný formát.");

            char[][] map = new char[lines.Length][];

            for (int i = 0; i < lines.Length; i++)
            {
                map[i] = lines[i].ToCharArray();
            }

            return map;
        }
    }
}
