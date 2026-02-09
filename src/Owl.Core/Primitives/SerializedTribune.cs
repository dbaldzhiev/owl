using System.Collections.Generic;
using Rhino.Geometry;

namespace Owl.Core.Primitives
{
    public class SerializedTribune
    {
        public List<Point3d> RowPoints { get; set; }

        public SerializedTribune()
        {
            RowPoints = new List<Point3d>();
        }

        public SerializedTribune(List<Point3d> rowPoints)
        {
            RowPoints = rowPoints ?? new List<Point3d>();
        }
    }
}
