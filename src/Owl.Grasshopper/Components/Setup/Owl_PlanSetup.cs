using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Owl.Core.Primitives;

namespace Owl.Grasshopper.Components.Setup
{
    public class Owl_PlanSetup : GH_Component
    {
        public Owl_PlanSetup()
          : base("Plan Setup", "PlanSetup",
              "Define the plan boundaries and aisles for chair distribution.",
              "Owl", "Setup")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Origin", "Origin", "Origin of the plan that corresponds to the section origin", GH_ParamAccess.item, Point3d.Origin);
            pManager.AddCurveParameter("TribuneBoundary", "Boundary", "Closed polygon representing the tribune boundary in plan", GH_ParamAccess.item);
            pManager.AddCurveParameter("AisleBoundaries", "Aisles", "List of closed planar polygons/curves for aisles", GH_ParamAccess.list);
            pManager.AddCurveParameter("TunnelBoundary", "Tunnel", "Closed polygon representing the tunnel boundary (void) in plan", GH_ParamAccess.item);
            
            pManager[2].Optional = true;
            pManager[3].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("SerializedPlanSetup", "SPlan", "Serialized Plan Setup JSON", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Point3d origin = Point3d.Origin;
            Curve boundary = null;
            List<Curve> aisles = new List<Curve>();
            Curve tunnel = null;

            if (!DA.GetData(0, ref origin)) return;
            if (!DA.GetData(1, ref boundary)) return;
            DA.GetDataList(2, aisles);
            DA.GetData(3, ref tunnel);

            var setup = new PlanSetup(origin, boundary, aisles, tunnel);
            DA.SetData(0, setup.ToJson());
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                var assembly = typeof(Owl_PlanSetup).Assembly;
                // Using AudienceSetup icon for now as a placeholder
                var resourceName = "Owl.Grasshopper.Icons.Owl_AudienceSetup_24.png";
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null) return null;
                    return new System.Drawing.Bitmap(stream);
                }
            }
        }

        public override Guid ComponentGuid => new Guid("B2C3D4E5-F678-9012-3456-7890ABCDEF12");
    }
}
