using System;
using Grasshopper.Kernel;
using Owl.Core.Primitives;

namespace Owl.Grasshopper.Components.Setup
{
    public class Owl_RailingSetup : GH_Component
    {
        public Owl_RailingSetup()
          : base("Railing Setup", "RailSetup",
              "Define railing dimensions.",
              "Owl", "Setup")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("RailHeight", "RailH", "Height of railing", GH_ParamAccess.item, 1.0);
            pManager.AddNumberParameter("RailWidth", "RailW", "Width/Thickness of railing", GH_ParamAccess.item, 0.05);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("RailingSetup", "RSetup", "Railing Setup Object", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double rH = 0.0;
            double rW = 0.0;

            if (!DA.GetData(0, ref rH)) return;
            if (!DA.GetData(1, ref rW)) return;

            var setup = new RailingSetup(rH, rW);
            DA.SetData(0, setup);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                var assembly = typeof(Owl_RailingSetup).Assembly;
                var resourceName = "Owl.Grasshopper.Icons.Owl_RailingSetup_24.png";
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null) return null;
                    return new System.Drawing.Bitmap(stream);
                }
            }
        }
        public override Guid ComponentGuid => new Guid("33333333-3333-3333-3333-333333333333");
    }
}
