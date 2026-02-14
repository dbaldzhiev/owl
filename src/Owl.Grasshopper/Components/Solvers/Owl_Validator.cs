using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Owl.Core.Primitives;

namespace Owl.Grasshopper.Components.Solvers
{
    public class Owl_Validator : GH_Component
    {
        public Owl_Validator()
          : base("Validator", "Validate",
              "Validate sightlines and check gaps in the tribune setup.",
              "Owl", "Solvers")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("SerializedAnalysis", "SAnalysis", "Serialized Analysis Data from the Analysis component", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Gaps", "Gaps", "Distance from back of railing to stair (gaps)", GH_ParamAccess.list);
            pManager.AddNumberParameter("RailingToFrontLimit", "R2FL", "Distance from back side of railing to front limit of audience", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            SerializedAnalysis analysis = null;
            if (!DA.GetData(0, ref analysis) || analysis == null) return;

            if (analysis.Tribune != null && analysis.Tribune.Gaps != null)
            {
                DA.SetDataList(0, analysis.Tribune.Gaps);
            }

            if (analysis.Audiences != null && analysis.Audiences.Count > 0 && analysis.Tribune != null && analysis.Tribune.RowPoints != null)
            {
                var r2fl = new List<double>();
                int rowCount = analysis.Tribune.RowPoints.Count;
                var toggles = analysis.Tribune.RailingToggles;

                for (int i = 0; i < rowCount; i++)
                {
                    AudienceSetup currentAudience = analysis.Audiences[i % analysis.Audiences.Count];
                    if (currentAudience == null)
                    {
                        r2fl.Add(0);
                        continue;
                    }

                    double offset = 0;
                    if (analysis.Offsets != null && analysis.Offsets.Count > 0)
                    {
                        offset = analysis.Offsets[i % analysis.Offsets.Count];
                    }

                    bool hasRailing = true;
                    if (toggles != null && toggles.Count > i)
                    {
                        hasRailing = toggles[i];
                    }

                    if (hasRailing)
                    {
                        // Railing present: distance from back of railing to front limit
                        r2fl.Add(currentAudience.FrontLimit + offset);
                    }
                    else
                    {
                        // No railing: distance from previous row's hard back limit to current front limit
                        if (i > 0)
                        {
                            AudienceSetup prevAudience = analysis.Audiences[(i - 1) % analysis.Audiences.Count];
                            double prevHardBack = prevAudience != null ? prevAudience.HardBackLimit : 0;
                            r2fl.Add(prevHardBack + currentAudience.FrontLimit + offset);
                        }
                        else
                        {
                            // Row 0 with no railing — fallback to frontLimit + offset
                            r2fl.Add(currentAudience.FrontLimit + offset);
                        }
                    }
                }
                DA.SetDataList(1, r2fl);
            }
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // Reusing Analysis icon placeholder logic or returning null
                return null;
            }
        }

        public override Guid ComponentGuid => new Guid("F2C1A3B4-5678-4321-8901-23456789ABCD");
    }
}
