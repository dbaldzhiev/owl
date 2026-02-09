using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Owl.Core.Primitives;

namespace Owl.Grasshopper.Components.Setup
{
    public class Owl_TribuneSetup : GH_Component
    {
        public Owl_TribuneSetup()
          : base("Tribune Setup", "TribSetup",
              "Define the base dimensions and stepping logic of the tribune.",
              "Owl", "Setup")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("Rows", "Rows", "Number of elevated rows", GH_ParamAccess.item, 10);
            pManager.AddNumberParameter("RowWidths", "RowWs", "List of row widths (modules)", GH_ParamAccess.list, new List<double> { 200 });
            pManager.AddIntegerParameter("Elevations", "Elev", "List of steps per row", GH_ParamAccess.list, new List<int> { 3 });
            pManager.AddBooleanParameter("StairInsets", "Insets", "Shift stairs by railing width (per row)", GH_ParamAccess.list, new List<bool> { false });
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("TribuneSetup", "TSetup", "Tribune Setup Object", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            int rows = 0;
            List<double> rowWidths = new List<double>();
            List<int> elev = new List<int>();
            List<bool> insets = new List<bool>();

            if (!DA.GetData(0, ref rows)) return;
            if (!DA.GetDataList(1, rowWidths)) return;
            if (!DA.GetDataList(2, elev)) return;
            DA.GetDataList(3, insets); // Optional
            
            var setup = new TribuneSetup(rows, rowWidths, elev, insets);
            DA.SetData(0, setup);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                var assembly = typeof(Owl_TribuneSetup).Assembly;
                var resourceName = "Owl.Grasshopper.Icons.Owl_TribuneSetup_24.png";
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null) return null;
                    return new System.Drawing.Bitmap(stream);
                }
            }
        }
        public override Guid ComponentGuid => new Guid("11111111-1111-1111-1111-111111111111");
    }
}
