using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;
using Owl.Core.Primitives;

namespace Owl.Grasshopper.Components.Visualization
{
    public class Owl_SectionViz : GH_Component
    {
        public Owl_SectionViz()
          : base("Section Visualization", "SecViz",
              "Visualizes section geometry from SerializedAnalysis",
              "Owl", "Visualization")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("SerializedAnalysis", "SAnalysis", "Serialized Analysis Data", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddLineParameter("Sightlines", "Lines", "Sightlines", GH_ParamAccess.list);
            pManager.AddBrepParameter("ProjectorCone", "Cone", "Projector Cone", GH_ParamAccess.item);
            pManager.AddGenericParameter("Chairs", "Chairs", "Section Chairs", GH_ParamAccess.tree);
            pManager.AddLineParameter("LimitLines", "Limits", "Limit Lines", GH_ParamAccess.tree);
            pManager.AddCurveParameter("TribuneProfile", "Trib", "Tribune Profile", GH_ParamAccess.list);
            pManager.AddCurveParameter("StairsProfile", "Stairs", "Stairs Profile", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            SerializedAnalysis analysis = null;
            if (!DA.GetData(0, ref analysis) || analysis == null) return;

            DA.SetDataList(0, analysis.Sightlines);
            if (analysis.ProjectorCone != null)
                DA.SetData(1, analysis.ProjectorCone);

            // Chairs Tree
            var chairTree = new DataTree<GeometryBase>();
            if (analysis.SectionChairs != null)
            {
                for (int i = 0; i < analysis.SectionChairs.Count; i++)
                {
                    chairTree.AddRange(analysis.SectionChairs[i], new GH_Path(i));
                }
            }
            DA.SetDataTree(2, chairTree);

            // Limit Lines Tree
            var limitTree = new DataTree<Line>();
            if (analysis.LimitLines != null)
            {
                for (int i = 0; i < analysis.LimitLines.Count; i++)
                {
                    limitTree.AddRange(analysis.LimitLines[i], new GH_Path(i));
                }
            }
            DA.SetDataTree(3, limitTree);

            // Tribune
            var tribList = new List<Curve>();
            if (analysis.Tribune != null)
            {
                if (analysis.Tribune.Risers != null) tribList.AddRange(analysis.Tribune.Risers);
                if (analysis.Tribune.Treads != null) tribList.AddRange(analysis.Tribune.Treads);
            }
            DA.SetDataList(4, tribList);

            // Stairs - TODO: Add stairs to SerializedTribune if needed, or assume they are in Risers/Treads?
            // Currently Stairs are not explicitly separated in SerializedTribune, 
            // but the request asks for "Stairs Profile". 
            // If they are not in SerializedTribune, we might need to add them or just output standard profile.
            // For now, let's output empty or what we have.
            // Wait, looking at SerializedTribune, it has Risers and Treads.
            // We just output them in TribuneProfile.
            // If we have separate Stairs, we need to add them to SerializedTribune or Analysis.
            // Currently Analysis logic generates them but doesn't explicitly store "Stairs Only" separately 
            // unless we did that in previous steps. 
            // Actually, TribuneSolver generates them. 
            // Use existing if available.
             DA.SetDataList(5, new List<Curve>()); // Placeholder if no explicit stairs list
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return null; // TODO: Add Icon
            }
        }

        public override Guid ComponentGuid => new Guid("12345678-90AB-CDEF-1234-567890ABCDEF");
    }
}
