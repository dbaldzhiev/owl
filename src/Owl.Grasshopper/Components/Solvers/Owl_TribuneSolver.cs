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
            pManager.AddBooleanParameter("Flip", "Flip", "Flip the tribune (Right-to-Left)", GH_ParamAccess.item, false);
            pManager.AddPointParameter("Origin", "Origin", "Origin point of the tribune", GH_ParamAccess.item, Point3d.Origin);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("TribuneProfile", "Profile", "Profile of the tribune", GH_ParamAccess.item);
            pManager.AddCurveParameter("StairsProfile", "Stairs", "Profile of the stairs", GH_ParamAccess.item);
            pManager.AddCurveParameter("RailingProfiles", "Railings", "Profiles of the railings", GH_ParamAccess.list);
            pManager.AddNumberParameter("Gaps", "Gaps", "Gap distances", GH_ParamAccess.list);
            pManager.AddLineParameter("TribRows", "Rows", "Tribune Row Lines", GH_ParamAccess.list);
            pManager.AddPointParameter("RRint", "RRint", "Row-Railing Intersection Points", GH_ParamAccess.list);
            pManager.AddGenericParameter("SerializedTribune", "STrib", "Serialized Tribune Data", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            TribuneSetup tribune = null;
            StairSetup stairs = null;
            RailingSetup railings = null;
            bool flip = false;
            Point3d origin = Point3d.Origin;

            if (!DA.GetData(0, ref tribune) || tribune == null) return;
            if (!DA.GetData(1, ref stairs) || stairs == null) return;
            if (!DA.GetData(2, ref railings) || railings == null) return;
            DA.GetData(3, ref flip);
            DA.GetData(4, ref origin);

            TribuneSolver solver = new TribuneSolver(tribune, stairs, railings);
            Curve tripP;
            Curve stairsP;
            List<Curve> railsP;
            List<double> gaps;
            SerializedTribune strib;
            List<Line> tribRows;
            List<Point3d> rrInt;

            solver.Solve(out tripP, out stairsP, out railsP, out gaps, out strib, out tribRows, out rrInt, flip, origin);

            DA.SetData(0, tripP);
            DA.SetData(1, stairsP);
            DA.SetDataList(2, railsP);
            DA.SetDataList(3, gaps);
            DA.SetDataList(4, tribRows);
            DA.SetDataList(5, rrInt);
            DA.SetData(6, strib);
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
