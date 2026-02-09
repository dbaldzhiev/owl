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

        public void Solve(out Curve tribuneProfile, out Curve stairsProfile, out List<Curve> railingProfiles, out List<double> gaps)
        {
            tribuneProfile = null;
            stairsProfile = null;
            railingProfiles = new List<Curve>();
            gaps = new List<double>();

            if (_tribune.Rows <= 0) return;

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
            if (_tribune.RowWidths.Count > 0) row0Width = _tribune.RowWidths[0]; // Assuming index 0 is used for the very first ground segment? 
            // OR: Rows are numbered 1..N. RowWidths usually correspond to the elevated rows?
            // Let's assume RowWidths[i] is the width of Row i (where Row 0 is the "ground" row, Row 1 is first elevated...).
            
            // Re-evaluating Loop logic:
            // "rows" usually means "number of standing rows".
            // If i=0 is the ground level row. 
            // Let's use cyclic access for safety.

            Func<int, double> getRowWidth = (i) => {
                if (_tribune.RowWidths.Count == 0) return 0.8; // Default
                return _tribune.RowWidths[i % _tribune.RowWidths.Count];
            };

            currX += getRowWidth(0);
            tribPts.Add(new Point3d(currX, 0, currZ));

            // Elevated Rows
            for (int r = 1; r <= _tribune.Rows; r++)
            {
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

                // 1. Riser UP
                currZ += rowRise;
                tribPts.Add(new Point3d(currX, 0, currZ));

                // --- Generate Railing at this Riser ---
                double rBottomZ = currZ - rowRise;
                double rTopZ = currZ + _railings.RailHeight;
                double railW = _railings.RailWidth;

                Point3d p0 = new Point3d(currX, 0, rBottomZ);
                Point3d p1 = new Point3d(currX, 0, rTopZ);
                Point3d p2 = new Point3d(currX + railW, 0, rTopZ);
                Point3d p3 = new Point3d(currX + railW, 0, rBottomZ);

                double thisRowWidth = getRowWidth(r);
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

            // -----------------------------
            // B) STAIRS PROFILE
            // -----------------------------
            var stairPts = new List<Point3d>();
            
            // Logic:
            // Iterate again to build stairs.
            // We need to track the "Base" position (where the row starts).
            
            double currentBaseX = getRowWidth(0); // Start of Row 1 (first elevated)
            double currentBaseZ = 0.0;

            for (int r = 0; r < _tribune.Rows; r++)
            {
                // This loop handles the stairs LEADING UP TO Row (r+1).
                // Wait, logic check:
                // Row 1 is elevated. The stairs to reach Row 1 are located in the "Row 0" area?
                // OR: Stairs start at the landing of Row 0 and go UP to Row 1 landing?
                // Usually stairs cut into the row module.
                
                // Let's stick to the previous logic which seemed to work:
                // Stairs fill the rise.
                
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
                
                // --- INSET LOGIC ---
                // Check if this row's stairs should be inset.
                bool inset = false;
                if (_tribune.StairInsets.Count > 0)
                {
                    // Which index logic? 
                    // Let's use 'r' (0..Rows-1).
                    // If true, the stair start X is shifted by RailWidth.
                    inset = _tribune.StairInsets[r % _tribune.StairInsets.Count];
                }
                
                double insetVal = inset ? _railings.RailWidth : 0.0;
                
                // The landing for this flight is at:
                double targetLandingZ = currentBaseZ + (count * rise);
                
                // Calculate where flight starts relative to target
                // If the stairs are simply "placed", they end at the target X (start of next row)
                // target X = currentBaseX.
                
                double flightRun = (count - 1) * run;
                double startX = currentBaseX - flightRun + insetVal; // Shift forward if inset?
                // If "Inset inside the row with the width of the railing":
                // Usually means the "Walking width" is reduced, or the start is pushed.
                // Assuming "Push the start away from the riser by RailWidth".
                
                // --- GAP CALCULATION ---
                // Gap = Distance between Railing Inner Face (at currentBaseX + RailWidth)
                //       and the Stair First Nosing (startX).
                // Railing is at currentBaseX. Inner face = currentBaseX + RailWidth.
                // Stair Start = startX.
                // Gap = startX - (currentBaseX + RailWidth) ??
                
                // Wait, previous logic: "Railing sits on previous level".
                // We are at Row r (0..). 
                // Railing is at 'currentBaseX' (the riser location).
                // Railing extends to 'currentBaseX + RailWidth'.
                // Stair starts at 'startX'.
                
                // Wait, 'startX' is calculated as 'targetX - flightRun'.
                // 'targetX' IS 'currentBaseX' ??
                // No, 'currentBaseX' is the START of Row (r+1). 
                // The Stairs GO UP to 'currentBaseX'.
                // So the *top* of the stairs is at 'currentBaseX'.
                // The *bottom* (start) is at 'currentBaseX - flightRun'.
                
                // The RAILING is at the PREVIOUS Riser (start of Row r).
                // Wait.
                // Row 0 -> Row 1.
                // We are on Row 0 floor. We walk towards Row 1 riser.
                // The stairs are ON Row 0 essentially?
                // No, stairs connect Level 0 to Level 1.
                // Logic:
                // Railing 1 is at Riser 1 (Level 0 -> Level 1).
                // Railing 2 is at Riser 2 (Level 1 -> Level 2).
                
                // So for Loop r=0 (First Rise):
                // We are climbing from 0 to 1.
                // There is a Railing at the connection point? 
                // Usually railings are at the Step edges.
                
                // Let's assume Gap logic:
                // Gap between "The Railing of the PREVIOUS row (which is behind us)" and "The start of THIS stair"?
                // OR Gap between "The Railing of THIS row (ahead of us)" and "The top of this stair".
                
                // User said: "gaps between the railing and the staris that are on each row"
                // Likely: On a given row (walking surface), there is a Railing (from the riser below? no, riser above).
                // And there is the start of the stairs for the NEXT level.
                // Gap = Distance(RailingFace, StairStart).
                
                // Railing for "this row rise" is at currentBaseX (the riser line).
                // Stair for "this row rise" ends at currentBaseX.
                // Wait...
                
                // Let's look at a ROW MODULE 'r'.
                // It starts at 'rowStartX' (bottom of riser) and ends at 'rowEndX' (bottom of next riser).
                // On this row module, we have:
                // 1. The Railing of the riser we just climbed (at rowStartX). Width extends to rowStartX + RailW.
                // 2. The Start of the Stairs for the NEXT riser (at rowEndX - flightRunNext).
                
                // So Gap = (StairStartNext) - (RailingEndPrev).
                
                // Let's calculate this Gap for the CURRENT Layout.
                // We are generating Stair 'r' (Level r -> Level r+1).
                // It starts at `startX`.
                // The previous riser was at `prevRiserX` (which is `currentBaseX - WidthOfRow(r-1)`? No.)
                
                // Let's track `previousRiserX`.
                
                double prevRailingInnerFace = 0.0;
                if (r == 0)
                {
                    // Row 0. Ground. No previous railing? 
                    // Or maybe there is one at 0?
                    // Let's assume railing at 0 is at X=0? Not usually.
                    // Start of Row 1 is at getRowWidth(0).
                    // This stair ends at getRowWidth(0).
                    // It starts at getRowWidth(0) - flightRun.
                    // Gap relative to what?
                    // Maybe just 0 for first one if no railing behind.
                    gaps.Add(0.0);
                    
                    // But wait, if inset is applied, startX moved.
                }
                else
                {
                    // We are at Row 'r' (which is the landing of stair r-1).
                    // We are building Stair 'r' (starts on Row r, goes to r+1).
                    // Railing 'r' is at the riser of Row r (where we are). 
                    // Railing 'r' is at `currentBaseX - RowWidth(?))`.
                    // Actually, simple variable tracking is better.
                }
                
                // Let's re-eval Logic for Gap:
                // We need the Gap on the Tread Surface.
                // Surface 'r' is generated in the Tribune Loop.
                // Riser 'r' is at X_r. Railing is at X_r. Inner Face X_r + RailW.
                // Stair 'r+1' starts at X_{r+1} - Run_{r+1}.
                // Gap = (X_{r+1} - Run_{r+1}) - (X_r + RailW).
                
                // BUT the stair loop is building Stair 'r' (0->1).
                // So for r=0, we are building stair 0->1.
                // It starts at X_1 - Run_1.
                // Previous Railing? There isn't one at X_0? 
                
                // Let's Calculate Gap for Row 'r'.
                // Can we do it in the loop?
                // We need:
                // 1. Position of Riser r (Left side of row).
                // 2. Position of Stair r+1 Start (Right side of row).
                
                // This suggests Gaps list should align with Rows.
                
                // Let's just store the start X of the stairs we are creating.
                // `startX` is the start of Stair `r` (0->1).
                // Where is the railing behind it?
                // For r=0, railing is at 0? No.
                // Let's calculate Gap[r] as: Gap on the landing *before* this stair.
                
                // For r=0 (Stair 0->1). Landing is Row 0 (Ground).
                // Railing at Start of Row 0? (X=0).
                // Stair starts at `startX`.
                // Gap = `startX` - (0 + ?).
                
                // For r=1 (Stair 1->2). Landing is Row 1.
                // Railing for Row 1 is at `currentBaseX - RowWidth`.
                // No, `currentBaseX` is the Target (end of this stair).
                // This variable naming is confusing.
                
                // Let's use `tribPts` X coordinates?
                // We know exactly where Risers are.
                // Riser 0: 0.0? No, Riser 1 is at Width0.
                
                // Let's use clean variables.
                 
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

                // Calculate Gap for this Row (The row 'below' this stair).
                // Previous Riser X is `currentBaseX - getRowWidth(r)`.
                // Actually `currentBaseX` starts at `getRowWidth(0)`.
                double prevRiserX = currentBaseX - getRowWidth(r);
                if (r == 0) prevRiserX = 0; // Ground start
                
                // Railing at `prevRiserX`?
                // For r=0, X=0. Is there a railing at X=0? Usually no, it's ground.
                // But for r=1, Riser is at RowWidth(0). Railing is there.
                
                double railInnerFace = prevRiserX + _railings.RailWidth;
                if (r == 0) railInnerFace = 0; // Assume no railing obstruction on ground start?
                
                // Gap = StairStart - RailInnerFace
                double thisGap = startX - railInnerFace;
                gaps.Add(thisGap);

                // Move base to next
                currentBaseX += getRowWidth(r+1); // Next row width
                currentBaseZ = targetLandingZ;
            }

            if (stairPts.Count > 1)
                stairsProfile = new Polyline(stairPts).ToNurbsCurve();
        }
    }
}
