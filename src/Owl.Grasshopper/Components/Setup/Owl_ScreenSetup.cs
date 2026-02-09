using System;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Owl.Core.Primitives;

namespace Owl.Grasshopper.Components.Setup
{
    public class Owl_ScreenSetup : GH_Component
    {
        public Owl_ScreenSetup()
          : base("Screen Setup", "Screen",
              "Define the screen geometry (curve).",
              "Owl", "Setup")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("ScreenCurve", "Crv", "Line curve representing the screen", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("ScreenSetup", "Screen", "Screen Setup Object", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve crv = null;
            if (!DA.GetData(0, ref crv)) return;

            var setup = new ScreenSetup(crv);
            DA.SetData(0, setup);
        }

        protected override System.Drawing.Bitmap Icon => null;
        public override Guid ComponentGuid => new Guid("4A7E8D21-1234-4567-8901-23456789ABCD"); // Random GUID
    }
}
