using System.Collections.Generic;
using Rhino.Geometry;

namespace Owl.Core.Primitives
{
    public class SerializedTribune
    {
        public Point3d Origin { get; set; }
        public List<Point3d> RowPoints { get; set; }
        public List<double> Gaps { get; set; }
        public bool Flip { get; set; }
        public List<Curve> Risers { get; set; }
        public List<Curve> Treads { get; set; }

        public SerializedTribune()
        {
            RowPoints = new List<Point3d>();
            Gaps = new List<double>();
            Flip = false;
            Risers = new List<Curve>();
            Treads = new List<Curve>();
            Origin = Point3d.Origin;
        }

        public SerializedTribune(List<Point3d> rowPoints, List<double>? gaps = null, bool flip = false, List<Curve>? risers = null, List<Curve>? treads = null, Point3d origin = default)
        {
            RowPoints = rowPoints ?? new List<Point3d>();
            Gaps = gaps ?? new List<double>();
            Flip = flip;
            Risers = risers ?? new List<Curve>();
            Treads = treads ?? new List<Curve>();
            Origin = origin;
        }
    }
}
