using System;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Owl.Core.Primitives;

namespace Owl.Grasshopper.Components.Setup
{
    public class Owl_ProjectorSetup : GH_Component
    {
        public Owl_ProjectorSetup()
          : base("Projector Setup", "Projector",
              "Define the projector location.",
              "Owl", "Setup")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Location", "Loc", "Projector location point", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("ProjectorSetup", "Projector", "Projector Setup Object", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Point3d loc = Point3d.Unset;
            if (!DA.GetData(0, ref loc)) return;

            var setup = new ProjectorSetup(loc);
            DA.SetData(0, setup);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                var assembly = typeof(Owl_ProjectorSetup).Assembly;
                var resourceName = "Owl.Grasshopper.Icons.Owl_ProjectorSetup_24.png";
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null) return null;
                    return new System.Drawing.Bitmap(stream);
                }
            }
        }
        public override Guid ComponentGuid => new Guid("5B8F9E32-2345-5678-9012-34567890BCDE"); // Random GUID
    }
}
