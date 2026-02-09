using System.Collections.Generic;

namespace Owl.Core.Primitives
{
    public class TribuneSetup
    {
        public int Rows { get; }
        public List<double> RowWidths { get; }
        public List<int> ElevCounts { get; }
        public List<bool> StairInsets { get; }

        public TribuneSetup(int rows, List<double> rowWidths, List<int> elevCounts, List<bool> stairInsets)
        {
            Rows = rows;
            RowWidths = rowWidths ?? new List<double>();
            ElevCounts = elevCounts ?? new List<int>();
            StairInsets = stairInsets ?? new List<bool>();
        }
    }
}
