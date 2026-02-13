using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Owl.Core.Primitives;
using Owl.Core.Solvers;

namespace Owl.Grasshopper.Components.Optimization
{
    public class Owl_Optimizer : GH_Component
    {
        public Owl_Optimizer()
          : base("Tribune Optimizer", "TribOpt",
              "Calculate the fitness of a tribune layout based on visibility and projector clearance.",
              "Owl", "Optimization")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("TribuneSetup", "Trib", "Tribune Setup Object", GH_ParamAccess.item);
            pManager.AddGenericParameter("StairSetup", "Stair", "Stair Setup Object", GH_ParamAccess.item);
            pManager.AddGenericParameter("RailingSetup", "Rail", "Railing Setup Object", GH_ParamAccess.item);
            pManager.AddGenericParameter("AudienceSetup", "Audience", "Audience Setup List", GH_ParamAccess.list);
            pManager.AddGenericParameter("ScreenSetup", "Screen", "Screen Setup Object", GH_ParamAccess.item);
            pManager.AddGenericParameter("ProjectorSetup", "Projector", "Projector Setup Object", GH_ParamAccess.item);
            
            pManager.AddBooleanParameter("Flip", "Flip", "Flip the tribune (Right-to-Left)", GH_ParamAccess.item, false);
            pManager.AddPointParameter("Origin", "Origin", "Origin point", GH_ParamAccess.item, Point3d.Origin);
            pManager.AddBooleanParameter("RailingToggles", "RailTogs", "Railing Toggle List", GH_ParamAccess.list);
            pManager.AddNumberParameter("AudienceOffsets", "AudOffsets", "Audience X Offsets", GH_ParamAccess.list);

            pManager[6].Optional = true; // Flip
            pManager[7].Optional = true; // Origin
            pManager[8].Optional = true; // BailTogs
            pManager[9].Optional = true; // AudOffsets
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Fitness", "Fit", "Fitness Score (Higher is better)", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            TribuneSetup tribune = null;
            StairSetup stairs = null;
            RailingSetup railings = null;
            List<AudienceSetup> audiences = new List<AudienceSetup>();
            ScreenSetup screen = null;
            ProjectorSetup projector = null;
            bool flip = false;
            Point3d origin = Point3d.Origin;
            List<bool> railingToggles = new List<bool>();
            List<double> audienceOffsets = new List<double>();

            if (!DA.GetData(0, ref tribune) || tribune == null) return;
            if (!DA.GetData(1, ref stairs) || stairs == null) return;
            if (!DA.GetData(2, ref railings) || railings == null) return;
            if (!DA.GetDataList(3, audiences) || audiences.Count == 0) return;
            if (!DA.GetData(4, ref screen) || screen == null) return;
            if (!DA.GetData(5, ref projector) || projector == null) return;
            
            DA.GetData(6, ref flip);
            DA.GetData(7, ref origin);
            DA.GetDataList(8, railingToggles);
            DA.GetDataList(9, audienceOffsets);

            double fitness = Optimization.Evaluate(
                tribune, 
                stairs, 
                railings, 
                audiences, 
                screen, 
                projector, 
                flip, 
                origin, 
                railingToggles, 
                audienceOffsets
            );

            DA.SetData(0, fitness);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // Return null or default for now
                return null; 
            }
        }

        public override Guid ComponentGuid => new Guid("A1B2C3D4-E5F6-7890-1234-567890ABCDEF");
    }
}
