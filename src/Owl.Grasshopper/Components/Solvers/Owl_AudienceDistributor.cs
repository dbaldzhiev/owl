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
            pManager.AddCurveParameter("ExistingRisers", "ExRisers", "List of existing tribune risers for alignment", GH_ParamAccess.list);
            pManager.AddBooleanParameter("AlignFirstChair", "AlignFirst", "Align the first chair's back limit to existing risers", GH_ParamAccess.item, false);

            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
            pManager[5].Optional = true;
            pManager[6].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddLineParameter("Sightlines", "Lines", "Sightlines from eyes to screen bottom", GH_ParamAccess.list);
            pManager.AddBrepParameter("ProjectorCone", "Cone", "Projector cone geometry", GH_ParamAccess.item);
            pManager.AddCurveParameter("Chairs", "Chairs", "Distributed chair geometry", GH_ParamAccess.tree);
            pManager.AddLineParameter("LimitLines", "Limits", "Vertical lines for front and back limits", GH_ParamAccess.tree);
            pManager.AddGenericParameter("SerializedAnalysis", "SAnalisys", "Serialized Analysis Data (for validation)", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<AudienceSetup> audiences = new List<AudienceSetup>();
            SerializedTribune strib = null;
            ProjectorSetup projector = null;
            ScreenSetup screen = null;
            List<double> offsets = new List<double>();
            List<Curve> existingRisers = new List<Curve>();
            bool alignFirst = false;

            if (!DA.GetDataList(0, audiences) || audiences.Count == 0) return;
            if (!DA.GetData(1, ref strib) || strib == null) return;
            DA.GetData(2, ref projector);
            DA.GetData(3, ref screen);
            DA.GetDataList(4, offsets);
            DA.GetDataList(5, existingRisers);
            DA.GetData(6, ref alignFirst);
            
            List<Line> sightlines;
            List<List<Line>> limitLines;
            Brep cone;
            List<List<Curve>> chairs;

            Analysis.Calculate(audiences, strib, screen, projector, offsets, existingRisers, alignFirst, out sightlines, out limitLines, out cone, out chairs);

            var serializedAnalysis = new SerializedAnalysis(strib, audiences, sightlines, offsets);

            // Convert to DataTrees
            var limitTree = new DataTree<Line>();
            for (int i = 0; i < limitLines.Count; i++)
            {
                limitTree.AddRange(limitLines[i], new global::Grasshopper.Kernel.Data.GH_Path(i));
            }

            var chairTree = new DataTree<Curve>();
            for (int i = 0; i < chairs.Count; i++)
            {
                chairTree.AddRange(chairs[i], new global::Grasshopper.Kernel.Data.GH_Path(i));
            }

            DA.SetDataList(0, sightlines);
            DA.SetData(1, cone);
            DA.SetDataTree(2, chairTree);
            DA.SetDataTree(3, limitTree);
            DA.SetData(4, serializedAnalysis);
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
