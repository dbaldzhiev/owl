using System.Collections.Generic;
using Rhino.Geometry;

namespace Owl.Core.Primitives
{
    public class SerializedTribune
    {
        public List<Point3d> RowPoints { get; set; }
        public bool Flip { get; set; }
        public List<double> Gaps { get; set; }

        public SerializedTribune()
        {
            RowPoints = new List<Point3d>();
            Gaps = new List<double>();
            Flip = false;
        }

        public SerializedTribune(List<Point3d> rowPoints, List<double>? gaps = null, bool flip = false)
        {
            RowPoints = rowPoints ?? new List<Point3d>();
            Gaps = gaps ?? new List<double>();
            Flip = flip;
        }
    }
}
