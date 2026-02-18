using System;
using System.Collections.Generic;
using Rhino.Geometry;

namespace Owl.Core.Primitives
{
    /// <summary>
    /// Container for the results of a TribuneSolver calculation.
    /// Acts as the Single Source of Truth for visualization and validation.
    /// </summary>
    public class TribuneSolution
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public Plane BasePlane { get; set; } = Plane.WorldXY;

        // Section Data
        public Curve SectionTribuneProfile { get; set; }
        public Curve SectionStairsProfile { get; set; }
        public List<Curve> SectionRailings { get; set; } = new List<Curve>();
        public List<Point3d> SectionRailingsSpine { get; set; } = new List<Point3d>();
        public List<Point3d> SectionRowSpine { get; set; } = new List<Point3d>(); // "Origins" of rows in section
        public List<List<Curve>> SectionChairs { get; set; } = new List<List<Curve>>();
        public List<List<Line>> SectionLimitLines { get; set; } = new List<List<Line>>();

        // Plan Data
        public List<Curve> PlanTribuneLines { get; set; } = new List<Curve>();
        public List<Curve> PlanStairLines { get; set; } = new List<Curve>();
        public List<Curve> PlanRailings { get; set; } = new List<Curve>();
        public List<Curve> PlanRailingsSpine { get; set; } = new List<Curve>();
        public List<List<Curve>> PlanChairs { get; set; } = new List<List<Curve>>();
        public List<List<Curve>> PlanRowSpine { get; set; } = new List<List<Curve>>(); // Curves along which chairs are distributed

        // Placement Planes (for block insertion or logic)
        public List<Plane> SectionChairPlanes { get; set; } = new List<Plane>();
        public List<List<Plane>> PlanChairPlanes { get; set; } = new List<List<Plane>>();

        // Projector/Screen & Clash
        public Point3d SectionProjector { get; set; }
        public Curve SectionScreen { get; set; }
        public Curve ExistingTribuneProfile { get; set; }
        public List<Point3d> Clashes { get; set; } = new List<Point3d>();

        // Metadata / Validation Helpers
        public List<Point3d> RowPoints { get; set; } = new List<Point3d>(); // Nominal row starts (World)
        public List<Point3d> RowLocalPoints { get; set; } = new List<Point3d>(); // Nominal row starts (Local Profile Space)
        public List<double> Gaps { get; set; } = new List<double>();
        public List<bool> RailingToggles { get; set; } = new List<bool>();
        public List<double> StairFlightStartX { get; set; } = new List<double>();
        public List<double> StairFlightEndX { get; set; } = new List<double>();
        public bool Flipped { get; set; }

        public List<AudienceSetup> Audiences { get; set; } = new List<AudienceSetup>();
        public List<double> AudienceOffsets { get; set; } = new List<double>();
        public HallSetup HallSetup { get; set; }

        public TribuneSolution() { }
    }
}
