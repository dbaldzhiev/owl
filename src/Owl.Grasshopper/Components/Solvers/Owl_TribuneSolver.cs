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
            pManager.AddGenericParameter("TribuneSetup", "Trib", "Tribune Setup Object", GH_ParamAccess.item);          // 0
            pManager.AddGenericParameter("StairSetup", "Stair", "Stair Setup Object", GH_ParamAccess.item);             // 1
            pManager.AddGenericParameter("RailingSetup", "Rail", "Railing Setup Object", GH_ParamAccess.item);           // 2
            pManager.AddBooleanParameter("Flip", "Flip", "Flip the tribune (Right-to-Left)", GH_ParamAccess.item, false); // 3
            pManager.AddPointParameter("Origin", "Origin", "Origin point of the tribune", GH_ParamAccess.item, Point3d.Origin); // 4
            pManager.AddBooleanParameter("RailingToggles", "RailTogs", "Toggle railing per row", GH_ParamAccess.list);   // 5
            pManager.AddGenericParameter("AudienceSetup", "Audience", "Audience Setup List", GH_ParamAccess.list);       // 6
            pManager.AddNumberParameter("AudienceOffsets", "Offsets", "X offsets for chairs per row", GH_ParamAccess.list); // 7
            pManager.AddGenericParameter("HallSetup", "Hall", "Hall Setup for plan generation", GH_ParamAccess.item);     // 8

            pManager[5].Optional = true;
            pManager[6].Optional = true;
            pManager[7].Optional = true;
            pManager[8].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("SerializedTribune", "STrib", "Serialized Tribune Data", GH_ParamAccess.item);             // 0
            pManager.AddCurveParameter("SecTribune", "SecTrib", "Section tribune profile", GH_ParamAccess.item);                    // 1
            pManager.AddCurveParameter("SecStairs", "SecStairs", "Section stairs profile", GH_ParamAccess.item);                    // 2
            pManager.AddCurveParameter("SecRailings", "SecRails", "Section railing profiles", GH_ParamAccess.list);                 // 3
            pManager.AddPointParameter("SecRailingsSpine", "SecRSpine", "Section railing midpoint axis", GH_ParamAccess.list);      // 4
            pManager.AddPointParameter("SecRowSpine", "SecSpine", "Section row spine points", GH_ParamAccess.list);                 // 5
            pManager.AddCurveParameter("SecChairs", "SecChairs", "Section chair geometry", GH_ParamAccess.tree);                    // 6
            pManager.AddLineParameter("SecLimitLines", "SecLimits", "Section limit lines", GH_ParamAccess.tree);                    // 7
            pManager.AddCurveParameter("PlanTribune", "PlanTrib", "Plan tribune lines", GH_ParamAccess.list);                       // 8
            pManager.AddCurveParameter("PlanStairs", "PlanStairs", "Plan stair lines", GH_ParamAccess.list);                        // 9
            pManager.AddCurveParameter("PlanRailings", "PlanRails", "Plan railing lines", GH_ParamAccess.list);                     // 10
            pManager.AddCurveParameter("PlanRailingsSpine", "PlanRSpine", "Plan railing spine axis lines", GH_ParamAccess.list);     // 11
            pManager.AddCurveParameter("PlanChairs", "PlanChairs", "Plan chair geometry", GH_ParamAccess.tree);                     // 12
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            TribuneSetup tribune = null;
            StairSetup stairs = null;
            RailingSetup railings = null;
            bool flip = false;
            Point3d origin = Point3d.Origin;
            List<bool> railingToggles = new List<bool>();
            List<AudienceSetup> audiences = new List<AudienceSetup>();
            List<double> offsets = new List<double>();
            HallSetup hallSetup = null;

            if (!DA.GetData(0, ref tribune) || tribune == null) return;
            if (!DA.GetData(1, ref stairs) || stairs == null) return;
            if (!DA.GetData(2, ref railings) || railings == null) return;
            DA.GetData(3, ref flip);
            DA.GetData(4, ref origin);
            DA.GetDataList(5, railingToggles);
            DA.GetDataList(6, audiences);
            DA.GetDataList(7, offsets);
            DA.GetData(8, ref hallSetup);

            TribuneSolver solver = new TribuneSolver(tribune, stairs, railings);

            SerializedTribune strib;
            Curve secTrib, secStairs;
            List<Curve> secRails;
            List<Point3d> secRailSpine;
            List<Point3d> secRowSpine;
            List<List<Curve>> secChairs;
            List<List<Line>> secLimits;
            List<Curve> planTrib;
            List<Curve> planStairs;
            List<Curve> planRails;
            List<Curve> planRailSpine;
            List<List<Curve>> planChairs;

            solver.Solve(
                out strib, out secTrib, out secStairs,
                out secRails, out secRailSpine, out secRowSpine,
                out secChairs, out secLimits,
                out planTrib, out planStairs, out planRails, out planRailSpine, out planChairs,
                flip, origin, railingToggles, audiences, offsets, hallSetup);

            // Convert to DataTrees
            var chairTree = new DataTree<Curve>();
            for (int i = 0; i < secChairs.Count; i++)
                chairTree.AddRange(secChairs[i], new GH_Path(i));

            var limitTree = new DataTree<Line>();
            for (int i = 0; i < secLimits.Count; i++)
                limitTree.AddRange(secLimits[i], new GH_Path(i));

            var planChairTree = new DataTree<Curve>();
            for (int i = 0; i < planChairs.Count; i++)
                planChairTree.AddRange(planChairs[i], new GH_Path(i));

            DA.SetData(0, strib);
            DA.SetData(1, secTrib);
            DA.SetData(2, secStairs);
            DA.SetDataList(3, secRails);
            DA.SetDataList(4, secRailSpine);
            DA.SetDataList(5, secRowSpine);
            DA.SetDataTree(6, chairTree);
            DA.SetDataTree(7, limitTree);
            DA.SetDataList(8, planTrib);
            DA.SetDataList(9, planStairs);
            DA.SetDataList(10, planRails);
            DA.SetDataList(11, planRailSpine);
            DA.SetDataTree(12, planChairTree);
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
