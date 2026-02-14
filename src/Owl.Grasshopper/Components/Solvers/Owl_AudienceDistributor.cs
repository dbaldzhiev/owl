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
            pManager.AddGenericParameter("RailingSetup", "Railing", "Railing Setup Object", GH_ParamAccess.item);
            
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
            pManager[5].Optional = true;
            pManager[6].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("SerializedAnalysis", "SAnalisys", "Serialized Analysis Data container for visualization", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<AudienceSetup> audiences = new List<AudienceSetup>();
            SerializedTribune strib = null;
            RailingSetup railing = null;
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
            DA.GetData(6, ref railing);
            
            List<Curve> planTribune;
            List<Curve> planRailings;
            List<Curve> planStairs;

            var analysisResult = Analysis.Calculate(audiences, strib, railing, screen, projector, offsets, plan);

            DA.SetData(0, analysisResult);
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
