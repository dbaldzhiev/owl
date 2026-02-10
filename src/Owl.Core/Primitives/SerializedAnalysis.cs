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

        public SerializedAnalysis()
        {
            Audiences = new List<AudienceSetup>();
            Sightlines = new List<Line>();
            Offsets = new List<double>();
        }

        public SerializedAnalysis(SerializedTribune tribune, List<AudienceSetup> audiences, List<Line> sightlines, List<double> offsets = null)
        {
            Tribune = tribune;
            Audiences = audiences ?? new List<AudienceSetup>();
            Sightlines = sightlines ?? new List<Line>();
            Offsets = offsets ?? new List<double>();
        }
    }
}
