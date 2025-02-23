using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace semestral_work.Map
{
    public class ParsedMap
    {
        public CellType[,] Cells { get; }
        public int Rows { get; }
        public int Columns { get; }

        public ParsedMap(CellType[,] cells)
        {
            Cells = cells;
            Rows = cells.GetLength(0);
            Columns = cells.GetLength(1);
        }
    }
}
