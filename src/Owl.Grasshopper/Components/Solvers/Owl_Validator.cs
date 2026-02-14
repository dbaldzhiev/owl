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
            pManager.AddGenericParameter("SerializedTribune", "STrib", "Serialized Tribune Data from the Tribune Solver", GH_ParamAccess.item);
            pManager.AddGenericParameter("AudienceSetup", "Audience", "Audience Setup List", GH_ParamAccess.list);
            pManager.AddNumberParameter("AudienceOffsets", "Offsets", "List of X offsets per row", GH_ParamAccess.list);

            pManager[1].Optional = true;
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Gaps", "Gaps", "Distance from back of railing to stair (gaps)", GH_ParamAccess.list);
            pManager.AddNumberParameter("RailingToFrontLimit", "R2FL", "Distance from back side of railing to front limit of audience", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            SerializedTribune tribune = null;
            List<AudienceSetup> audiences = new List<AudienceSetup>();
            List<double> offsets = new List<double>();

            if (!DA.GetData(0, ref tribune) || tribune == null) return;
            DA.GetDataList(1, audiences);
            DA.GetDataList(2, offsets);

            // Output Gaps
            if (tribune.Gaps != null)
            {
                DA.SetDataList(0, tribune.Gaps);
            }

            // Compute R2FL
            if (audiences.Count > 0 && tribune.RowPoints != null)
            {
                var r2fl = new List<double>();
                int rowCount = tribune.RowPoints.Count;
                var toggles = tribune.RailingToggles;

                for (int i = 0; i < rowCount; i++)
                {
                    AudienceSetup currentAudience = audiences[i % audiences.Count];
                    if (currentAudience == null)
                    {
                        r2fl.Add(0);
                        continue;
                    }

                    double offset = 0;
                    if (offsets != null && offsets.Count > 0)
                    {
                        offset = offsets[i % offsets.Count];
                    }

                    bool hasRailing = true;
                    if (toggles != null && toggles.Count > i)
                    {
                        hasRailing = toggles[i];
                    }

                    if (hasRailing)
                    {
                        r2fl.Add(currentAudience.SecFL + offset);
                    }
                    else
                    {
                        if (i > 0)
                        {
                            AudienceSetup prevAudience = audiences[(i - 1) % audiences.Count];
                            double prevHardBack = prevAudience != null ? prevAudience.SecHBL : 0;
                            r2fl.Add(prevHardBack + currentAudience.SecFL + offset);
                        }
                        else
                        {
                            r2fl.Add(currentAudience.SecFL + offset);
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
                return null;
            }
        }

        public override Guid ComponentGuid => new Guid("F2C1A3B4-5678-4321-8901-23456789ABCD");
    }
}
