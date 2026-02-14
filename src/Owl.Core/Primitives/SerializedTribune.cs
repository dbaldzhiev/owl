using System.Collections.Generic;
using Rhino.Geometry;

namespace Owl.Core.Primitives
{
    public class SerializedTribune
    {
        public List<Point3d> RowPoints { get; set; }
        public bool Flip { get; set; }
        public List<double> Gaps { get; set; }
        public List<Curve> Risers { get; set; }
        public List<Curve> Treads { get; set; }
        public List<bool> RailingToggles { get; set; }
        public List<Point3d> SecRowSpine { get; set; }

        public SerializedTribune()
        {
            RowPoints = new List<Point3d>();
            Gaps = new List<double>();
            Flip = false;
            Risers = new List<Curve>();
            Treads = new List<Curve>();
            RailingToggles = new List<bool>();
            SecRowSpine = new List<Point3d>();
        }

        public SerializedTribune(List<Point3d> rowPoints, List<double>? gaps = null, bool flip = false, List<Curve>? risers = null, List<Curve>? treads = null, List<bool>? railingToggles = null, List<Point3d>? secRowSpine = null)
        {
            RowPoints = rowPoints ?? new List<Point3d>();
            Gaps = gaps ?? new List<double>();
            Flip = flip;
            Risers = risers ?? new List<Curve>();
            Treads = treads ?? new List<Curve>();
            RailingToggles = railingToggles ?? new List<bool>();
            SecRowSpine = secRowSpine ?? new List<Point3d>();
        }
    }
}
