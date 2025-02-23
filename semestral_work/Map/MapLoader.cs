using Microsoft.Extensions.Configuration;
using semestral_work.Config;

namespace semestral_work.Map
{
    internal class MapLoader
    {
        public static char[][] LoadMap()
        {
            string filePath = AppConfig.GetMapFilePath();

            if (string.IsNullOrEmpty(filePath))
                throw new Exception("File path is not specified in appsettings.json.");

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File {filePath} not found.");

            string[] lines = File.ReadAllLines(filePath);
            if (lines.Length == 0)
                throw new InvalidDataException("File is empty or has an invalid format.");

            char[][] map = new char[lines.Length][];

            for (int i = 0; i < lines.Length; i++)
            {
                map[i] = lines[i].ToCharArray();
            }

            return map;
        }
    }
}
