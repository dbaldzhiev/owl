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

        public HallSetup(Curve tribuneBoundary, List<Curve> aisleBoundaries = null, List<Curve> tunnelBoundaries = null)
        {
            TribuneBoundary = tribuneBoundary;
            AisleBoundaries = aisleBoundaries ?? new List<Curve>();
            TunnelBoundaries = tunnelBoundaries ?? new List<Curve>();
        }
    }
}
