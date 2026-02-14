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
    public class Owl_AudienceDistributor : GH_Component
    {
        public Owl_AudienceDistributor()
          : base("Audience Distributor", "AudDist",
              "Calculates sightlines, projector cone, and distributes chairs with optional alignment.",
              "Owl", "Solvers")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("AudienceSetup", "Audience", "Audience Setup Object", GH_ParamAccess.list);
            pManager.AddGenericParameter("SerializedTribune", "STrib", "Serialized Tribune Output", GH_ParamAccess.item);
            pManager.AddGenericParameter("ProjectorSetup", "Projector", "Projector Setup Object", GH_ParamAccess.item);
            pManager.AddGenericParameter("ScreenSetup", "Screen", "Screen Setup Object", GH_ParamAccess.item);
            pManager.AddNumberParameter("AudienceOffsets", "Offsets", "List of X offsets for chairs and eyes per row", GH_ParamAccess.list);
            pManager.AddGenericParameter("PlanSetup", "Plan", "Serialized Plan Setup Object", GH_ParamAccess.item);
            
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
            pManager[5].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddLineParameter("Sightlines", "Lines", "Sightlines from eyes to screen bottom", GH_ParamAccess.list);
            pManager.AddBrepParameter("ProjectorCone", "Cone", "Projector cone geometry", GH_ParamAccess.item);
            pManager.AddGenericParameter("Chairs", "Chairs", "Distributed chair geometry (Section)", GH_ParamAccess.tree);
            pManager.AddLineParameter("LimitLines", "Limits", "Vertical lines for front and back limits", GH_ParamAccess.tree);
            pManager.AddGenericParameter("SerializedAnalysis", "SAnalisys", "Serialized Analysis Data (for validation)", GH_ParamAccess.item);
            pManager.AddCurveParameter("PlanTribune", "PlanTrib", "Plan Tribune Lines", GH_ParamAccess.list);
            pManager.AddCurveParameter("PlanRailings", "PlanRail", "Plan Railing Lines", GH_ParamAccess.list);
            pManager.AddCurveParameter("PlanStairs", "PlanStair", "Plan Stair Lines", GH_ParamAccess.list);
            pManager.AddGenericParameter("PlanChairs", "PlanChairs", "Plan Distributed Chair Geometry (Blocks/Curves)", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<AudienceSetup> audiences = new List<AudienceSetup>();
            SerializedTribune strib = null;
            ProjectorSetup projector = null;
            ScreenSetup screen = null;
            List<double> offsets = new List<double>();
            PlanSetup plan = null;

            if (!DA.GetDataList(0, audiences) || audiences.Count == 0) return;
            if (!DA.GetData(1, ref strib) || strib == null) return;
            DA.GetData(2, ref projector);
            DA.GetData(3, ref screen);
            DA.GetDataList(4, offsets);
            DA.GetData(5, ref plan);
            
            List<Line> sightlines;
            List<List<Line>> limitLines;
            Brep cone;
            List<List<GeometryBase>> chairs;
            List<Curve> planTribune;
            List<Curve> planRailings;
            List<Curve> planStairs;

            Analysis.Calculate(audiences, strib, screen, projector, offsets, plan, 
                out sightlines, 
                out limitLines, 
                out cone, 
                out chairs,
                out planTribune,
                out planRailings,
                out planStairs);

            var serializedAnalysis = new SerializedAnalysis(strib, audiences, sightlines, offsets, plan);

            // Convert to DataTrees
            var limitTree = new DataTree<Line>();
            for (int i = 0; i < limitLines.Count; i++)
            {
                limitTree.AddRange(limitLines[i], new GH_Path(i));
            }

            var chairTree = new DataTree<GeometryBase>();
            for (int i = 0; i < chairs.Count; i++)
            {
                chairTree.AddRange(chairs[i], new GH_Path(i));
            }

            DA.SetDataList(0, sightlines);
            DA.SetData(1, cone);
            DA.SetDataTree(2, chairTree); // This might contain Plan Chairs or Section Chairs depending on what Analysis.Calculate put in there. 
            // In my implementation of Calculate, 'placedChairs' contains the Plan chairs if plan is valid, or Section chairs if not.
            // So output 2 is actually the main "Chairs" output.
            DA.SetDataTree(3, limitTree);
            DA.SetData(4, serializedAnalysis);
            DA.SetDataList(5, planTribune);
            DA.SetDataList(6, planRailings);
            DA.SetDataList(7, planStairs);
            DA.SetDataTree(8, chairTree); // Duplicating for explicit "PlanChairs" output, or just pointing user to generic chairs?
            // User requested explicit visualization logic.
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                var assembly = typeof(Owl_AudienceDistributor).Assembly;
                // Keeping old icon for now, or should check if new one exists?
                var resourceName = "Owl.Grasshopper.Icons.Owl_Analysis_24.png"; 
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null) return null;
                    return new System.Drawing.Bitmap(stream);
                }
            }
        }
        public override Guid ComponentGuid => new Guid("8E12C163-5678-8901-2345-67890123EFAB"); // Keep existing GUID to maintain connection if possible? Or new one? Usually safest to keep if renaming in place.
    }
}
