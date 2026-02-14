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
              "Generate the 3D profile curves, place chairs, and compute limits.",
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
            pManager.AddBooleanParameter("RailingToggles", "RailTogs", "List of booleans to toggle railing per row", GH_ParamAccess.list);
            pManager.AddGenericParameter("AudienceSetup", "Audience", "Audience Setup List", GH_ParamAccess.list);
            pManager.AddNumberParameter("AudienceOffsets", "Offsets", "List of X offsets for chairs per row", GH_ParamAccess.list);

            pManager[5].Optional = true;
            pManager[6].Optional = true;
            pManager[7].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("TribuneProfile", "Profile", "Profile of the tribune", GH_ParamAccess.item);           // 0
            pManager.AddCurveParameter("StairsProfile", "Stairs", "Profile of the stairs", GH_ParamAccess.item);              // 1
            pManager.AddCurveParameter("RailingProfiles", "Railings", "Profiles of the railings", GH_ParamAccess.list);       // 2
            pManager.AddPointParameter("RailMidpoints", "RailMid", "Midpoint axis of each railing (top, mid-thickness)", GH_ParamAccess.list); // 3
            pManager.AddLineParameter("TribRows", "Rows", "Tribune Row Lines", GH_ParamAccess.list);                         // 4
            pManager.AddPointParameter("RowSpine", "Spine", "Row-Railing intersection (railing ON) or previous row hard limit intersection (railing OFF)", GH_ParamAccess.list); // 5
            pManager.AddCurveParameter("Chairs", "Chairs", "Distributed chair geometry", GH_ParamAccess.tree);                // 6
            pManager.AddLineParameter("LimitLines", "Limits", "Vertical lines for front and back limits", GH_ParamAccess.tree); // 7
            pManager.AddGenericParameter("SerializedTribune", "STrib", "Serialized Tribune Data", GH_ParamAccess.item);        // 8
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

            if (!DA.GetData(0, ref tribune) || tribune == null) return;
            if (!DA.GetData(1, ref stairs) || stairs == null) return;
            if (!DA.GetData(2, ref railings) || railings == null) return;
            DA.GetData(3, ref flip);
            DA.GetData(4, ref origin);
            DA.GetDataList(5, railingToggles);
            DA.GetDataList(6, audiences);
            DA.GetDataList(7, offsets);

            TribuneSolver solver = new TribuneSolver(tribune, stairs, railings);
            Curve tripP;
            Curve stairsP;
            List<Curve> railsP;
            List<Point3d> railMidpoints;
            SerializedTribune strib;
            List<Line> tribRows;
            List<Point3d> rowSpine;
            List<List<Curve>> chairs;
            List<List<Line>> limits;

            solver.Solve(
                out tripP, out stairsP, out railsP, out railMidpoints,
                out strib, out tribRows, out rowSpine,
                out chairs, out limits,
                flip, origin, railingToggles, audiences, offsets);

            // Convert to DataTrees
            var chairTree = new DataTree<Curve>();
            for (int i = 0; i < chairs.Count; i++)
            {
                chairTree.AddRange(chairs[i], new GH_Path(i));
            }

            var limitTree = new DataTree<Line>();
            for (int i = 0; i < limits.Count; i++)
            {
                limitTree.AddRange(limits[i], new GH_Path(i));
            }

            DA.SetData(0, tripP);
            DA.SetData(1, stairsP);
            DA.SetDataList(2, railsP);
            DA.SetDataList(3, railMidpoints);
            DA.SetDataList(4, tribRows);
            DA.SetDataList(5, rowSpine);
            DA.SetDataTree(6, chairTree);
            DA.SetDataTree(7, limitTree);
            DA.SetData(8, strib);
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
