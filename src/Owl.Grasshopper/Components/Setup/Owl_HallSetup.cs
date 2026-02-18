using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Owl.Core.Primitives;

namespace Owl.Grasshopper.Components.Setup
{
    public class Owl_HallSetup : GH_Component
    {
        public Owl_HallSetup()
          : base("Hall Setup", "Hall",
              "Define the hall boundaries for plan generation (tribune, aisles, tunnels).",
              "Owl", "Setup")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("TribuneBoundary", "TribBnd", "Overall tribune footprint boundary curve", GH_ParamAccess.item);
            pManager.AddCurveParameter("AisleBoundaries", "Aisles", "Aisle boundary curves", GH_ParamAccess.list);
            pManager.AddCurveParameter("TunnelBoundaries", "Tunnels", "Tunnel/void boundary curves", GH_ParamAccess.list);
            pManager.AddPlaneParameter("SectionFrame", "SecFrame", "Frame defining the section cut and origin", GH_ParamAccess.item, Plane.WorldXY);
            pManager.AddPlaneParameter("PlanFrame", "PlanFrame", "Frame defining the plan origin", GH_ParamAccess.item, Plane.WorldXY);
            pManager.AddPointParameter("ProjectorLocation", "ProjPt", "Location of the projector", GH_ParamAccess.item);
            pManager.AddCurveParameter("ScreenCurve", "Screen", "Curve representing the screen", GH_ParamAccess.item);

            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
            pManager[5].Optional = true;
            pManager[6].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("HallSetup", "Hall", "Hall Setup Object", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve tribBnd = null;
            List<Curve> aisles = new List<Curve>();
            List<Curve> tunnels = new List<Curve>();
            Plane secFrame = Plane.WorldXY;
            Plane planFrame = Plane.WorldXY;
            Point3d projPt = Point3d.Unset;
            Curve screen = null;

            if (!DA.GetData(0, ref tribBnd) || tribBnd == null) return;
            DA.GetDataList(1, aisles);
            DA.GetDataList(2, tunnels);
            DA.GetData(3, ref secFrame);
            DA.GetData(4, ref planFrame);
            DA.GetData(5, ref projPt);
            DA.GetData(6, ref screen);

            // Project Boundaries to PlanFrame
            tribBnd = Curve.ProjectToPlane(tribBnd, planFrame);
            
            var projAisles = new List<Curve>();
            foreach(var c in aisles) if(c!=null) projAisles.Add(Curve.ProjectToPlane(c, planFrame));
            
            var projTunnels = new List<Curve>();
            foreach(var c in tunnels) if(c!=null) projTunnels.Add(Curve.ProjectToPlane(c, planFrame));

            var setup = new HallSetup(tribBnd, projAisles, projTunnels, secFrame, planFrame, projPt, screen);
            DA.SetData(0, setup);
        }

        protected override System.Drawing.Bitmap Icon => null;
        public override Guid ComponentGuid => new Guid("A1B2C3D4-5678-9012-3456-789012345678");
    }
}
