using System;
using System.Collections.Generic;
using Rhino.Geometry;
using Owl.Core.Primitives;

namespace Owl.Core.Solvers
{
    public class TribuneSolver
    {
        private readonly TribuneSetup _tribune;
        private readonly StairSetup _stairs;
        private readonly RailingSetup _railings;

        public TribuneSolver(TribuneSetup tribune, StairSetup stairs, RailingSetup railings)
        {
            _tribune = tribune ?? throw new ArgumentNullException(nameof(tribune));
            _stairs = stairs ?? throw new ArgumentNullException(nameof(stairs));
            _railings = railings ?? throw new ArgumentNullException(nameof(railings));
        }

        public void Solve(out Curve tribuneProfile, out Curve stairsProfile, out List<Curve> railingProfiles, out List<double> gaps, out SerializedTribune serializedTribune)
        {
            tribuneProfile = null;
            stairsProfile = null;
            railingProfiles = new List<Curve>();
            gaps = new List<double>();
            
            var rowPoints = new List<Point3d>();

            if (_tribune.Rows <= 0) 
            {
                serializedTribune = new SerializedTribune();
                return;
            }

            // -----------------------------
            // A) TRIBUNE PROFILE
            // -----------------------------
            var tribPts = new List<Point3d>();
            double currX = 0.0;
            double currZ = 0.0;

            // Start point
            tribPts.Add(new Point3d(currX, 0, currZ));

            // Row 0 (Ground/Front row base)
            double row0Width = 0.8; 
            if (_tribune.RowWidths.Count > 0) row0Width = _tribune.RowWidths[0];
            
            Func<int, double> getRowWidth = (i) => {
                if (_tribune.RowWidths.Count == 0) return 0.8; // Default
                return _tribune.RowWidths[i % _tribune.RowWidths.Count];
            };

            // Capture Row 0 Point (Back of the flat area)
            rowPoints.Add(new Point3d(currX + row0Width, 0, currZ));

            currX += getRowWidth(0);
            tribPts.Add(new Point3d(currX, 0, currZ));

            // Elevated Rows
            for (int r = 1; r < _tribune.Rows; r++) // Loop to Rows-1? 
            {
                // Logic check:
                // _tribune.Rows is total rows.
                // Row 0 is ground.
                // Rows 1..N-1 are elevated.
                // If Rows=10. r goes 1..9.
                
                // Original code: for (int r = 1; r <= _tribune.Rows; r++)
                // Wait, if Rows=10, we have 10 rows total?
                // Or "Rows" means elevated rows?
                // Setup default is 10.
                // "Number of elevated rows" description says so.
                // So Total Rows = 1 (Ground) + N (Elevated)?
                // Or Total Rows = N (including ground)?
                
                // Let's assume input "Rows" is the TOTAL count of rows the user expects.
                // Loop r=1 to < Rows.

                int idx = r - 1;
                int count = 1;
                
                if (_tribune.ElevCounts != null && _tribune.ElevCounts.Count > 0)
                {
                    if (idx < _tribune.ElevCounts.Count)
                        count = _tribune.ElevCounts[idx];
                    else
                        count = _tribune.ElevCounts[_tribune.ElevCounts.Count - 1];
                }

                if (count < 1) count = 1;

                double rowRise = count * _stairs.TreadHeight;
                double thisRowWidth = getRowWidth(r);

                // 1. Riser UP
                currZ += rowRise;
                tribPts.Add(new Point3d(currX, 0, currZ));

                // Capture Row Point (Back of flat area for Row r)
                rowPoints.Add(new Point3d(currX + thisRowWidth, 0, currZ));

                // --- Generate Railing at this Riser ---
                double rBottomZ = currZ - rowRise;
                double rTopZ = currZ + _railings.RailHeight;
                double railW = _railings.RailWidth;

                Point3d p0 = new Point3d(currX, 0, rBottomZ);
                Point3d p1 = new Point3d(currX, 0, rTopZ);
                Point3d p2 = new Point3d(currX + railW, 0, rTopZ);
                Point3d p3 = new Point3d(currX + railW, 0, rBottomZ);

                if (railW < thisRowWidth)
                {
                    var railRec = new Polyline(new[] { p0, p1, p2, p3, p0 });
                    railingProfiles.Add(railRec.ToNurbsCurve());
                }

                // 2. Tread FORWARD
                currX += thisRowWidth;
                tribPts.Add(new Point3d(currX, 0, currZ));
            }

            if (tribPts.Count > 1)
                tribuneProfile = new Polyline(tribPts).ToNurbsCurve();
            
            serializedTribune = new SerializedTribune(rowPoints);

            // -----------------------------
            // B) STAIRS PROFILE
            // -----------------------------
            var stairPts = new List<Point3d>();
            
            double currentBaseX = getRowWidth(0); // Start of Row 1 (first elevated)
            double currentBaseZ = 0.0;

            for (int r = 0; r < _tribune.Rows - 1; r++) // Adjusted loop limit based on new understanding?
            {
                // We need stairs connecting Row r to Row r+1.
                // Max r is Rows-2 (connecting to Rows-1).
                // Existing logic: r goes 0 to Rows-1? 
                
                // If Rows=3. Indices 0, 1, 2.
                // Stair 0: Connects 0->1.
                // Stair 1: Connects 1->2.
                // Stair 2: Connects 2->3? (Row 3 doesn't exist).
                
                // So loop should be r < Rows - 1.
                
                int idx = r;
                int count = 1;

                if (_tribune.ElevCounts != null && _tribune.ElevCounts.Count > 0)
                {
                    if (idx < _tribune.ElevCounts.Count)
                        count = _tribune.ElevCounts[idx];
                    else
                        count = _tribune.ElevCounts[_tribune.ElevCounts.Count - 1];
                }
                if (count < 1) count = 1;

                double rise = _stairs.TreadHeight;
                double run = _stairs.TreadWidth;
                
                bool inset = false;
                if (_tribune.StairInsets.Count > 0)
                {
                    inset = _tribune.StairInsets[r % _tribune.StairInsets.Count];
                }
                
                double insetVal = inset ? _railings.RailWidth : 0.0;
                
                double targetLandingZ = currentBaseZ + (count * rise);
                
                double flightRun = (count - 1) * run;
                double startX = currentBaseX - flightRun + insetVal; 
                
                // Connect from previous
                if (stairPts.Count > 0)
                {
                     Point3d lastPt = stairPts[stairPts.Count - 1];
                     if (Math.Abs(lastPt.X - startX) > 0.001 || Math.Abs(lastPt.Z - currentBaseZ) > 0.001)
                     {
                         stairPts.Add(new Point3d(startX, 0, currentBaseZ));
                     }
                }
                else
                {
                     stairPts.Add(new Point3d(startX, 0, currentBaseZ));
                }

                // Build steps
                double cx = startX;
                double cz = currentBaseZ;

                for (int i = 0; i < count; i++)
                {
                    cz += rise;
                    stairPts.Add(new Point3d(cx, 0, cz)); 
                    if (i < count - 1)
                    {
                        cx += run;
                        stairPts.Add(new Point3d(cx, 0, cz));
                    }
                }

                double prevRiserX = currentBaseX - getRowWidth(r);
                if (r == 0) prevRiserX = 0; 
                
                double railInnerFace = prevRiserX + _railings.RailWidth;
                if (r == 0) railInnerFace = 0; 
                
                double thisGap = startX - railInnerFace;
                gaps.Add(thisGap);

                currentBaseX += getRowWidth(r+1); 
                currentBaseZ = targetLandingZ;
            }

            if (stairPts.Count > 1)
                stairsProfile = new Polyline(stairPts).ToNurbsCurve();
        }
    }
}
