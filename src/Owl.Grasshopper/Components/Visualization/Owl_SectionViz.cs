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
          : base("Section Visualization", "SectViz",
              "Visualizes section geometry from SerializedAnalysis.",
              "Owl", "Visualization")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("SerializedAnalysis", "Analysis", "Serialized Analysis Data from Audience Distributor", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddLineParameter("Sightlines", "Sight", "Sightlines from eye to focal point", GH_ParamAccess.list);
            pManager.AddBrepParameter("ProjectorCone", "Cone", "Projector cone geometry", GH_ParamAccess.item);
            pManager.AddGenericParameter("SectionChairs", "Chairs", "Chairs distributed in section", GH_ParamAccess.tree);
            pManager.AddLineParameter("LimitLines", "Limits", "Front/Back limit lines", GH_ParamAccess.tree);
            pManager.AddCurveParameter("TribuneProfile", "Trib", "Tribune Section Profile", GH_ParamAccess.item);
            pManager.AddCurveParameter("StairsProfile", "Stair", "Stairs Section Profile", GH_ParamAccess.item);
            pManager.AddCurveParameter("RailingProfiles", "Rail", "Railing Section Profiles", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            SerializedAnalysis analysis = null;
            if (!DA.GetData(0, ref analysis) || analysis == null) return;

            // 1. Sightlines
            DA.SetDataList(0, analysis.Sightlines);

            // 2. Projector Cone
            DA.SetData(1, analysis.ProjectorCone);

            // 3. Section Chairs (Convert List<List> to Tree)
            var chairTree = new DataTree<GeometryBase>();
            if (analysis.SectionChairs != null)
            {
                for (int i = 0; i < analysis.SectionChairs.Count; i++)
                {
                    chairTree.AddRange(analysis.SectionChairs[i], new GH_Path(i));
                }
            }
            DA.SetDataTree(2, chairTree);

            // 4. Limit Lines (Convert List on analysis, but Analysis.Calculate populates single List<Line> result.LimitLines?)
            // Wait, in Analysis.Calculate change I did: result.LimitLines is List<Line>.
            // But Limits are per row. 
            // In my update to Analysis.cs, I flattened them all into result.LimitLines.
            // If I want a Tree, I need structure or reconstruct it.
            // For now, let's output a flattened list or put all in one branch if tree required.
            // Or better: Change logic to use DataTree logic on output, but source is flat.
            // Wait, if source is flat List<Line>, I can't easily reconstruct per-row tree without knowing row counts.
            // But result.LimitLines was populated inside the row loop in Analysis.cs
            // It adds 3 lines per row.
            // So I can simulate the tree structure: i/3 is row index?
            // Actually, Analysis.Calculate was adding 3 lines per row.
            // So:
            var limitTree = new DataTree<Line>();
            if (analysis.LimitLines != null)
            {
                for (int i = 0; i < analysis.LimitLines.Count; i++)
                {
                    int rowIndex = i / 3; 
                    limitTree.Add(analysis.LimitLines[i], new GH_Path(rowIndex));
                }
            }
            DA.SetDataTree(3, limitTree);

            // 5. Tribune Components (From SerializedTribune)
            if (analysis.Tribune != null)
            {
                DA.SetData(4, analysis.Tribune.TribuneProfile);
                DA.SetData(5, analysis.Tribune.StairsProfile);
                DA.SetDataList(6, analysis.Tribune.RailingProfiles);
            }
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
            get { return new Guid("11111111-2222-3333-4444-555555555555"); } // TODO: Generate unique GUID
        }
    }
}
