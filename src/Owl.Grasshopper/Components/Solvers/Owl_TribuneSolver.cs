using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Owl.Core.Primitives;
using Owl.Core.Solvers;

namespace Owl.Grasshopper.Components.Solvers
{
    public class Owl_TribuneSolver : GH_Component
    {
        public Owl_TribuneSolver()
          : base("Tribune Solver", "TribSolve",
              "Generate the 3D profile curves based on the setup objects.",
              "Owl", "Solvers")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("TribuneSetup", "Trib", "Tribune Setup Object", GH_ParamAccess.item);
            pManager.AddGenericParameter("StairSetup", "Stair", "Stair Setup Object", GH_ParamAccess.item);
            pManager.AddGenericParameter("RailingSetup", "Rail", "Railing Setup Object", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("TribuneProfile", "TProf", "Tribune Profile Curve", GH_ParamAccess.item);
            pManager.AddCurveParameter("StairsProfile", "SProf", "Stairs Profile Curve", GH_ParamAccess.item);
            pManager.AddCurveParameter("RailingProfiles", "RProf", "Railing Profile Curves", GH_ParamAccess.list);
            pManager.AddNumberParameter("Gaps", "Gaps", "Free gap calculation per row", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            TribuneSetup tSetup = null;
            StairSetup sSetup = null;
            RailingSetup rSetup = null;

            if (!DA.GetData(0, ref tSetup) || tSetup == null) return;
            if (!DA.GetData(1, ref sSetup) || sSetup == null) return;
            if (!DA.GetData(2, ref rSetup) || rSetup == null) return;

            var solver = new TribuneSolver(tSetup, sSetup, rSetup);
            
            Curve tProfile, sProfile;
            List<Curve> rProfiles;
            List<double> gaps;

            solver.Solve(out tProfile, out sProfile, out rProfiles, out gaps);

            DA.SetData(0, tProfile);
            DA.SetData(1, sProfile);
            DA.SetDataList(2, rProfiles);
            DA.SetDataList(3, gaps);
        }

        protected override System.Drawing.Bitmap Icon => null;
        public override Guid ComponentGuid => new Guid("44444444-4444-4444-4444-444444444444");
    }
}
