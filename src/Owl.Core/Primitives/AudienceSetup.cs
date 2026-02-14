using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace Owl.Core.Primitives
{
    public class AudienceSetup
    {
        public Point3d EyeLocation { get; set; }
        public Point3d Origin { get; set; }
        public List<Curve> Chairs { get; set; }
        public double FrontLimit { get; set; }
        public double HardBackLimit { get; set; }
        public double SoftBackLimit { get; set; }

        public GeometryBase PlanGeo { get; set; }
        public Point3d PlanOriginPt { get; set; } = Point3d.Origin;
        public double ChairWidth { get; set; } = 55.0;
        public System.Guid PlanChairBlockId { get; set; } = System.Guid.Empty;
        public string PlanChairBlockName { get; set; } = string.Empty;
        public List<GeometryBase> PlanGeometry { get; set; } = new List<GeometryBase>();

        public AudienceSetup()
        {
            Chairs = new List<Curve>();
            FrontLimit = 45.0;
            HardBackLimit = 182.5;
            SoftBackLimit = 200.0;
        }

        public AudienceSetup(Point3d eyeLocation, Point3d origin, List<Curve> chairs, double frontLimit = 45.0, double hardBackLimit = 182.5, double softBackLimit = 200.0)
        {
            EyeLocation = eyeLocation;
            Origin = origin;
            Chairs = chairs ?? new List<Curve>();
            FrontLimit = frontLimit;
            HardBackLimit = hardBackLimit;
            SoftBackLimit = softBackLimit;
        }

        public AudienceSetup Duplicate()
        {
            var dup = new AudienceSetup(
                EyeLocation,
                Origin,
                Chairs?.Select(c => c.DuplicateCurve()).ToList(),
                FrontLimit,
                HardBackLimit,
                SoftBackLimit
            )
            {
                PlanGeo = PlanGeo?.Duplicate(),
                PlanOriginPt = PlanOriginPt,
                ChairWidth = ChairWidth,
                PlanChairBlockId = PlanChairBlockId,
                PlanChairBlockName = PlanChairBlockName,
                PlanGeometry = PlanGeometry?.Select(g => g.Duplicate()).ToList()
            };
            return dup;
        }

        public string ToJson()
        {
             var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
             // Create a lightweight DTO or serialize directly if valid
             // Using an anonymous object to control structure matching requirements
             var dto = new 
             {
                SchemaVersion = "1.0",
                EyeLocation = $"{EyeLocation.X},{EyeLocation.Y},{EyeLocation.Z}",
                Origin = $"{Origin.X},{Origin.Y},{Origin.Z}",
                FrontLimit = FrontLimit,
                HardBackLimit = HardBackLimit,
                SoftBackLimit = SoftBackLimit,
                PlanSetup = new 
                {
                    BlockId = PlanChairBlockId.ToString(),
                    BlockName = PlanChairBlockName,
                    ChairWidth = ChairWidth,
                    PlanOriginOverride = $"{PlanOriginPt.X},{PlanOriginPt.Y},{PlanOriginPt.Z}"
                }
             };
             return System.Text.Json.JsonSerializer.Serialize(dto, options);
        }
    }
}
