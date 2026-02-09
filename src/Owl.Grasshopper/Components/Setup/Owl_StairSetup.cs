using System;
using Grasshopper.Kernel;
using Owl.Core.Primitives;

namespace Owl.Grasshopper.Components.Setup
{
    public class Owl_StairSetup : GH_Component
    {
        public Owl_StairSetup()
          : base("Stair Setup", "StairSetup",
              "Define the dimensions of the steps (risers and treads).",
              "Owl", "Setup")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("TreadHeight", "TrH", "Height of one riser", GH_ParamAccess.item, 0.15);
            pManager.AddNumberParameter("TreadWidth", "TrW", "Depth of one tread step", GH_ParamAccess.item, 0.28);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("StairSetup", "SSetup", "Stair Setup Object", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double trH = 0.0;
            double trW = 0.0;

            if (!DA.GetData(0, ref trH)) return;
            if (!DA.GetData(1, ref trW)) return;

            var setup = new StairSetup(trH, trW);
            DA.SetData(0, setup);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                var assembly = typeof(Owl_StairSetup).Assembly;
                var resourceName = "Owl.Grasshopper.Icons.Owl_StairSetup_24.png";
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null) return null;
                    return new System.Drawing.Bitmap(stream);
                }
            }
        }
        public override Guid ComponentGuid => new Guid("22222222-2222-2222-2222-222222222222");
    }
}
