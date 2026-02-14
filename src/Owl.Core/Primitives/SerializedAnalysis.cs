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
        public List<Line> LimitLines { get; set; }
        public Brep ProjectorCone { get; set; }
        public List<List<GeometryBase>> SectionChairs { get; set; }
        public List<Curve> PlanTribune { get; set; }
        public List<Curve> PlanRailings { get; set; }
        public List<Curve> PlanStairs { get; set; }
        public List<List<GeometryBase>> PlanChairs { get; set; }

        public SerializedAnalysis()
        {
            Audiences = new List<AudienceSetup>();
            Sightlines = new List<Line>();
            Offsets = new List<double>();
            LimitLines = new List<Line>();
            SectionChairs = new List<List<GeometryBase>>();
            LimitLines = new List<Line>();
            SectionChairs = new List<List<GeometryBase>>();
            PlanTribune = new List<Curve>();
            PlanRailings = new List<Curve>();
            PlanStairs = new List<Curve>();
            PlanChairs = new List<List<GeometryBase>>();
        }

        public SerializedAnalysis(SerializedTribune tribune, List<AudienceSetup> audiences, List<Line> sightlines, List<double> offsets = null, PlanSetup plan = null)
        {
            Tribune = tribune;
            Audiences = audiences ?? new List<AudienceSetup>();
            Sightlines = sightlines ?? new List<Line>();
            Offsets = offsets ?? new List<double>();
            Plan = plan;
            
            // Initialize others to empty to avoid nulls
            LimitLines = new List<Line>();
            SectionChairs = new List<List<GeometryBase>>();
            PlanTribune = new List<Curve>();
            PlanRailings = new List<Curve>();
            PlanStairs = new List<Curve>();
            PlanChairs = new List<List<GeometryBase>>();
        }
    }
}
