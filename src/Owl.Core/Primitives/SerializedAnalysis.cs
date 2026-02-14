using System.Collections.Generic;
using Rhino.Geometry;

namespace Owl.Core.Primitives
{
    public class SerializedAnalysis
    {
        public SerializedTribune Tribune { get; set; }
        public List<AudienceSetup> Audiences { get; set; }
        public List<Line> Sightlines { get; set; }
        public List<double> Offsets { get; set; }
        public PlanSetup Plan { get; set; }

        public Brep ProjectorCone { get; set; }
        public List<List<Line>> LimitLines { get; set; }
        public List<List<GeometryBase>> SectionChairs { get; set; }
        public List<List<GeometryBase>> PlanChairs { get; set; }
        public List<Curve> PlanTribuneLines { get; set; }
        public List<Curve> PlanRailingLines { get; set; }
        public List<Curve> PlanStairLines { get; set; }

        public SerializedAnalysis()
        {
            Audiences = new List<AudienceSetup>();
            Sightlines = new List<Line>();
            Offsets = new List<double>();
            LimitLines = new List<List<Line>>();
            SectionChairs = new List<List<GeometryBase>>();
            PlanChairs = new List<List<GeometryBase>>();
            PlanTribuneLines = new List<Curve>();
            PlanRailingLines = new List<Curve>();
            PlanStairLines = new List<Curve>();
        }

        public SerializedAnalysis(
            SerializedTribune tribune, 
            List<AudienceSetup> audiences, 
            List<Line> sightlines, 
            List<double> offsets = null, 
            PlanSetup plan = null,
            Brep projectorCone = null,
            List<List<Line>> limitLines = null,
            List<List<GeometryBase>> sectionChairs = null,
            List<List<GeometryBase>> planChairs = null,
            List<Curve> planTribuneLines = null,
            List<Curve> planRailingLines = null,
            List<Curve> planStairLines = null)
        {
            Tribune = tribune;
            Audiences = audiences ?? new List<AudienceSetup>();
            Sightlines = sightlines ?? new List<Line>();
            Offsets = offsets ?? new List<double>();
            Plan = plan;
            ProjectorCone = projectorCone;
            LimitLines = limitLines ?? new List<List<Line>>();
            SectionChairs = sectionChairs ?? new List<List<GeometryBase>>();
            PlanChairs = planChairs ?? new List<List<GeometryBase>>();
            PlanTribuneLines = planTribuneLines ?? new List<Curve>();
            PlanRailingLines = planRailingLines ?? new List<Curve>();
            PlanStairLines = planStairLines ?? new List<Curve>();
        }
    }
}
