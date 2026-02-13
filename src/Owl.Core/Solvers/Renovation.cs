using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using Owl.Core.Primitives;

namespace Owl.Core.Solvers
{
    public class Renovation
    {
        public static SerializedTribune Solve(Curve frontLimit, Curve backLimit, Curve tribune, out Point3d flOrigin, out Point3d blOrigin, out List<Curve> treads, out List<Curve> risers)
        {
            flOrigin = Point3d.Unset;
            blOrigin = Point3d.Unset;
            treads = new List<Curve>();
            risers = new List<Curve>();

            if (tribune == null) return new SerializedTribune();

            // 1. Intersections
            if (frontLimit != null)
            {
                var events = Intersection.CurveCurve(frontLimit, tribune, 0.001, 0.001);
                if (events.Count > 0)
                {
                    flOrigin = events[0].PointA;
                }
            }

            if (backLimit != null)
            {
                var events = Intersection.CurveCurve(backLimit, tribune, 0.001, 0.001);
                if (events.Count > 0)
                {
                    blOrigin = events[0].PointA;
                }
            }

            // 2. Treads and Risers
            var polyline = tribune as PolylineCurve;
            if (polyline == null)
            {
                // Try to get as polyline if it's just a curve that can be converted
                if (tribune.TryGetPolyline(out var pl))
                {
                    polyline = new PolylineCurve(pl);
                }
            }

            if (polyline != null)
            {
                for (int i = 0; i < polyline.PointCount - 1; i++)
                {
                    Point3d p0 = polyline.Point(i);
                    Point3d p1 = polyline.Point(i + 1);
                    Curve segment = new LineCurve(p0, p1);

                    double dx = Math.Abs(p1.X - p0.X);
                    double dz = Math.Abs(p1.Z - p0.Z);

                    if (dz < 0.001) // Horizontal
                    {
                        treads.Add(segment);
                    }
                    else if (dx < 0.001) // Vertical
                    {
                        risers.Add(segment);
                    }
                }
            }

            return new SerializedTribune(null, null, false, risers, treads);
        }
    }
}
