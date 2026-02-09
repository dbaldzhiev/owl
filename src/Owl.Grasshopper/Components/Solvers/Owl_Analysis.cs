using System;
using System.Collections.Generic;
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
              "Calculate sightlines, projector cone, and distribute chairs.",
              "Owl", "Solvers")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("AudienceSetup", "Audience", "Audience Setup Object", GH_ParamAccess.item);
            pManager.AddGenericParameter("SerializedTribune", "STrib", "Serialized Tribune Output", GH_ParamAccess.item);
            pManager.AddGenericParameter("ProjectorSetup", "Projector", "Projector Setup Object", GH_ParamAccess.item);
            pManager.AddGenericParameter("ScreenSetup", "Screen", "Screen Setup Object", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddLineParameter("Sightlines", "Lines", "Sightlines from eyes to screen bottom", GH_ParamAccess.list);
            pManager.AddBrepParameter("ProjectorCone", "Cone", "Projector cone geometry", GH_ParamAccess.item);
            pManager.AddCurveParameter("Chairs", "Chairs", "Distributed chair geometry", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            AudienceSetup audience = null;
            SerializedTribune strib = null;
            ProjectorSetup projector = null;
            ScreenSetup screen = null;

            if (!DA.GetData(0, ref audience) || audience == null) return;
            if (!DA.GetData(1, ref strib) || strib == null) return;
            if (!DA.GetData(2, ref projector) || projector == null) return;
            if (!DA.GetData(3, ref screen) || screen == null) return;

            List<Line> sightlines;
            Brep cone;
            List<Curve> chairs;

            Analysis.Calculate(audience, strib, screen, projector, out sightlines, out cone, out chairs);

            DA.SetDataList(0, sightlines);
            DA.SetData(1, cone);
            DA.SetDataList(2, chairs);
        }

        protected override System.Drawing.Bitmap Icon => null;
        public override Guid ComponentGuid => new Guid("8E12C163-5678-8901-2345-67890123EFAB");
    }
}
