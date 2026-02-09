using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Owl.Core.Primitives;

namespace Owl.Grasshopper.Components.Setup
{
    public class Owl_AudienceSetup : GH_Component
    {
        public Owl_AudienceSetup()
          : base("Audience Setup", "Audience",
              "Define the audience setup connecting tribune, eye location, and chairs.",
              "Owl", "Setup")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("EyeLocation", "Eye", "Location of the eye relative to the origin", GH_ParamAccess.item);
            pManager.AddPointParameter("Origin", "Origin", "Origin of the chair-eye config", GH_ParamAccess.item);
            pManager.AddCurveParameter("Chairs", "Chairs", "2d curves geometry of the chairs (illustrative)", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("AudienceSetup", "Audience", "Audience Setup Object", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Point3d eye = Point3d.Unset;
            Point3d origin = Point3d.Unset;
            List<Curve> chairs = new List<Curve>();

            if (!DA.GetData(0, ref eye)) return;
            if (!DA.GetData(1, ref origin)) return;
            if (!DA.GetDataList(2, chairs)) return;

            var setup = new AudienceSetup(eye, origin, chairs);
            DA.SetData(0, setup);
        }

        protected override System.Drawing.Bitmap Icon => null;
        public override Guid ComponentGuid => new Guid("6C90AF43-3456-6789-0123-45678901CDEF"); // Random GUID
    }
}
