using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Owl.Core.Primitives;

namespace Owl.Grasshopper.Components
{
    public class StairFromProfile : GH_Component
    {
        public StairFromProfile()
          : base("Stair from Profile", "StairProf",
              "Generates risers and treads from a section profile and constant riser height.",
              "Owl", "Primitives")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Profile", "P", "Section profile curve (should be monotonic in Z)", GH_ParamAccess.item);
            pManager.AddNumberParameter("RiserHeight", "R", "Strict riser height", GH_ParamAccess.item, 0.15); // Default 150mm assuming meters
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Risers", "R", "Generated riser curves", GH_ParamAccess.list);
            pManager.AddCurveParameter("Treads", "T", "Generated tread curves", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve profile = null;
            double riserHeight = 0;

            if (!DA.GetData(0, ref profile)) return;
            if (!DA.GetData(1, ref riserHeight)) return;

            if (riserHeight <= 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Riser height must be > 0");
                return;
            }

            var generator = new OWL_StairGenerator(profile, riserHeight);
            generator.Generate();

            DA.SetDataList(0, generator.Risers);
            DA.SetDataList(1, generator.Treads);
        }

        protected override System.Drawing.Bitmap Icon => null;

        public override Guid ComponentGuid => new Guid("4b2c1d3f-8a9e-4c5b-9d2a-1e3f5a6b7c8d");
    }
}
