using System;
using System.Collections.Generic;
using Rhino.Geometry;
using Owl.Core.Primitives;
using Rhino.Geometry.Intersect;

namespace Owl.Core.Solvers
{
    public class TribuneValidator
    {
        public static void Validate(
            TribuneSolution solution,
            out List<double> landingLengths,
            out List<double> chairClearances,
            out List<double> cValues,
            out List<string> errors,
            out List<Point3d> clashPoints)
        {
            landingLengths = new List<double>();
            chairClearances = new List<double>();
            cValues = new List<double>();
            errors = new List<string>();
            clashPoints = new List<Point3d>();

            if (solution == null)
            {
                errors.Add("Solution is null.");
                return;
            }

            var audiences = solution.Audiences ?? new List<AudienceSetup>();
            var offsets = solution.AudienceOffsets ?? new List<double>();

            // ============================================================
            // CHECK 1 - STAIRS
            // ============================================================
            if (solution.StairFlightStartX != null && solution.StairFlightStartX.Count > 0)
            {
                int flightCount = solution.StairFlightStartX.Count;
                for (int r = 0; r < flightCount; r++)
                {
                    double startX = solution.StairFlightStartX[r]; 
                    double refX = (r == 0) ? 0.0 : solution.StairFlightEndX[r - 1]; 
                    double landing = startX - refX; 
                    landingLengths.Add(landing);
                    if (landing < 0) errors.Add($"Row {r} Stair Landing Negative: {landing:F1}");
                }
            }

            // ============================================================
            // CHECK 2 - CHAIRS: Front clearance
            // ============================================================
            if (audiences.Count > 0 && solution.RowLocalPoints != null && solution.RowLocalPoints.Count > 0)
            {
                int rowCount = solution.RowLocalPoints.Count;
                for (int i = 0; i < rowCount; i++)
                {
                    AudienceSetup aud = audiences[i % audiences.Count];
                    if (aud == null) continue;

                    double myOffset = (offsets.Count > 0) ? offsets[i % offsets.Count] : 0;
                    double rX = solution.RowLocalPoints[i].X;
                    double currentFL = rX + myOffset + aud.SecFL;
                    double obstacleX = 0;
                    if (i > 0)
                    {
                        int prevIdx = i - 1;
                        var prevAud = audiences[prevIdx % audiences.Count];
                        double prevOffset = (offsets.Count > 0) ? offsets[prevIdx % offsets.Count] : 0;
                        if (prevAud != null)
                        {
                            obstacleX = solution.RowLocalPoints[prevIdx].X + prevOffset + prevAud.SecHBL;
                        }
                    }
                    
                    double clearance = currentFL - obstacleX;
                    chairClearances.Add(clearance);
                    if (clearance < 0) errors.Add($"Row {i} Chair Clearance Clash: {clearance:F1}");
                }
            }
            
            // ============================================================
            // CHECK 3 - CLASH DETECTION (Existing Tribune)
            // ============================================================
            if (solution.ExistingTribuneProfile != null && solution.SectionChairs != null)
            {
                Curve exist = solution.ExistingTribuneProfile;
                foreach(var rowChairs in solution.SectionChairs)
                {
                    if (rowChairs == null) continue;
                    foreach(var chairCrv in rowChairs)
                    {
                        if (chairCrv == null) continue;
                        var events = Intersection.CurveCurve(chairCrv, exist, 0.001, 0.001);
                        if (events != null && events.Count > 0)
                        {
                            foreach(var e in events) clashPoints.Add(e.PointA);
                            errors.Add("Clash detected between chair and existing tribune.");
                        }
                    }
                }
            }

            // ============================================================
            // CHECK 4 - C-VALUES (Placeholder)
            // ============================================================
            // To be implemented when Screen logic is robust.
            // For now, returning empty list.
        }
    }
}
