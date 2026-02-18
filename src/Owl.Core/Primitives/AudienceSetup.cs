using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace Owl.Core.Primitives
{
    public class AudienceSetup
    {
        public Point3d EyeLocation { get; set; }
        public Plane SecOriginPlane { get; set; }
        public List<Curve> SecChairGeo { get; set; }
        public double SecFL { get; set; }
        public double SecHBL { get; set; }
        public double SecSBL { get; set; }

        // Plan properties
        public List<Curve> PlanChairGeo { get; set; }
        public Plane PlanChairOriginPlane { get; set; }
        public double PlanChairWidth { get; set; }

        public AudienceSetup()
        {
            SecChairGeo = new List<Curve>();
            SecOriginPlane = Plane.WorldYZ;
            SecFL = 45.0;
            SecHBL = 182.5;
            SecSBL = 200.0;
            PlanChairGeo = new List<Curve>();
            PlanChairOriginPlane = Plane.WorldXY;
            PlanChairWidth = 500.0;
        }

        public AudienceSetup(
            Point3d eyeLocation,
            Plane secOriginPlane,
            List<Curve>? secChairGeo = null,
            double secFL = 45.0,
            double secHBL = 182.5,
            double secSBL = 200.0,
            List<Curve>? planChairGeo = null,
            Plane planChairOriginPlane = default,
            double planChairWidth = 500.0)
        {
            EyeLocation = eyeLocation;
            SecOriginPlane = secOriginPlane;
            SecChairGeo = secChairGeo ?? new List<Curve>();
            SecFL = secFL;
            SecHBL = secHBL;
            SecSBL = secSBL;
            PlanChairGeo = planChairGeo ?? new List<Curve>();
            PlanChairOriginPlane = planChairOriginPlane; // Struct default is invalid, so let's check
            if (!planChairOriginPlane.IsValid) PlanChairOriginPlane = Plane.WorldXY;
            PlanChairWidth = planChairWidth;
        }

        public AudienceSetup Duplicate()
        {
            return new AudienceSetup(
                EyeLocation,
                SecOriginPlane,
                SecChairGeo?.Select(c => c.DuplicateCurve()).ToList(),
                SecFL,
                SecHBL,
                SecSBL,
                PlanChairGeo?.Select(c => c.DuplicateCurve()).ToList(),
                PlanChairOriginPlane,
                PlanChairWidth
            );
        }
    }
}
