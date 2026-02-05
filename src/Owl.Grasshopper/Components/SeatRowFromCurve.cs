using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Owl.Core.Primitives;

namespace Owl.Grasshopper.Components
{
    public class SeatRowFromCurve : GH_Component
    {
        public SeatRowFromCurve()
          : base("Seat Row from Curve", "SeatRow",
              "Populates a curve with seat occupants (Eye/Head points).",
              "Owl", "Primitives")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curve", "C", "Row curve", GH_ParamAccess.item);
            pManager.AddNumberParameter("SeatWidth", "W", "Width per seat", GH_ParamAccess.item, 0.55); // 55cm default
            pManager.AddNumberParameter("EyeHeight", "EH", "Eye height from floor", GH_ParamAccess.item, 1.2);
            pManager.AddNumberParameter("HeadClearance", "HC", "C-Value clearance above eye", GH_ParamAccess.item, 0.12);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("EyePoints", "EP", "Viewer eye locations", GH_ParamAccess.list);
            pManager.AddPointParameter("HeadPoints", "HP", "clearance points above viewers", GH_ParamAccess.list);
            pManager.AddGenericParameter("Occupants", "O", "Occupant objects", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve crv = null;
            double width = 0.55;
            double eyeH = 1.2;
            double headC = 0.12;

            if (!DA.GetData(0, ref crv)) return;
            DA.GetData(1, ref width);
            DA.GetData(2, ref eyeH);
            DA.GetData(3, ref headC);

            var row = new OWL_SeatRow(crv, width, eyeH, headC);

            var eyes = new List<Point3d>();
            var heads = new List<Point3d>();
            var occs = new List<OWL_SeatOccupant>();

            foreach (var occ in row.Occupants)
            {
                eyes.Add(occ.EyePoint);
                heads.Add(occ.HeadPoint);
                occs.Add(occ);
            }

            DA.SetDataList(0, eyes);
            DA.SetDataList(1, heads);
            DA.SetDataList(2, occs);
        }

        protected override System.Drawing.Bitmap Icon => null;

        public override Guid ComponentGuid => new Guid("9c5d2e1f-4a3b-4c5d-8e7f-1a2b3c4d5e6f");
    }
}
