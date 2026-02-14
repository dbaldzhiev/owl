using Rhino.Geometry;
using System.Collections.Generic;

namespace Owl.Core.Primitives
{
    public class PlanSetup
    {
        public Point3d Origin { get; set; }
        public Curve TribuneBoundary { get; set; }
        public List<Curve> AisleBoundaries { get; set; }

        public Curve TunnelBoundary { get; set; }

        public PlanSetup()
        {
            Origin = Point3d.Origin;
            AisleBoundaries = new List<Curve>();
        }

        public PlanSetup(Point3d origin, Curve tribuneBoundary, List<Curve> aisleBoundaries = null, Curve tunnelBoundary = null)
        {
            Origin = origin;
            TribuneBoundary = tribuneBoundary;
            AisleBoundaries = aisleBoundaries ?? new List<Curve>();
            TunnelBoundary = tunnelBoundary;
        }

        public PlanSetup Duplicate()
        {
            return new PlanSetup(
                Origin,
                TribuneBoundary?.DuplicateCurve(),
                AisleBoundaries?.ConvertAll(c => c.DuplicateCurve()),
                TunnelBoundary?.DuplicateCurve()
            );
        }
    }
}
