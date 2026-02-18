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

            TribuneValidator.Validate(
                solution,
                out List<double> landings,
                out List<double> cc,
                out List<double> cValues,
                out List<string> errors,
                out List<Point3d> clashes
            );

            // Set Data
            DA.SetDataList(0, landings);
            DA.SetDataList(1, cc);
            DA.SetDataList(2, cValues);
            DA.SetDataList(3, errors);
            DA.SetDataList(4, clashes);
        }

        protected override System.Drawing.Bitmap Icon => null;

        public override Guid ComponentGuid => new Guid("F2C1A3B4-5678-4321-8901-23456789ABCD");
    }
}

