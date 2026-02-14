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
            pManager.AddNumberParameter("FrontLimit", "FLimit", "Front limit (default 45)", GH_ParamAccess.item, 45.0);
            pManager.AddNumberParameter("HardBackLimit", "HBLimit", "Hard-back limit (default 182.5)", GH_ParamAccess.item, 182.5);
            pManager.AddNumberParameter("SoftBackLimit", "SBLimit", "Soft-back limit (default 200)", GH_ParamAccess.item, 200.0);
            pManager.AddGeometryParameter("PlanGeo", "PGeo", "Plan geometry of the chair", GH_ParamAccess.item);
            pManager.AddPointParameter("PlanOriginPt", "POrigin", "Origin point in plan (center of chair)", GH_ParamAccess.item, Point3d.Origin);
            pManager.AddNumberParameter("ChairWidth", "Width", "Width of the chair for distribution", GH_ParamAccess.item, 55.0);
            
            pManager[2].Optional = true; // Chairs can be optional
            pManager[6].Optional = true;
            pManager[7].Optional = true;
            pManager[8].Optional = true;
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
            double flimit = 45.0;
            double hblimit = 182.5;
            double sblimit = 200.0;
            GeometryBase planGeo = null;
            Point3d planOrigin = Point3d.Origin;
            double chairWidth = 55.0;

            DA.GetData(0, ref eye);
            DA.GetData(1, ref origin);
            DA.GetDataList(2, chairs);
            DA.GetData(3, ref flimit);
            DA.GetData(4, ref hblimit);
            DA.GetData(5, ref sblimit);
            DA.GetData(6, ref planGeo);
            DA.GetData(7, ref planOrigin);
            DA.GetData(8, ref chairWidth);

            var setup = new AudienceSetup(eye, origin, chairs, flimit, hblimit, sblimit)
            {
                PlanGeo = planGeo,
                PlanOriginPt = planOrigin,
                ChairWidth = chairWidth
            };
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
