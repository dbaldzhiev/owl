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
            pManager.AddPointParameter("EyeLocation", "Eye", "Location of the eye relative to the origin", GH_ParamAccess.item, new Point3d(155, 0, 97));
            pManager.AddPointParameter("Origin", "Origin", "Origin of the chair-eye config", GH_ParamAccess.item, Point3d.Origin);
            pManager.AddCurveParameter("Chairs", "Chairs", "2d curves geometry of the chairs (illustrative)", GH_ParamAccess.list);
            
            pManager[2].Optional = true; // Chairs can be optional
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("AudienceSetup", "Audience", "Audience Setup Object", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Point3d eye = new Point3d(155, 0, 97);
            Point3d origin = Point3d.Origin;
            List<Curve> chairs = new List<Curve>();

            DA.GetData(0, ref eye);
            DA.GetData(1, ref origin);
            DA.GetDataList(2, chairs);

            var setup = new AudienceSetup(eye, origin, chairs);
            DA.SetData(0, setup);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                var assembly = typeof(Owl_AudienceSetup).Assembly;
                var resourceName = "Owl.Grasshopper.Icons.Owl_AudienceSetup_24.png";
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null) return null;
                    return new System.Drawing.Bitmap(stream);
                }
            }
        }
        public override Guid ComponentGuid => new Guid("6C90AF43-3456-6789-0123-45678901CDEF"); // Random GUID
    }
}
