using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace semestral_work.Map
{
    internal class MapParser
    {

        public static ParsedMap ParseMap()
        {
            char[][] rawMap = MapLoader.LoadMap();

            if (rawMap.Length == 0)
            {
                Log.Error("Map file is empty.");
                throw new Exception("Map file is empty.");
            }

            string dimensionLine = new string(rawMap[0]);
            string[] dims = dimensionLine.Split('x', 'X');

            if (dims.Length != 2 ||
                !int.TryParse(dims[0], out int width) ||
                !int.TryParse(dims[1], out int height))
            {
                Log.Error("Invalid map dimensions line: {DimensionLine}", dimensionLine);
                throw new Exception("Invalid map dimensions line.");
            }

            if (rawMap.Length - 1 != height)
            {
                Log.Error("Number of rows in map ({ActualRows}) does not match expected height ({ExpectedHeight}).", rawMap.Length - 1, height);
                throw new Exception("Map row count mismatch.");
            }

            CellType[,] parsedCells = new CellType[height, width];
            int playerStartCount = 0;

            for (int i = 0; i < height; i++)
            {
                char[] rowChars = rawMap[i + 1];

                if (rowChars.Length != width)
                {
                    Log.Error("Row {RowIndex} length ({ActualWidth}) does not match expected width ({ExpectedWidth}).", i, rowChars.Length, width);
                    throw new Exception("Invalid row length in map.");
                }

                for (int j = 0; j < width; j++)
                {
                    char currentChar = rowChars[j];
                    parsedCells[i, j] = ConvertCharToCellType(currentChar);

                    if (parsedCells[i, j] == CellType.PlayerStart)
                        playerStartCount++;
                }
            }

            if (playerStartCount != 1)
            {
                Log.Error("Map must contain exactly one player start position. Found {PlayerStartCount}.", playerStartCount);
                throw new Exception("Invalid number of player start positions.");
            }

            Log.Information("Map successfully parsed.");
            return new ParsedMap(parsedCells);
        }

        private static CellType ConvertCharToCellType(char c)
        {
            // По заданию:
            // " " или 'a' - 'n' => свободное пространство
            // 'o' - 'z' => стена
            // '@' => стартовая позиция наблюдателя
            // '*', '^', '!' => светильники
            // 'A' - 'G' => двери и входы в тайные ходы
            // 'H' - 'N' => прочие объекты
            // 'O' - 'R' => чужие персонажи (противники)
            // 'T' - 'Z' => предметы для сбора

            if (c == '@')
                return CellType.PlayerStart;
            else if (c >= 'o' && c <= 'z')
                return CellType.Wall;
            else if (c == '*' || c == '^' || c == '!')
                return CellType.Light;
            else if (c >= 'A' && c <= 'G')
                return CellType.Door;
            else if (c >= 'H' && c <= 'N')
                return CellType.SolidObject;
            else if (c >= 'O' && c <= 'R')
                return CellType.Enemy;
            else if (c >= 'T' && c <= 'Z')
                return CellType.Collectable;
            else if (c == 'k')
                return CellType.Photo1;
            else if (c == 'l')
                return CellType.Photo1;
            else
                return CellType.Free;
        }
    }
}