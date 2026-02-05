using System;
using System.Drawing;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Owl.Core.Primitives;

namespace Owl.Grasshopper.Components
{
    public class TribuneFromCurve : GH_Component
    {
        public TribuneFromCurve()
          : base("Tribune Line from Curve", "TribLine",
              "Constructs a Tribune Line primitive from a base curve",
              "Owl", "Primitives")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curve", "C", "Base curve for the tribune", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            // Using Generic parameter for now as we don't have a specific Goo wrapper yet
            pManager.AddGenericParameter("TribuneLine", "T", "Resulting Tribune Line primitive", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve crv = null;
            if (!DA.GetData(0, ref crv)) return;

            if (!crv.IsValid)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid curve supplied.");
                return;
            }

            var tribune = new OWL_TribuneLine(crv);

            DA.SetData(0, tribune);
        }

        protected override System.Drawing.Bitmap Icon => null;

        public override Guid ComponentGuid => new Guid("6b02008b-62bf-466d-8e4f-6e83d9370014");
    }
}
