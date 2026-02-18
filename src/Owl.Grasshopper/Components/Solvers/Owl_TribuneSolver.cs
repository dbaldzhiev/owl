using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;
using Owl.Core.Primitives;
using Owl.Core.Solvers;

namespace Owl.Grasshopper.Components.Solvers
{
    public class Owl_TribuneSolver : GH_Component
    {
        public Owl_TribuneSolver()
          : base("Tribune Solver", "TribSolve",
              "Generate section and plan geometry for the tribune.",
              "Owl", "Solvers")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("StairTribuneSetup", "Setup", "Stair and Tribune Setup Object", GH_ParamAccess.item); // 0
            pManager.AddGenericParameter("RailingSetup", "Rail", "Railing Setup Object", GH_ParamAccess.item);           // 1
            pManager.AddBooleanParameter("Flip", "Flip", "Flip the tribune (Right-to-Left)", GH_ParamAccess.item, false); // 2
            pManager.AddBooleanParameter("RailingToggles", "RailTogs", "Toggle railing per row", GH_ParamAccess.list);   // 3
            pManager.AddGenericParameter("AudienceSetup", "Audience", "Audience Setup List", GH_ParamAccess.list);       // 4
            pManager.AddNumberParameter("AudienceOffsets", "Offsets", "X offsets for chairs per row", GH_ParamAccess.list); // 5
            pManager.AddGenericParameter("HallSetup", "Hall", "Hall Setup for plan generation", GH_ParamAccess.item);     // 6

            pManager[3].Optional = true;
            pManager[4].Optional = true;
            pManager[5].Optional = true;
            pManager[6].Optional = true;
        }

            protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("TribuneSolution", "Sol", "Tribune Solution container", GH_ParamAccess.item);
            pManager.AddTextParameter("Errors", "Err", "Validation Errors", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            StairTribuneSetup stSetup = null;
            RailingSetup railings = null;
            bool flip = false;
            List<bool> railingToggles = new List<bool>();
            List<AudienceSetup> audiences = new List<AudienceSetup>();
            List<double> offsets = new List<double>();
            HallSetup hallSetup = null;

            if (!DA.GetData(0, ref stSetup) || stSetup == null) return;
            if (!DA.GetData(1, ref railings) || railings == null) return;
            DA.GetData(2, ref flip);
            DA.GetDataList(3, railingToggles);
            DA.GetDataList(4, audiences);
            DA.GetDataList(5, offsets);
            DA.GetData(6, ref hallSetup);

            TribuneSolver solver = new TribuneSolver(stSetup, railings);

            TribuneSolution solution = solver.Solve(
                flip, 
                railingToggles, 
                audiences, 
                offsets, 
                hallSetup
            );

            DA.SetData(0, solution);
            DA.SetDataList(1, solution.Errors);
            if (solution.Errors.Count > 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Solution has errors.");
            }
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                var assembly = typeof(Owl_TribuneSolver).Assembly;
                var resourceName = "Owl.Grasshopper.Icons.Owl_TribuneSolver_24.png";
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null) return null;
                    return new System.Drawing.Bitmap(stream);
                }
            }
        }
        public override Guid ComponentGuid => new Guid("44444444-4444-4444-4444-444444444444");
    }
}
