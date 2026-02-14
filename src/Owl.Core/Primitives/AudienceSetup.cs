using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace Owl.Core.Primitives
{
    public class AudienceSetup
    {
        public Point3d EyeLocation { get; set; }
        public Point3d SecOrigin { get; set; }
        public List<Curve> SecChairGeo { get; set; }
        public double SecFL { get; set; }
        public double SecHBL { get; set; }
        public double SecSBL { get; set; }

        // Plan properties
        public List<Curve> PlanChairGeo { get; set; }
        public Point3d PlanChairOrigin { get; set; }
        public double PlanChairWidth { get; set; }

        public AudienceSetup()
        {
            SecChairGeo = new List<Curve>();
            SecFL = 45.0;
            SecHBL = 182.5;
            SecSBL = 200.0;
            PlanChairGeo = new List<Curve>();
            PlanChairOrigin = Point3d.Origin;
            PlanChairWidth = 500.0;
        }

        public AudienceSetup(
            Point3d eyeLocation,
            Point3d secOrigin,
            List<Curve> secChairGeo,
            double secFL = 45.0,
            double secHBL = 182.5,
            double secSBL = 200.0,
            List<Curve> planChairGeo = null,
            Point3d planChairOrigin = default,
            double planChairWidth = 500.0)
        {
            EyeLocation = eyeLocation;
            SecOrigin = secOrigin;
            SecChairGeo = secChairGeo ?? new List<Curve>();
            SecFL = secFL;
            SecHBL = secHBL;
            SecSBL = secSBL;
            PlanChairGeo = planChairGeo ?? new List<Curve>();
            PlanChairOrigin = planChairOrigin;
            PlanChairWidth = planChairWidth;
        }

        public AudienceSetup Duplicate()
        {
            return new AudienceSetup(
                EyeLocation,
                SecOrigin,
                SecChairGeo?.Select(c => c.DuplicateCurve()).ToList(),
                SecFL,
                SecHBL,
                SecSBL,
                PlanChairGeo?.Select(c => c.DuplicateCurve()).ToList(),
                PlanChairOrigin,
                PlanChairWidth
            );
        }
    }
}
