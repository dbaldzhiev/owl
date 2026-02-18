using System.Collections.Generic;
using Rhino.Geometry;

namespace Owl.Core.Primitives
{
    public class HallSetup
    {
        public Curve TribuneBoundary { get; set; }
        public List<Curve> AisleBoundaries { get; set; }
        public List<Curve> TunnelBoundaries { get; set; }

        public HallSetup()
        {
            AisleBoundaries = new List<Curve>();
            TunnelBoundaries = new List<Curve>();
        }

        public HallSetup(Curve tribuneBoundary, List<Curve> aisleBoundaries = null, List<Curve> tunnelBoundaries = null, Plane sectionFrame = default, Plane planFrame = default, Point3d projectorLocation = default, Curve screenCurve = null)
        {
            TribuneBoundary = tribuneBoundary;
            AisleBoundaries = aisleBoundaries ?? new List<Curve>();
            TunnelBoundaries = tunnelBoundaries ?? new List<Curve>();
            SectionFrame = sectionFrame != default ? sectionFrame : Plane.WorldXY;
            PlanFrame = planFrame != default ? planFrame : Plane.WorldXY;
            ProjectorLocation = projectorLocation;
            ScreenCurve = screenCurve;
        }

        public Plane SectionFrame { get; set; } = Plane.WorldXY;
        public Plane PlanFrame { get; set; } = Plane.WorldXY;
        public Point3d ProjectorLocation { get; set; }
        public Curve ScreenCurve { get; set; }
    }
}
