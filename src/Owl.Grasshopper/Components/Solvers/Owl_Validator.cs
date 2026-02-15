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
              "Validate tribune clearances: stair landings and chair front clearance.",
              "Owl", "Solvers")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("SerializedTribune", "STrib", "Serialized Tribune Data", GH_ParamAccess.item);          // 0
            pManager.AddGenericParameter("AudienceSetup", "Audience", "Audience Setup List", GH_ParamAccess.list);                // 1
            pManager.AddNumberParameter("AudienceOffsets", "Offsets", "List of X offsets per row", GH_ParamAccess.list);           // 2

            pManager[1].Optional = true;
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            // CHECK 1 - STAIRS
            pManager.AddNumberParameter("Landings", "Land", "Landing lengths between stair flights (horizontal)", GH_ParamAccess.list);  // 0
            pManager.AddPointParameter("LandA", "LandA", "Landing measurement start points", GH_ParamAccess.list);                       // 1
            pManager.AddPointParameter("LandB", "LandB", "Landing measurement end points", GH_ParamAccess.list);                         // 2

            // CHECK 2 - CHAIRS
            pManager.AddNumberParameter("ChairClearance", "CC", "Chair front clearance per row (horizontal)", GH_ParamAccess.list);      // 3
            pManager.AddPointParameter("CCA", "CCA", "Chair clearance start points (obstacle)", GH_ParamAccess.list);                    // 4
            pManager.AddPointParameter("CCB", "CCB", "Chair clearance end points (front limit)", GH_ParamAccess.list);                   // 5
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            SerializedTribune trib = null;
            List<AudienceSetup> audiences = new List<AudienceSetup>();
            List<double> offsets = new List<double>();

            if (!DA.GetData(0, ref trib) || trib == null) return;
            DA.GetDataList(1, audiences);
            DA.GetDataList(2, offsets);

            // Determine transformation for debug points
            // Calculations are done in 'local' (unflipped) space where X grows positively.
            // If the tribune is flipped (mirrored), we must mirror the debug points to match visual output.
            Transform xform = trib.Flip ? Transform.Mirror(Plane.WorldYZ) : Transform.Identity;

            // ============================================================
            // CHECK 1 - STAIRS: Landing lengths between stair flights
            // ...
            // ============================================================
            if (trib.StairFlightStartX != null && trib.StairFlightStartX.Count > 0)
            {
                var landings = new List<double>();
                var landA = new List<Point3d>();
                var landB = new List<Point3d>();

                int flightCount = trib.StairFlightStartX.Count;

                for (int r = 0; r < flightCount; r++)
                {
                    double startX = trib.StairFlightStartX[r];
                    double z = (trib.RowPoints != null && r < trib.RowPoints.Count)
                        ? trib.RowPoints[r].Z : 0;

                    double refX;
                    if (r == 0)
                    {
                        // First flight: measure from origin
                        refX = 0.0;
                    }
                    else
                    {
                        // Subsequent flights: measure from end of previous flight
                        refX = trib.StairFlightEndX[r - 1];
                    }

                    double landing = startX - refX;
                    landings.Add(landing);
                    
                    Point3d pA = new Point3d(refX, 0, z);
                    Point3d pB = new Point3d(startX, 0, z);
                    
                    if (trib.Flip)
                    {
                        pA.Transform(xform);
                        pB.Transform(xform);
                    }

                    landA.Add(pA);
                    landB.Add(pB);
                }

                DA.SetDataList(0, landings);
                DA.SetDataList(1, landA);
                DA.SetDataList(2, landB);
            }

            // ============================================================
            // CHECK 2 - CHAIRS: Front clearance per row
            // ...
            // ============================================================
            if (audiences.Count > 0 && trib.RowPoints != null && trib.RowPoints.Count > 0)
            {
                var cc = new List<double>();
                var ccA = new List<Point3d>();
                var ccB = new List<Point3d>();

                int rowCount = trib.RowPoints.Count;
                var toggles = trib.RailingToggles;

                for (int i = 0; i < rowCount; i++)
                {
                    AudienceSetup aud = audiences[i % audiences.Count];
                    if (aud == null)
                    {
                        cc.Add(0);
                        Point3d pA_ev = trib.RowPoints[i];
                        Point3d pB_ev = trib.RowPoints[i];
                        if (trib.Flip) { pA_ev.Transform(xform); pB_ev.Transform(xform); }
                        ccA.Add(pA_ev);
                        ccB.Add(pB_ev);
                        continue;
                    }

                    double myOffset = 0;
                    if (offsets != null && offsets.Count > 0)
                        myOffset = offsets[i % offsets.Count];

                    double rowZ = trib.RowPoints[i].Z;

                    // B = chair front limit
                    double chairFrontX = trib.RowPoints[i].X + aud.SecFL + myOffset;

                    // A = obstacle
                    double obstacleX;

                    if (i == 0)
                    {
                        // First row: measure to origin
                        obstacleX = 0.0;
                    }
                    else
                    {
                        bool hasRailing = true;
                        if (toggles != null && toggles.Count > i)
                            hasRailing = toggles[i];

                        if (hasRailing)
                        {
                            // Railing inner wall
                            obstacleX = trib.RowPoints[i].X;
                        }
                        else
                        {
                            // Previous row's hard back limit
                            var prevAud = audiences[(i - 1) % audiences.Count];
                            double prevHBL = (prevAud != null) ? prevAud.SecHBL : 0;
                            double prevOffset = 0;
                            if (offsets != null && offsets.Count > 0)
                                prevOffset = offsets[(i - 1) % offsets.Count];
                            obstacleX = trib.RowPoints[i - 1].X + prevHBL + prevOffset;
                        }
                    }

                    double clearance = chairFrontX - obstacleX;
                    cc.Add(clearance);
                    
                    Point3d pA = new Point3d(obstacleX, 0, rowZ);
                    Point3d pB = new Point3d(chairFrontX, 0, rowZ);

                    if (trib.Flip)
                    {
                        pA.Transform(xform);
                        pB.Transform(xform);
                    }

                    ccA.Add(pA);
                    ccB.Add(pB);
                }

                DA.SetDataList(3, cc);
                DA.SetDataList(4, ccA);
                DA.SetDataList(5, ccB);
            }
        }

        protected override System.Drawing.Bitmap Icon => null;

        public override Guid ComponentGuid => new Guid("F2C1A3B4-5678-4321-8901-23456789ABCD");
    }
}
