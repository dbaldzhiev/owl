using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Owl.Core.Primitives;

namespace Owl.Grasshopper.Components.Setup
{
    public class Owl_DisabledSeatsSetup : GH_Component
    {
        public Owl_DisabledSeatsSetup()
          : base("Disabled Seats Setup", "DisSeat",
              "Configuration for Disabled Seats in the first row.",
              "Owl", "Setup")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("Count", "Count", "Number of disabled chairs", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("Distribution", "Dist", "Distribution of chairs (0.0 to 1.0)", GH_ParamAccess.item, 0.5);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("DisabledSeatsSetup", "DSetup", "Disabled Seats Setup Object", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            int count = 0;
            double dist = 0.5;

            DA.GetData(0, ref count);
            DA.GetData(1, ref dist);

            var setup = new DisabledSeatsSetup(count, dist);

            DA.SetData(0, setup);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // return Properties.Resources.Owl_DisabledSeatsSetup;
                return null; 
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("2f094e1d-8456-429d-9071-6c7890a2b3c4"); }
        }
    }
}
