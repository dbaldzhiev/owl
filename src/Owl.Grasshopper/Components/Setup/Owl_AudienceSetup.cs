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
            pManager.AddPointParameter("EyePoint", "Eye", "Location of the eye relative to the origin", GH_ParamAccess.item, new Point3d(155, 0, 97));
            pManager.AddPlaneParameter("SectionFrame", "SecFrame", "Section origin plane of the chair-eye config", GH_ParamAccess.item, Plane.WorldXY);
            pManager.AddCurveParameter("ChairProfile", "ChairProf", "2D section curves of the chairs", GH_ParamAccess.list);
            pManager.AddNumberParameter("FrontLimit", "FL", "Section front limit (default 45)", GH_ParamAccess.item, 45.0);
            pManager.AddNumberParameter("HardBackLimit", "HBL", "Section hard-back limit (default 182.5)", GH_ParamAccess.item, 182.5);
            pManager.AddNumberParameter("SoftBackLimit", "SBL", "Section soft-back limit (default 200)", GH_ParamAccess.item, 200.0);
            pManager.AddCurveParameter("ChairPlan", "PlanProf", "Plan view chair curves", GH_ParamAccess.list);
            pManager.AddPlaneParameter("PlanFrame", "PlanFrame", "Plan view chair origin plane", GH_ParamAccess.item, Plane.WorldXY);
            pManager.AddNumberParameter("ChairWidth", "Width", "Plan view chair width", GH_ParamAccess.item, 500.0);

            pManager[2].Optional = true;   // ChairProfile
            pManager[6].Optional = true;   // ChairPlan
            pManager[7].Optional = true;   // PlanFrame
            pManager[8].Optional = true;   // ChairWidth
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("AudienceSetup", "Audience", "Audience Setup Object", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Point3d eye = new Point3d(155, 0, 97);
            Plane secOrigin = Plane.WorldXY;
            List<Curve> secChairs = new List<Curve>();
            double secFL = 45.0;
            double secHBL = 182.5;
            double secSBL = 200.0;
            List<Curve> planChairs = new List<Curve>();
            Plane planOrigin = Plane.WorldXY;
            double planWidth = 500.0;

            DA.GetData(0, ref eye);
            DA.GetData(1, ref secOrigin);
            DA.GetDataList(2, secChairs);
            DA.GetData(3, ref secFL);
            DA.GetData(4, ref secHBL);
            DA.GetData(5, ref secSBL);
            DA.GetDataList(6, planChairs);
            DA.GetData(7, ref planOrigin);
            DA.GetData(8, ref planWidth);

            var setup = new AudienceSetup(eye, secOrigin, secChairs, secFL, secHBL, secSBL, planChairs, planOrigin, planWidth);
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
        public override Guid ComponentGuid => new Guid("6C90AF43-3456-6789-0123-45678901CDEF");
    }
}
