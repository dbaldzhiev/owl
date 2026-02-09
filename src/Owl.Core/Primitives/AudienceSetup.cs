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

        public AudienceSetup()
        {
            Chairs = new List<Curve>();
        }

        public AudienceSetup(Point3d eyeLocation, Point3d origin, List<Curve> chairs)
        {
            EyeLocation = eyeLocation;
            Origin = origin;
            Chairs = chairs ?? new List<Curve>();
        }

        public AudienceSetup Duplicate()
        {
            return new AudienceSetup(
                EyeLocation,
                Origin,
                Chairs?.Select(c => c.DuplicateCurve()).ToList()
            );
        }
    }
}
