using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using Owl.Core.Primitives;
using Owl.Core.Solvers;

namespace Owl.Grasshopper.Components.Solvers
{
    public class Owl_Validator : GH_Component
    {
        public Owl_Validator()
          : base("Validator", "Validate",
              "Validate tribune clearances: stair landings and chair front clearance.",
              "Owl", "Solvers")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("TribuneSolution", "Sol", "Tribune Solution container", GH_ParamAccess.item);         // 0
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            // CHECK 1 - STAIRS
            pManager.AddNumberParameter("StairClearances", "StairClr", "Landing lengths between stair flights", GH_ParamAccess.list);  // 0
            
            // CHECK 2 - CHAIRS
            pManager.AddNumberParameter("ChairClearances", "ChairClr", "Chair front clearance per row", GH_ParamAccess.list);      // 1
            
            // CHECK 3 - C-VALUES
            pManager.AddNumberParameter("CValues", "CVal", "C-Values for sightlines", GH_ParamAccess.list);                        // 2
            
            // CHECK 4 - ERRORS
            pManager.AddTextParameter("Errors", "Err", "Validation error messages", GH_ParamAccess.list);                          // 3

            // Visualizers
            pManager.AddPointParameter("Clashes", "Clash", "Points where chairs intersect existing tribune", GH_ParamAccess.list);       // 4
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            TribuneSolution solution = null;

            if (!DA.GetData(0, ref solution) || solution == null) return;

            // Use pre-calculated values from Solution
            // STAIRS
            DA.SetDataList(0, solution.StairClearances);
            
            // CHAIRS
            DA.SetDataList(1, solution.ChairClearances);
            
            // C-Val (Not calculated yet? Keep placeholder or remove)
            // DA.SetDataList(2, cValues); 
            
            // ERRORS
            DA.SetDataList(3, solution.Errors);
            
            // Clashes
            DA.SetDataList(4, solution.Clashes);
        }


        protected override System.Drawing.Bitmap Icon => null;

        public override Guid ComponentGuid => new Guid("F2C1A3B4-5678-4321-8901-23456789ABCD");
    }
}

