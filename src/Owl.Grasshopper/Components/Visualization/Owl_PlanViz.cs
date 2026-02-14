using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;
using Owl.Core.Primitives;

namespace Owl.Grasshopper.Components.Visualization
{
    public class Owl_PlanViz : GH_Component
    {
        public Owl_PlanViz()
          : base("Plan Visualization", "PlanViz",
              "Visualizes plan geometry from SerializedAnalysis",
              "Owl", "Visualization")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("SerializedAnalysis", "SAnalysis", "Serialized Analysis Data", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("PlanTribune", "Trib", "Plan Tribune", GH_ParamAccess.list);
            pManager.AddCurveParameter("PlanRailings", "Rails", "Plan Railings", GH_ParamAccess.list);
            pManager.AddCurveParameter("PlanStairs", "Stairs", "Plan Stairs", GH_ParamAccess.list);
            pManager.AddGenericParameter("PlanChairs", "Chairs", "Plan Chairs", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            SerializedAnalysis analysis = null;
            if (!DA.GetData(0, ref analysis) || analysis == null) return;

            DA.SetDataList(0, analysis.PlanTribuneLines);
            DA.SetDataList(1, analysis.PlanRailingLines);
            DA.SetDataList(2, analysis.PlanStairLines);

            var chairTree = new DataTree<GeometryBase>();
            if (analysis.PlanChairs != null)
            {
                for (int i = 0; i < analysis.PlanChairs.Count; i++)
                {
                    chairTree.AddRange(analysis.PlanChairs[i], new GH_Path(i));
                }
            }
            DA.SetDataTree(3, chairTree);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return null; // TODO: Add Icon
            }
        }

        public override Guid ComponentGuid => new Guid("98765432-10FE-DCBA-9876-543210FEDCBA");
    }
}
