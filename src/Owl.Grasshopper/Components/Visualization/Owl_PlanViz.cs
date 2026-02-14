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
              "Visualizes plan geometry from SerializedAnalysis.",
              "Owl", "Visualization")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("SerializedAnalysis", "Analysis", "Serialized Analysis Data from Audience Distributor", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("PlanTribune", "Trib", "Plan Tribune Lines", GH_ParamAccess.list);
            pManager.AddCurveParameter("PlanRailings", "Rail", "Plan Railing Lines", GH_ParamAccess.list);
            pManager.AddCurveParameter("PlanStairs", "Stair", "Plan Stair Lines", GH_ParamAccess.list);
            pManager.AddGenericParameter("PlanChairs", "Chairs", "Plan Distributed Chairs", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            SerializedAnalysis analysis = null;
            if (!DA.GetData(0, ref analysis) || analysis == null) return;

            DA.SetDataList(0, analysis.PlanTribune);
            DA.SetDataList(1, analysis.PlanRailings);
            DA.SetDataList(2, analysis.PlanStairs);

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
                // return Properties.Resources.IconForThisComponent;
                return null;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("66666666-7777-8888-9999-AAAAAAAAAAAA"); } // TODO: Generate unique GUID
        }
    }
}
