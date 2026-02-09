using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Owl.Core.Primitives;

namespace Owl.Grasshopper.Components
{
    public class TribuneProfile : GH_Component
    {
        public TribuneProfile()
          : base("Tribune Profile", "TribProf",
              "Generates a Tribune Profile and Stair Profiles from a Stair Definition",
              "Owl", "Primitives")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Stair Definition", "SD", "The Stair Definition object", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Row Count", "C", "Target number of rows", GH_ParamAccess.item, 10);
            pManager.AddNumberParameter("Row Width", "W", "Width (depth) of each row", GH_ParamAccess.item, 0.8);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Tribune Profile", "P", "The structural profile of the tribune", GH_ParamAccess.item);
            pManager.AddCurveParameter("Stair Profiles", "S", "The profiles of the stairs", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            OWL_StairDefinition stairDef = null;
            int count = 10;
            double width = 0.8;

            // We need to handle the Generic Object wrapper.
            // Grasshopper wraps non-Goo objects in GH_ObjectWrapper usually.
            
            object stairDefObj = null;
            if (!DA.GetData(0, ref stairDefObj)) return;
            if (!DA.GetData(1, ref count)) return;
            if (!DA.GetData(2, ref width)) return;

            // Unwrap
            if (stairDefObj is GH_ObjectWrapper wrapper)
            {
                stairDef = wrapper.Value as OWL_StairDefinition;
            }
            else
            {
                stairDef = stairDefObj as OWL_StairDefinition;
            }

            if (stairDef == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid Stair Definition.");
                return;
            }
            
            if (count < 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Row count must be at least 1.");
                return;
            }
            if (width <= 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Row width must be positive.");
                return;
            }

            var generator = new OWL_TribuneProfileGenerator(stairDef, count, width);
            generator.Generate();

            DA.SetData(0, generator.TribuneProfile);
            DA.SetDataList(1, generator.StairProfiles);
        }

        protected override System.Drawing.Bitmap Icon => null; // Todo: Add Icon

        public override Guid ComponentGuid => new Guid("5f6a8b3c-2d1e-4f7a-9b8c-6e5d4a3b2c1d");
    }
}
