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

        public string ToJson(double tolerance = 0.001)
        {
            var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            
            // Helper to serialize curves - basic polyline/nurbs extraction or simplified representation?
            // User requested: "boundary_tribune (curve serialization)"
            // "If your existing system serializes RhinoCommon geometry to Base64, reuse it."
            
            var dto = new 
            {
                SchemaVersion = "1.0",
                OriginPlan = $"{Origin.X},{Origin.Y},{Origin.Z}",
                Tolerance = tolerance,
                BoundaryTribune = GeometryToBase64(TribuneBoundary),
                BoundariesAisles = AisleBoundaries?.ConvertAll(GeometryToBase64),
                BoundaryTunnel = TunnelBoundary != null ? GeometryToBase64(TunnelBoundary) : null
            };
            return System.Text.Json.JsonSerializer.Serialize(dto, options);
        }

        private static string GeometryToBase64(GeometryBase geo)
        {
            if (geo == null) return null;
            var options = new Rhino.FileIO.SerializationOptions();
            return geo.ToJSON(options); 
        }
    }
}
