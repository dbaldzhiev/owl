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
              "Validate sightlines and check clearances in the tribune setup.",
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
            pManager.AddNumberParameter("Gaps", "Gaps", "Horizontal gap between back of railing/HBL and stair flight (per stair flight)", GH_ParamAccess.list);
            pManager.AddNumberParameter("RailingToFrontLimit", "R2FL", "Horizontal distance from chair spine to front limit line (per row)", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            SerializedTribune tribune = null;
            List<AudienceSetup> audiences = new List<AudienceSetup>();
            List<double> offsets = new List<double>();

            if (!DA.GetData(0, ref tribune) || tribune == null) return;
            DA.GetDataList(1, audiences);
            DA.GetDataList(2, offsets);

            // Output Gaps directly from the solver
            if (tribune.Gaps != null)
            {
                DA.SetDataList(0, tribune.Gaps);
            }

            // Compute R2FL: horizontal distance from chair spine (SecRowSpine)
            // to the front limit line for each row.
            // SecRowSpine already accounts for railing presence/absence
            // (when no railing, spine is shifted to prevHBL position).
            // R2FL = SecFL + offset, measured from the spine point.
            if (audiences.Count > 0 && tribune.SecRowSpine != null && tribune.SecRowSpine.Count > 0)
            {
                var r2fl = new List<double>();
                int rowCount = tribune.SecRowSpine.Count;

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

                    // The front limit distance is purely the audience property + offset,
                    // measured forward from the spine point (which is the chair origin).
                    r2fl.Add(currentAudience.SecFL + offset);
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
