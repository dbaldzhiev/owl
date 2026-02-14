using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Owl.Core.Primitives;
using Owl.Core.Solvers;

namespace Owl.Grasshopper.Components.Solvers
{
    public class Owl_Analysis : GH_Component
    {
        public Owl_Analysis()
          : base("Analysis", "Analysis",
              "Calculates sightlines and projector cone from tribune, screen, and projector data.",
              "Owl", "Solvers")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("AudienceSetup", "Audience", "Audience Setup Object", GH_ParamAccess.list);
            pManager.AddGenericParameter("SerializedTribune", "STrib", "Serialized Tribune Output", GH_ParamAccess.item);
            pManager.AddGenericParameter("ProjectorSetup", "Projector", "Projector Setup Object", GH_ParamAccess.item);
            pManager.AddGenericParameter("ScreenSetup", "Screen", "Screen Setup Object", GH_ParamAccess.item);
            pManager.AddNumberParameter("AudienceOffsets", "Offsets", "List of X offsets for eyes per row", GH_ParamAccess.list);

            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddLineParameter("Sightlines", "Lines", "Sightlines from eyes to screen bottom", GH_ParamAccess.list);
            pManager.AddBrepParameter("ProjectorCone", "Cone", "Projector cone geometry", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<AudienceSetup> audiences = new List<AudienceSetup>();
            SerializedTribune strib = null;
            ProjectorSetup projector = null;
            ScreenSetup screen = null;
            List<double> offsets = new List<double>();

            if (!DA.GetDataList(0, audiences) || audiences.Count == 0) return;
            if (!DA.GetData(1, ref strib) || strib == null) return;
            DA.GetData(2, ref projector);
            DA.GetData(3, ref screen);
            DA.GetDataList(4, offsets);

            List<Line> sightlines;
            Brep cone;

            Analysis.Calculate(strib, audiences, screen, projector, offsets, out sightlines, out cone);

            DA.SetDataList(0, sightlines);
            DA.SetData(1, cone);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                var assembly = typeof(Owl_Analysis).Assembly;
                var resourceName = "Owl.Grasshopper.Icons.Owl_Analysis_24.png";
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null) return null;
                    return new System.Drawing.Bitmap(stream);
                }
            }
        }
        public override Guid ComponentGuid => new Guid("8E12C163-5678-8901-2345-67890123EFAB");
    }
}
