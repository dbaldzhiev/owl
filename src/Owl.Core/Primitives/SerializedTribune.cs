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
        public List<Point3d> StairPoints { get; set; }
        public Curve TribuneProfile { get; set; }
        public Curve StairsProfile { get; set; }
        public List<Curve> RailingProfiles { get; set; }

        public SerializedTribune()
        {
            RowPoints = new List<Point3d>();
            Gaps = new List<double>();
            Flip = false;
            Risers = new List<Curve>();
            Treads = new List<Curve>();
            StairPoints = new List<Point3d>();
            RailingProfiles = new List<Curve>();
        }

        public SerializedTribune(List<Point3d> rowPoints, List<double>? gaps = null, bool flip = false, List<Curve>? risers = null, List<Curve>? treads = null, List<Point3d>? stairPoints = null, Curve tribuneProfile = null, Curve stairsProfile = null, List<Curve> railingProfiles = null)
        {
            RowPoints = rowPoints ?? new List<Point3d>();
            Gaps = gaps ?? new List<double>();
            Flip = flip;
            Risers = risers ?? new List<Curve>();
            Treads = treads ?? new List<Curve>();
            StairPoints = stairPoints ?? new List<Point3d>();
            TribuneProfile = tribuneProfile;
            StairsProfile = stairsProfile;
            RailingProfiles = railingProfiles ?? new List<Curve>();
        }
    }
}
