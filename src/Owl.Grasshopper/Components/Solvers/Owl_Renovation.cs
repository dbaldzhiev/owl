using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Owl.Core.Solvers;

namespace Owl.Grasshopper.Components.Solvers
{
    public class Owl_Renovation : GH_Component
    {
        public Owl_Renovation()
          : base("Renovation", "Renovate",
              "Find intersection points between front/back limits and the tribune profile.",
              "Owl", "Solvers")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("FrontLimit", "FLimit", "Front limit curve", GH_ParamAccess.item);
            pManager.AddCurveParameter("BackLimit", "BLimit", "Back limit curve", GH_ParamAccess.item);
            pManager.AddCurveParameter("Tribune", "Trib", "Tribune polyline curve", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("FLOrigin", "FLO", "Front limit intersection point", GH_ParamAccess.item);
            pManager.AddPointParameter("BLOrigin", "BLO", "Back limit intersection point", GH_ParamAccess.item);
            pManager.AddCurveParameter("Treads", "T", "Horizontal segments of the tribune", GH_ParamAccess.list);
            pManager.AddCurveParameter("Risers", "R", "Vertical segments of the tribune", GH_ParamAccess.list);
            pManager.AddGenericParameter("SerializedTribune", "STrib", "Serialized Existing Tribune", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve fl = null;
            Curve bl = null;
            Curve trib = null;

            DA.GetData(0, ref fl);
            DA.GetData(1, ref bl);
            if (!DA.GetData(2, ref trib)) return;

            Point3d flOrigin;
            Point3d blOrigin;
            List<Curve> treads;
            List<Curve> risers;

            var existingTribune = Renovation.Solve(fl, bl, trib, out flOrigin, out blOrigin, out treads, out risers);

            DA.SetData(0, flOrigin);
            DA.SetData(1, blOrigin);
            DA.SetDataList(2, treads);
            DA.SetDataList(3, risers);
            DA.SetData(4, existingTribune);
        }

        protected override System.Drawing.Bitmap Icon => null;

        public override Guid ComponentGuid => new Guid("5A1B2C3D-4E5F-6A7B-8C9D-0E1F2A3B4C5D");
    }
}
