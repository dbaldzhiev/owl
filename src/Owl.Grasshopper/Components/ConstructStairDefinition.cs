using System;
using Grasshopper.Kernel;
using Owl.Core.Primitives;

namespace Owl.Grasshopper.Components
{
    public class ConstructStairDefinition : GH_Component
    {
        public ConstructStairDefinition()
          : base("Construct Stair Definition", "StairDef",
              "Creates a Stair Definition from Tread Height and Width",
              "Owl", "Primitives")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Tread Height", "TH", "Height of each stair step", GH_ParamAccess.item, 0.15);
            pManager.AddNumberParameter("Tread Width", "TW", "Width/Depth of each stair step", GH_ParamAccess.item, 0.28);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Stair Definition", "SD", "The resulting Stair Definition object", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double th = 0.15;
            double tw = 0.28;

            if (!DA.GetData(0, ref th)) return;
            if (!DA.GetData(1, ref tw)) return;

            if (th <= 0 || tw <= 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Dimensions must be positive.");
                return;
            }

            var definition = new OWL_StairDefinition(th, tw);
            DA.SetData(0, definition);
        }

        protected override System.Drawing.Bitmap Icon => null; // Todo: Add Icon

        public override Guid ComponentGuid => new Guid("9c5d2b1a-4e3f-4a5d-8b6c-7d8e9f0a1b2c");
    }
}
