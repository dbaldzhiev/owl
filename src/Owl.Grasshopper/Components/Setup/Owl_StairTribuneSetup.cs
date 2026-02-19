using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Owl.Core.Primitives;

namespace Owl.Grasshopper.Components.Setup
{
    public class Owl_StairTribuneSetup : GH_Component
    {
        public Owl_StairTribuneSetup()
          : base("Stair & Tribune Setup", "StairTrib",
              "Define parameters for the tribune stepping and stair dimensions.",
              "Owl", "Setup")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("Rows", "R", "Number of rows", GH_ParamAccess.item, 10);
            pManager.AddNumberParameter("RowWidth", "W", "Row width (tread depth)", GH_ParamAccess.list, 0.9);
            pManager.AddIntegerParameter("Elevations", "E", "Elevation steps per row", GH_ParamAccess.list, 2);
            pManager.AddBooleanParameter("StairInsets", "I", "Stair inset toggles", GH_ParamAccess.list, true);
            pManager.AddNumberParameter("TreadHeight", "TH", "Stair tread height", GH_ParamAccess.item, 0.15);
            pManager.AddNumberParameter("TreadWidth", "TW", "Stair tread width", GH_ParamAccess.item, 0.30);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("StairTribuneSetup", "Setup", "Combined Setup Object", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            int rows = 10;
            var rowWidths = new List<double>();
            var elevs = new List<int>();
            var insets = new List<bool>();
            double th = 0.15;
            double tw = 0.30;

            if (!DA.GetData(0, ref rows)) return;
            if (!DA.GetDataList(1, rowWidths)) return;
            if (!DA.GetDataList(2, elevs)) return;
            if (!DA.GetDataList(3, insets)) return;
            if (!DA.GetData(4, ref th)) return;
            if (!DA.GetData(5, ref tw)) return;

            var trib = new TribuneSetup(rows, rowWidths, elevs, insets);
            var stair = new StairSetup(th, tw);

            DA.SetData(0, new StairTribuneSetup(trib, stair));
        }

        protected override System.Drawing.Bitmap Icon => null;
        public override Guid ComponentGuid => new Guid("B2C3D4E5-F678-9012-3456-789ABCDEF012");
    }
}
