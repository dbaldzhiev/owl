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

        public void Solve(out Curve tribuneProfile, out Curve stairsProfile, out List<Curve> railingProfiles, out List<double> gaps, out SerializedTribune serializedTribune, out List<Line> tribRows, out List<Point3d> rrInt, bool flip = false, Point3d origin = default, List<bool> railingToggles = null, List<AudienceSetup> audiences = null)
        {
            tribuneProfile = null;
            stairsProfile = null;
            railingProfiles = new List<Curve>();
            gaps = new List<double>();
            tribRows = new List<Line>();
            rrInt = new List<Point3d>();
            
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

            // Capture Row 0 Data
            // Front of Row 0 (Railing Interior Intersection)
            double railW_0 = _railings.RailWidth;
            Point3d r0Start = new Point3d(currX + railW_0, 0, currZ); // Offset by RailWidth
            Point3d r0End = new Point3d(currX + row0Width, 0, currZ);
            
            rowPoints.Add(r0Start);
            rrInt.Add(r0Start);
            tribRows.Add(new Line(new Point3d(currX, 0, currZ), r0End)); // Physical row starts at currX

            currX += getRowWidth(0);
            tribPts.Add(new Point3d(currX, 0, currZ));

            // Elevated Rows
            for (int r = 1; r < _tribune.Rows; r++) 
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
                double thisRowWidth = getRowWidth(r);

                // 1. Riser UP
                currZ += rowRise;
                tribPts.Add(new Point3d(currX, 0, currZ));

                // Capture Row Data (Front of Row r)
                double railW = _railings.RailWidth;
                Point3d rStart = new Point3d(currX + railW, 0, currZ); // Offset by RailWidth
                Point3d rEnd = new Point3d(currX + thisRowWidth, 0, currZ);
                
                rowPoints.Add(rStart);
                rrInt.Add(rStart);
                tribRows.Add(new Line(new Point3d(currX, 0, currZ), rEnd)); // Physical row starts at currX

                // --- Generate Railing at this Riser ---
                // Determine if railing is ON for this row 'r'
                // Toggles list index corresponds to row index 'r'
                bool showRailing = true;
                if (railingToggles != null && railingToggles.Count > 0)
                {
                    showRailing = railingToggles[r % railingToggles.Count];
                }

                if (showRailing)
                {
                    double rBottomZ = currZ - rowRise;
                    double rTopZ = currZ + _railings.RailHeight;
                    // railW is already defined above

                    Point3d p0 = new Point3d(currX, 0, rBottomZ);
                    Point3d p1 = new Point3d(currX, 0, rTopZ);
                    Point3d p2 = new Point3d(currX + railW, 0, rTopZ);
                    Point3d p3 = new Point3d(currX + railW, 0, rBottomZ);

                    if (railW < thisRowWidth)
                    {
                        var railRec = new Polyline(new[] { p0, p1, p2, p3, p0 });
                        railingProfiles.Add(railRec.ToNurbsCurve());
                    }
                }

                // 2. Tread FORWARD
                currX += thisRowWidth;
                tribPts.Add(new Point3d(currX, 0, currZ));
            }

            if (tribPts.Count > 1)
                tribuneProfile = new Polyline(tribPts).ToNurbsCurve();
            
            serializedTribune = new SerializedTribune(rowPoints, gaps, false, null, null, origin);

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
                
                // Determine if railing is ON for this row 'r'
                bool showRailing = true;
                if (railingToggles != null && railingToggles.Count > 0)
                {
                    showRailing = railingToggles[r % railingToggles.Count];
                }

                double backOffset = _railings.RailWidth; // Default to rail width (physical presence)
                
                if (!showRailing)
                {
                    // Railing is OFF. Check for Audience limits.
                    if (audiences != null && audiences.Count > 0)
                    {
                        var aud = audiences[r % audiences.Count];
                        if (aud != null)
                        {
                            // "calculated from front rows hard limit line"
                            // Assuming HardBackLimit defines the back boundary of the seating area relative to row start.
                            backOffset = aud.HardBackLimit;
                        }
                    }
                    else
                    {
                        // No audience provided, but railing is off. 
                        // If we assume no physical obstruction, maybe 0?
                        // However, to be safe and avoid zero-width gaps if not intended, 
                        // we'll keep RailWidth as a placeholder or 0 if user wants "pure" gap.
                        // Given user request "when there is no railing the chair gaps are calculated from...",
                        // it implies a specific alternative behavior.
                        // I'll set it to 0 if no audience, meaning gap starts directly at riser.
                        backOffset = 0.0;
                    }
                }
                
                double railInnerFace = prevRiserX + backOffset;
                if (r == 0) railInnerFace = 0; 
                
                double thisGap = startX - railInnerFace;
                gaps.Add(thisGap);

                currentBaseX += getRowWidth(r+1); 
                currentBaseZ = targetLandingZ;
            }

            if (stairPts.Count > 1)
                stairsProfile = new Polyline(stairPts).ToNurbsCurve();

            // -----------------------------
            // C) FLIP LOGIC
            // -----------------------------
            if (flip)
            {
                var mirror = Transform.Mirror(Plane.WorldYZ);

                if (tribuneProfile != null) tribuneProfile.Transform(mirror);
                if (stairsProfile != null) stairsProfile.Transform(mirror);
                
                foreach (var rv in railingProfiles)
                {
                    if (rv != null) rv.Transform(mirror);
                }

                // Flip Rows
                for (int i = 0; i < tribRows.Count; i++)
                {
                    var ln = tribRows[i];
                    ln.Transform(mirror);
                    tribRows[i] = ln;
                }

                // Flip Points
                for (int i = 0; i < rrInt.Count; i++)
                {
                    var pt = rrInt[i];
                    pt.Transform(mirror);
                    rrInt[i] = pt;
                }
                
                // Update Serialized Tribune Points
                var newRowPoints = new List<Point3d>();
                foreach (var pt in serializedTribune.RowPoints)
                {
                    var p = pt;
                    p.Transform(mirror);
                    newRowPoints.Add(p);
                }
                serializedTribune.RowPoints = newRowPoints;
                serializedTribune.Flip = true;
                
                var newOrigin = serializedTribune.Origin;
                newOrigin.Transform(mirror);
                serializedTribune.Origin = newOrigin;
            }

            // -----------------------------
            // D) ORIGIN TRANSLATION
            // -----------------------------
            if (origin != Point3d.Origin)
            {
                var move = Transform.Translation(new Vector3d(origin));

                if (tribuneProfile != null) tribuneProfile.Transform(move);
                if (stairsProfile != null) stairsProfile.Transform(move);

                foreach (var rv in railingProfiles)
                {
                    if (rv != null) rv.Transform(move);
                }

                for (int i = 0; i < tribRows.Count; i++)
                {
                    var ln = tribRows[i];
                    ln.Transform(move);
                    tribRows[i] = ln;
                }

                for (int i = 0; i < rrInt.Count; i++)
                {
                    var pt = rrInt[i];
                    pt.Transform(move);
                    rrInt[i] = pt;
                }

                // Update Serialized Tribune Points
                var movedRowPoints = new List<Point3d>();
                foreach (var pt in serializedTribune.RowPoints)
                {
                    var p = pt;
                    p.Transform(move);
                    movedRowPoints.Add(p);
                }
                serializedTribune.RowPoints = movedRowPoints;
                
                var newOrigin = serializedTribune.Origin;
                // Origin was already passed as 'origin' argument to constructor, so it matches the transformation target?
                // Wait, 'origin' arg is the target origin. 
                // The geometry is built at (0,0) then moved to 'origin'.
                // So the final origin IS 'origin'.
                // If we passed 'origin' to constructor, it is already correct.
                // WE DO NOT NEED TO TRANSFORM IT AGAIN if it was set to 'origin' initially.
                // However, the Move logic here applies the transform to the points.
                // If we construct STribune with 'origin', it effectively sets the property.
                // But the points were built at 0,0 then moved. 
                // Let's check logic order.
                // 1. Build at 0,0.
                // 2. Create STrib(pts, ..., origin). -> Origin property = origin.
                // 3. Move points by Translation(origin).
                // So STrib.Origin is 'origin'. Points are at 'origin'.
                // Relative = Point - Origin.
                // (0+Origin) - Origin = 0. Correct.
                
                // EXCEPT: if 'origin' != 0, we move the points.
                // The constructor call was `new SerializedTribune(..., origin)`.
                // So STrib.Origin is ALREADY set to the target origin.
                // We don't need to transform it.
            }
        }
    }
}
