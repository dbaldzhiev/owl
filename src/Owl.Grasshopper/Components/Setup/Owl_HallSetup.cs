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

            pManager[1].Optional = true;
            pManager[2].Optional = true;
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

            if (!DA.GetData(0, ref tribBnd) || tribBnd == null) return;
            DA.GetDataList(1, aisles);
            DA.GetDataList(2, tunnels);

            var setup = new HallSetup(tribBnd, aisles, tunnels);
            DA.SetData(0, setup);
        }

        protected override System.Drawing.Bitmap Icon => null;
        public override Guid ComponentGuid => new Guid("A1B2C3D4-5678-9012-3456-789012345678");
    }
}
