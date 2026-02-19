using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using Owl.Core.Primitives;

namespace Owl.Core.Solvers
{
    public class TribuneSolver
    {
        private readonly StairTribuneSetup _stairTribune;
        private readonly RailingSetup _railings;

        public TribuneSolver(StairTribuneSetup stairTribune, RailingSetup railings)
        {
            _stairTribune = stairTribune ?? throw new ArgumentNullException(nameof(stairTribune));
            _railings = railings ?? throw new ArgumentNullException(nameof(railings));
        }

        public TribuneSolution Solve(
            bool flip = false,
            List<bool> railingToggles = null,
            List<AudienceSetup> audiences = null,
            List<double> audienceOffsets = null,
            HallSetup hallSetup = null,
            DisabledSeatsSetup disabledSeats = null,
            List<int> audienceMap = null)
        {
            var solution = new TribuneSolution();
            solution.Flipped = flip;
            solution.RailingToggles = railingToggles ?? new List<bool>();
            const double tol = 0.001;

            // Valid setup check
            if (hallSetup == null)
            {
                 solution.IsValid = false;
                 solution.Errors.Add("HallSetup is required.");
                 return solution;
            }

             // Check if PlanFrame origin is on the Tribune Boundary
             if (hallSetup.TribuneBoundary != null)
             {
                 double t;
                 // Using a relaxed tolerance for "OnCurve" check
                 // Users might not snap perfectly.
                 double validationTol = 0.1; 
                 if (!hallSetup.TribuneBoundary.ClosestPoint(hallSetup.PlanFrame.Origin, out t, validationTol))
                 {
                     // Not found? Or check distance
                     Point3d cp = hallSetup.TribuneBoundary.PointAt(t);
                     if (cp.DistanceTo(hallSetup.PlanFrame.Origin) > validationTol)
                     {
                         solution.IsValid = false;
                         solution.Errors.Add($"HallSetup.PlanFrame Origin is not on the Tribune Boundary (Distance > {validationTol}).");
                         return solution;
                     }
                 }
                 // If found, it's consistent.
             }

            // Store HallSetup for Visualization
            solution.HallSetup = hallSetup;
            solution.BasePlane = Plane.WorldXY; // Canonical

            var tribune = _stairTribune.Tribune;
            var stairs = _stairTribune.Stairs;
            
            if (tribune.Rows <= 0)
            {
                solution.IsValid = true;
                return solution;
            }

            // -----------------------------
            // A) TRIBUNE PROFILE GENERATION (Canonical Space: 0,0,0 growing +X, +Z)
            // -----------------------------
            var tribPts = new List<Point3d>();
            double currX = 0.0;
            double currZ = 0.0;
            tribPts.Add(new Point3d(currX, 0, currZ));

            Func<int, double> getRowWidth = (i) => {
                if (tribune.RowWidths == null || tribune.RowWidths.Count == 0) return 0.8;
                return tribune.RowWidths[i % tribune.RowWidths.Count];
            };

            // ... (Row 0 Logic similar to before) ...
            double railW_0 = _railings.RailWidth;
            Point3d r0Start = new Point3d(currX + railW_0, 0, currZ);
            
            solution.RowPoints.Add(r0Start);
            solution.RowLocalPoints.Add(r0Start);
            solution.SectionRowSpine.Add(r0Start);

            currX += getRowWidth(0);
            tribPts.Add(new Point3d(currX, 0, currZ));
            
            var resolvedToggles = new List<bool> { true };

            // ELEVATED ROWS
            for (int r = 1; r < tribune.Rows; r++)
            {
                // ... (Logic for rise/run) ...
                int idx = r - 1;
                int count = 1;
                if (tribune.ElevCounts != null && tribune.ElevCounts.Count > 0)
                {
                    if (idx < tribune.ElevCounts.Count) count = tribune.ElevCounts[idx];
                    else count = tribune.ElevCounts[tribune.ElevCounts.Count - 1];
                }
                if (count < 1) count = 1;

                double rowRise = count * stairs.TreadHeight;
                double thisRowWidth = getRowWidth(r);

                currZ += rowRise;
                tribPts.Add(new Point3d(currX, 0, currZ));

                bool showRailing = true;
                if (railingToggles != null && railingToggles.Count > 0)
                    showRailing = railingToggles[r % railingToggles.Count];
                resolvedToggles.Add(showRailing);

                double railW = _railings.RailWidth;
                Point3d rStart = new Point3d(currX + railW, 0, currZ);
                solution.RowPoints.Add(rStart);
                solution.RowLocalPoints.Add(rStart);

                if (showRailing) solution.SectionRowSpine.Add(rStart);
                else if (audiences != null && audiences.Count > 0 && r > 0)
                {
                    var prevAud = audiences[(r - 1) % audiences.Count];
                    double prevHardBackX = solution.RowPoints[r - 1].X + (prevAud != null ? prevAud.SecHBL : 0);
                    solution.SectionRowSpine.Add(new Point3d(prevHardBackX, 0, currZ));
                }
                else solution.SectionRowSpine.Add(rStart);

                if (showRailing)
                {
                    double rBottomZ = currZ - rowRise;
                    double rTopZ = currZ + _railings.RailHeight;
                    Point3d p0 = new Point3d(currX, 0, rBottomZ);
                    Point3d p1 = new Point3d(currX, 0, rTopZ);
                    Point3d p2 = new Point3d(currX + railW, 0, rTopZ);
                    Point3d p3 = new Point3d(currX + railW, 0, rBottomZ);

                    if (railW < thisRowWidth)
                    {
                        var railRec = new Polyline(new[] { p0, p1, p2, p3, p0 });
                        solution.SectionRailings.Add(railRec.ToNurbsCurve());
                        solution.SectionRailingsSpine.Add(new Point3d(currX + railW / 2.0, 0, rTopZ));
                    }
                }

                currX += thisRowWidth;
                tribPts.Add(new Point3d(currX, 0, currZ));
            }

            if (tribPts.Count > 1)
                solution.SectionTribuneProfile = new Polyline(tribPts).ToNurbsCurve();

            solution.RailingToggles = resolvedToggles;

            // ... (B) Stairs Profile: Same Logic applied locally ...
            // (Reusing existing Stair Logic simplified)
            var stairPts = new List<Point3d>();
            double currentBaseX = getRowWidth(0);
            double currentBaseZ = 0.0;

            for (int r = 0; r < tribune.Rows - 1; r++)
            {
                 // ... Copy logic ...
                 int count = 1;
                 if (tribune.ElevCounts != null && tribune.ElevCounts.Count > 0)
                 {
                     int idx = r;
                     if (idx < tribune.ElevCounts.Count) count = tribune.ElevCounts[idx];
                     else count = tribune.ElevCounts[tribune.ElevCounts.Count - 1];
                 }
                 if (count < 1) count = 1;
                 
                double rise = stairs.TreadHeight;
                double run = stairs.TreadWidth;
                bool inset = false;
                if (tribune.StairInsets != null && tribune.StairInsets.Count > 0) inset = tribune.StairInsets[r % tribune.StairInsets.Count];
                double insetVal = inset ? _railings.RailWidth : 0.0;
                double flightRun = (count - 1) * run;
                double startX = currentBaseX - flightRun + insetVal;
                double targetLandingZ = currentBaseZ + (count * rise);

                if (stairPts.Count > 0)
                {
                    Point3d lastPt = stairPts.Last();
                    if (Math.Abs(lastPt.X - startX) > tol || Math.Abs(lastPt.Z - currentBaseZ) > tol)
                        stairPts.Add(new Point3d(startX, 0, currentBaseZ));
                }
                else stairPts.Add(new Point3d(startX, 0, currentBaseZ));

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
                solution.StairFlightStartX.Add(startX);
                solution.StairFlightEndX.Add(cx);
                
                // Gap Calculation ...
                double prevRiserX = (r == 0) ? 0 : currentBaseX - getRowWidth(r);
                bool showRailing = resolvedToggles[r % resolvedToggles.Count];
                double referenceX;
                if (showRailing || r == 0) referenceX = (r == 0) ? 0.0 : prevRiserX + _railings.RailWidth;
                else
                {
                    double prevRowX = solution.RowPoints[r - 1].X;
                    double prevHBL = 0;
                    if (audiences != null && audiences.Count > 0)
                    {
                        var prevAud = audiences[(r - 1) % audiences.Count];
                        if (prevAud != null) prevHBL = prevAud.SecHBL;
                    }
                    referenceX = prevRowX + prevHBL;
                }
                solution.Gaps.Add(startX - referenceX);

                currentBaseX += getRowWidth(r + 1);
                currentBaseZ = targetLandingZ;
            }
            if (stairPts.Count > 1) solution.SectionStairsProfile = new Polyline(stairPts).ToNurbsCurve();


            // -----------------------------
            // C) CANONICAL FLIP LOGIC (Mirror only)
            // -----------------------------
            // If flip, we mirror all generated geometry across WorldYZ (X -> -X).
            // This is purely internal Canonical state.
            
            Transform mirror = flip ? Transform.Mirror(Plane.WorldYZ) : Transform.Identity;
            
            if (flip)
            {
                 if (solution.SectionTribuneProfile != null) solution.SectionTribuneProfile.Transform(mirror);
                 if (solution.SectionStairsProfile != null) solution.SectionStairsProfile.Transform(mirror);
                 foreach (var c in solution.SectionRailings) c.Transform(mirror);
                 for (int i=0; i<solution.SectionRailingsSpine.Count; i++) { var p = solution.SectionRailingsSpine[i]; p.Transform(mirror); solution.SectionRailingsSpine[i] = p; }
                 for (int i=0; i<solution.SectionRowSpine.Count; i++) { var p = solution.SectionRowSpine[i]; p.Transform(mirror); solution.SectionRowSpine[i] = p; }
                 for (int i=0; i<solution.RowPoints.Count; i++) { var p = solution.RowPoints[i]; p.Transform(mirror); solution.RowPoints[i] = p; }
                 for (int i=0; i<solution.RowLocalPoints.Count; i++) { var p = solution.RowLocalPoints[i]; p.Transform(mirror); solution.RowLocalPoints[i] = p; }
                 // Need to flip stair start/end X?
                 // StairFlightStartX are doubles. If flip, X -> -X.
                 // Swap Start/End if needed? No, just transform value.
                 for(int i=0; i<solution.StairFlightStartX.Count; i++) solution.StairFlightStartX[i] = -solution.StairFlightStartX[i];
                 for(int i=0; i<solution.StairFlightEndX.Count; i++) solution.StairFlightEndX[i] = -solution.StairFlightEndX[i];
            }


            // -----------------------------
            // D) PROJECTOR & SCREEN (Canonical Generation - Early for Sightlines)
            // -----------------------------
            // We need these before generating chairs to calculate sightlines.
            if (hallSetup != null)
            {
                 // Canonical Target: WorldXZ (X=Run, Z=Rise). matches Tribune profile.
                 Plane canTarget = new Plane(Point3d.Origin, Vector3d.XAxis, Vector3d.ZAxis);
                 
                 // Map: SectionFrame -> Canonical.
                 Transform toCan = Transform.PlaneToPlane(hallSetup.SectionFrame, canTarget);

                 // Transform mirror = flip ? Transform.Mirror(Plane.WorldYZ) : Transform.Identity; // Already defined above

                 if (hallSetup.ProjectorLocation != Point3d.Unset)
                 {
                     Point3d projOnSec = hallSetup.SectionFrame.ClosestPoint(hallSetup.ProjectorLocation);
                     projOnSec.Transform(toCan);
                     if(flip) projOnSec.Transform(mirror);
                     solution.SectionProjector = projOnSec;
                 }
                     
                 if (hallSetup.ScreenCurve != null)
                 {
                     var sc = Curve.ProjectToPlane(hallSetup.ScreenCurve, hallSetup.SectionFrame);
                     sc.Transform(toCan);
                     if(flip) sc.Transform(mirror);
                     solution.SectionScreen = sc;
                 }
            }


            // -----------------------------
            // E) SECTION CHAIRS & SIGHTLINES (Canonical)
            // -----------------------------
            if (audiences != null && audiences.Count > 0)
            {
                // Initialize Categorized Collections
                int typeCount = audiences.Count + 1;
                solution.CategorizedSectionChairs = new List<List<Curve>>();
                solution.CategorizedSectionChairPlanes = new List<List<Plane>>();
                for (int t = 0; t < typeCount; t++)
                {
                    solution.CategorizedSectionChairs.Add(new List<Curve>());
                    solution.CategorizedSectionChairPlanes.Add(new List<Plane>());
                }

                // Find Screen Bottom Z (lowest point) if screen exists
                Point3d screenTarget = Point3d.Unset;
                if (solution.SectionScreen != null)
                {
                    Point3d p1 = solution.SectionScreen.PointAtStart;
                    Point3d p2 = solution.SectionScreen.PointAtEnd;
                    screenTarget = (p1.Z < p2.Z) ? p1 : p2;
                }

                for (int i = 0; i < solution.RowLocalPoints.Count; i++)
                {
                    // Determine Type for this row
                    int rowTypeIdx = -1;
                    if (audienceMap != null && i < audienceMap.Count)
                        rowTypeIdx = audienceMap[i];
                    else
                        rowTypeIdx = (i % audiences.Count) + 1;

                    // Support Row 0 Disabled seat override in section view (shows the disabled chair type)
                    int seatType = rowTypeIdx;
                    if (i == 0 && disabledSeats != null && disabledSeats.Count > 0 && disabledSeats.Setup != null)
                        seatType = 0; // Use disabled type for section representation if present on row 0

                    AudienceSetup aud = null;
                    if (seatType == 0 && disabledSeats != null) aud = disabledSeats.Setup;
                    else if (seatType > 0 && seatType <= audiences.Count) aud = audiences[seatType - 1];

                    if (aud == null) 
                    {
                        solution.SectionChairs.Add(new List<Curve>());
                        solution.SectionChairPlanes.Add(Plane.Unset);
                        continue;
                    }

                    Point3d rowPt = solution.RowLocalPoints[i];
                    double offsetVal = (audienceOffsets != null && i < audienceOffsets.Count) ? audienceOffsets[i] : 0;
                    
                    Vector3d forward = flip ? -Vector3d.XAxis : Vector3d.XAxis;
                    Vector3d yDir = flip ? Vector3d.YAxis : -Vector3d.YAxis; 
                    
                    Point3d placePt = rowPt + (forward * offsetVal);
                    Plane placePlane = new Plane(placePt, forward, Vector3d.ZAxis); 
                    
                    Transform toCanonical = Transform.PlaneToPlane(aud.SecOriginPlane, placePlane);
                    
                    var rowChairs = new List<Curve>();
                    foreach (var c in aud.SecChairGeo)
                    {
                         if(c!=null)
                         {
                             var dc = c.DuplicateCurve();
                             dc.Transform(toCanonical);
                             rowChairs.Add(dc);
                         }
                    }
                    
                    solution.SectionChairPlanes.Add(placePlane);
                    solution.SectionChairs.Add(rowChairs);

                    // Add to categorized lists
                    if (seatType >= 0 && seatType < solution.CategorizedSectionChairs.Count)
                    {
                        solution.CategorizedSectionChairPlanes[seatType].Add(placePlane);
                        foreach (var c in rowChairs)
                        {
                            solution.CategorizedSectionChairs[seatType].Add(c.DuplicateCurve());
                        }
                    }

                    // Limits (Vertical Lines relative to Chair Frame)
                    List<Line> rowLimits = new List<Line>();
                    double extLen = 100.0;
                    Vector3d upVec = Vector3d.ZAxis; 
                    
                    Point3d startFL = placePt + (forward * aud.SecFL);
                    rowLimits.Add(new Line(startFL, startFL + (upVec * extLen)));
                    
                    Point3d startHBL = placePt + (forward * aud.SecHBL);
                    rowLimits.Add(new Line(startHBL, startHBL + (upVec * extLen)));
                    
                    Point3d startSBL = placePt + (forward * aud.SecSBL);
                    rowLimits.Add(new Line(startSBL, startSBL + (upVec * extLen)));
                    
                    solution.SectionLimitLines.Add(rowLimits);

                    // ---------------------------------------------------------
                    // CLEARANCE CALCULATION (Chair)
                    // ---------------------------------------------------------
                    Point3d myFront = placePt + (forward * aud.SecFL); 
                    Point3d obstruction = Point3d.Unset;
                    
                    int obsIdx = -1;
                    int count = solution.RowLocalPoints.Count;
                    
                    if (count > 1)
                    {
                        double x0 = ((Vector3d)solution.RowLocalPoints[0]) * forward;
                        double xN = ((Vector3d)solution.RowLocalPoints[count-1]) * forward;
                        bool zeroIsFront = (x0 < xN); 
                        
                        if (zeroIsFront) { if (i > 0) obsIdx = i - 1; }
                        else { if (i < count - 1) obsIdx = i + 1; }
                    }

                    bool hasRail = (i < solution.RailingToggles.Count && solution.RailingToggles[i]);

                    if (hasRail)
                    {
                         obstruction = rowPt; 
                    }
                    else if (obsIdx != -1)
                    {
                         int prevIdx = obsIdx;
                         if(prevIdx < solution.SectionChairPlanes.Count)
                         {
                             Plane prevPlane = solution.SectionChairPlanes[prevIdx];
                             
                             // Get Setup of obstruction row
                             int obsSeatType = -1;
                             if (audienceMap != null && prevIdx < audienceMap.Count) obsSeatType = audienceMap[prevIdx];
                             else obsSeatType = (prevIdx % audiences.Count) + 1;
                             if (prevIdx == 0 && disabledSeats != null && disabledSeats.Count > 0 && disabledSeats.Setup != null) obsSeatType = 0;

                             AudienceSetup prevAud = null;
                             if (obsSeatType == 0 && disabledSeats != null) prevAud = disabledSeats.Setup;
                             else if (obsSeatType > 0 && obsSeatType <= audiences.Count) prevAud = audiences[obsSeatType - 1];

                             if (prevAud != null && prevPlane.IsValid)
                             {
                                 obstruction = prevPlane.Origin + (forward * prevAud.SecHBL);
                             }
                         }
                    }
                    
                    if (obstruction != Point3d.Unset)
                    {
                        double val = (myFront - obstruction) * forward; 
                        solution.ChairClearances.Add(val);
                        
                        Point3d p2 = myFront + Vector3d.ZAxis * 20.0;
                        Point3d p1 = new Point3d(obstruction.X, obstruction.Y, p2.Z);
                        solution.ChairClearanceDims.Add(new LineCurve(p2, p1));
                        
                        solution.Clashes.Add(p1);
                        solution.Clashes.Add(p2);
                    }
                    else
                    {
                        solution.ChairClearances.Add(0.0);
                        solution.ChairClearanceDims.Add(null);
                    }

                    // Sightlines
                    if (screenTarget != Point3d.Unset)
                    {
                        Point3d eye = aud.EyeLocation;
                        eye.Transform(toCanonical);
                        solution.SectionSightlines.Add(new LineCurve(eye, screenTarget));
                    }
                }
            }


            // -----------------------------
            // STAIR CLEARANCES (From Gaps)
            // -----------------------------
            if (solution.Gaps.Count > 0 && solution.StairFlightStartX.Count == solution.Gaps.Count)
            {
                solution.StairClearances = new List<double>(solution.Gaps);
                
                for(int k=0; k<solution.Gaps.Count; k++)
                {
                    double gap = solution.Gaps[k];
                    double sX = solution.StairFlightStartX[k]; // Flipped if needed
                    double z = (k < solution.RowPoints.Count) ? solution.RowPoints[k].Z : 0;
                    
                    Point3d pStart, pEnd;
                    if (flip)
                    {
                         // Flipped (X negative). Uphill is -X (more negative).
                         // Gap is before Stair Start (less negative X).
                         pStart = new Point3d(sX + gap, 0, z+10);
                         pEnd = new Point3d(sX, 0, z+10);
                    }
                    else
                    {
                         // Normal (X positive). Uphill is +X (more positive).
                         // Gap is before Stair Start (less positive X).
                         pStart = new Point3d(sX - gap, 0, z+10);
                         pEnd = new Point3d(sX, 0, z+10);
                    }
                    solution.StairClearanceDims.Add(new LineCurve(pStart, pEnd));
                }
            }
            
            // -----------------------------
            // F) PLAN GENERATION (Canonical -> Target Plan Frame)
            // -----------------------------
            if (hallSetup != null && hallSetup.TribuneBoundary != null && hallSetup.TribuneBoundary.IsValid)
            {
                GeneratePlan(solution, hallSetup, audiences, audienceOffsets, tol, flip, disabledSeats, audienceMap);
            }
            
            // Store Inputs
            solution.Audiences = audiences ?? new List<AudienceSetup>();
            solution.AudienceOffsets = audienceOffsets ?? new List<double>();
            
            solution.IsValid = true;
            return solution;
        }

        private void GeneratePlan(
            TribuneSolution solution,
            HallSetup hallSetup,
            List<AudienceSetup> audiences,
            List<double> audienceOffsets,
            double tol,
            bool flip,
            DisabledSeatsSetup disabledSeats,
            List<int> audienceMap)
        {
            // Plan Logic:
            // 1. Map Hall Boundaries to Canonical (WorldXY).
            Plane sourceFrame = hallSetup.PlanFrame;
            Transform mapToCanonical = Transform.PlaneToPlane(sourceFrame, Plane.WorldXY);
            Transform mirror = flip ? Transform.Mirror(Plane.WorldYZ) : Transform.Identity;
            
            var tribBnd = hallSetup.TribuneBoundary.DuplicateCurve();
            tribBnd.Transform(mapToCanonical);
            if (flip) tribBnd.Transform(mirror);
            
            var clipRegions = new List<Curve>();
            var mapAisles = hallSetup.AisleBoundaries.Select(c => { var d=c.DuplicateCurve(); d.Transform(mapToCanonical); if(flip) d.Transform(mirror); return d; }).ToList();
            var mapTunnels = hallSetup.TunnelBoundaries.Select(c => { var d=c.DuplicateCurve(); d.Transform(mapToCanonical); if(flip) d.Transform(mirror); return d; }).ToList();
            
            if (tribBnd.IsClosed)
            {
                 foreach(var a in mapAisles) { var i = Curve.CreateBooleanIntersection(tribBnd, a, tol); if(i!=null) clipRegions.AddRange(i); }
                 if(mapTunnels.Count > 0 && clipRegions.Count > 0)
                 {
                     var cleaned = new List<Curve>();
                     foreach(var r in clipRegions)
                     {
                         var parts = new List<Curve>{r};
                         foreach(var t in mapTunnels) 
                         {
                             var np = new List<Curve>();
                             foreach(var p in parts) { var d=Curve.CreateBooleanDifference(p,t,tol); if(d!=null) np.AddRange(d); }
                             parts = np;
                         }
                         cleaned.AddRange(parts);
                     }
                     clipRegions = cleaned;
                 }
            }
            
            Plane worldXY = Plane.WorldXY;
            var deviations = new List<Curve>();
            deviations.AddRange(mapTunnels);
            deviations.AddRange(clipRegions);

            Func<Curve, List<Curve>> keepOutside = (crv) => TrimCurve(crv, deviations, false, worldXY, tol); 
            Func<Curve, List<Curve>> keepInside = (crv) => TrimCurve(crv, clipRegions, true, worldXY, tol);
            
            Func<double, List<Curve>> getPlanLinesAtX = (x) => 
            {
                // Robust Intersect: Use a large spanning line to handle concave shapes
                var bbox = tribBnd.GetBoundingBox(true);
                if (!bbox.IsValid) return new List<Curve>();
                
                Point3d p1 = new Point3d(x, bbox.Min.Y - 1000, 0);
                Point3d p2 = new Point3d(x, bbox.Max.Y + 1000, 0);
                var span = new LineCurve(p1, p2);
                
                return TrimCurve(span, new List<Curve> { tribBnd }, true, Plane.WorldXY, tol);
            };
            
            // 1. Tribune Lines (Using Actual Risers from Section)
            var riserX = new List<double>();
            if (solution.SectionTribuneProfile != null && solution.SectionTribuneProfile.TryGetPolyline(out Polyline poly))
            {
                foreach (var p in poly)
                {
                    if (!riserX.Any(rx => Math.Abs(rx - p.X) < tol)) riserX.Add(p.X);
                }
            }

            foreach(var x in riserX) 
            {
                var lines = getPlanLinesAtX(x);
                foreach(var ln in lines)
                {
                    var segs = keepOutside(ln);
                    foreach(var s in segs) solution.PlanTribuneLines.Add(s); 
                }
            }
            
            // 2. Stairs
            if(solution.StairFlightStartX!=null)
            {
                double run = _stairTribune.Stairs.TreadWidth;
                for(int i=0; i<solution.StairFlightStartX.Count; i++)
                {
                    double sx = solution.StairFlightStartX[i];
                    double ex = solution.StairFlightEndX[i];
                    // sx, ex are flipped if needed.
                    
                    double dx = ex - sx;
                    int n = (int)Math.Round(Math.Abs(dx)/run);
                    double dir = Math.Sign(dx);
                    if(dir == 0) dir = 1;

                    for(int k=0; k<=n; k++)
                    {
                        double x = sx + k * run * dir; 
                        // Logic verified: if flipped, sx > ex (negative). dir = -1. k*run*(-1) -> reduces x. Correct.
                        
                        var lines = getPlanLinesAtX(x);
                        foreach(var ln in lines)
                        {
                            var s=keepInside(ln); 
                            foreach(var c in s) solution.PlanStairLines.Add(c); 
                        }
                    }
                }
            }
            
            // 3. Railings
            double rW = _railings.RailWidth;
            for(int r=1; r<solution.RowLocalPoints.Count; r++) // Skip Row 0 (Front Row)
            {
                if(r < solution.RailingToggles.Count && solution.RailingToggles[r])
                {
                    double cx = solution.RowLocalPoints[r].X;
                    // Move center "forward" (away from row start/riser?)
                    // Current RowLocalPoint is at "Start" (currX + railW).
                    // Railing occupies [Start-W, Start].
                    // Center needs to be at Start - W/2.
                    
                    // If flip (X negative): Start is at -X. Forward is -X.
                    // Start - (-Forward * W/2)? No.
                    // Start is more negative.
                    // Railing [Start, Start + W] (towards origin)?
                    // Section Logic: [currX, currX+W]. RowPoint = currX+W.
                    // So Railing is [RowPoint-W, RowPoint]. Center = RowPoint - W/2.
                    
                    // IF FLIPPED:
                    // Section Logic: [-currX-W, -currX]. RowPoint = -currX-W.
                    // Railing is [RowPoint, RowPoint+W]. Center = RowPoint + W/2.
                    
                    double offset = flip ? rW/2.0 : -rW/2.0; 
                    double targetX = cx + offset;
                    
                    var lines = getPlanLinesAtX(targetX);
                    foreach(var ln in lines)
                    {
                        var segs = keepOutside(ln);
                        foreach(var seg in segs)
                        {
                            var rect = RectFromSpine(seg, rW, tol, flip); 
                            if(rect!=null) solution.PlanRailings.Add(rect); 
                        }
                    }
                }
            }
            
            // 4. Chairs
            if (audiences != null && audiences.Count > 0)
            {
                // Initialize Categorized Collections
                // Count = audiences.Count + 1 (for Type 0 = Disabled)
                int typeCount = audiences.Count + 1;
                solution.CategorizedPlanChairs = new List<List<Curve>>();
                solution.CategorizedPlanChairPlanes = new List<List<Plane>>();
                for (int t = 0; t < typeCount; t++)
                {
                    solution.CategorizedPlanChairs.Add(new List<Curve>());
                    solution.CategorizedPlanChairPlanes.Add(new List<Plane>());
                }

                for (int i = 0; i < solution.RowLocalPoints.Count; i++)
                {
                    double baseX = solution.RowLocalPoints[i].X;
                    double offVal = (audienceOffsets != null && audienceOffsets.Count > i) ? audienceOffsets[i] : 0;
                    double targetX = baseX + (flip ? -offVal : offVal);
                    Vector3d forward = flip ? -Vector3d.XAxis : Vector3d.XAxis;

                    var lines = getPlanLinesAtX(targetX);
                    if (lines == null || lines.Count == 0) continue;

                    var allSegments = new List<Curve>();
                    foreach(var ln in lines) allSegments.AddRange(keepOutside(ln));
                    if (allSegments.Count == 0) continue;

                    int rowTypeIdx = -1;
                    if (audienceMap != null && i < audienceMap.Count) rowTypeIdx = audienceMap[i];
                    else rowTypeIdx = (i % audiences.Count) + 1;

                    int seatIndexInRow = 0;
                    var rowChairs = new List<Curve>();
                    var rowPlanes = new List<Plane>();

                    // Helper to add chair to solution
                    Action<Plane, AudienceSetup, int> addChair = (p, setup, typeIdx) =>
                    {
                        var transformedGeo = new List<Curve>();
                        foreach (var c in setup.PlanChairGeo)
                        {
                            if (c != null)
                            {
                                var dc = c.DuplicateCurve();
                                dc.Transform(Transform.PlaneToPlane(setup.PlanChairOriginPlane, p));
                                transformedGeo.Add(dc);
                            }
                        }
                        rowChairs.AddRange(transformedGeo);
                        rowPlanes.Add(p);

                        if (typeIdx >= 0 && typeIdx < solution.CategorizedPlanChairs.Count)
                        {
                            solution.CategorizedPlanChairPlanes[typeIdx].Add(p);
                            foreach (var c in transformedGeo) solution.CategorizedPlanChairs[typeIdx].Add(c.DuplicateCurve());
                        }
                    };

                    // Check for Disabled Seats on Row 0
                    bool hasManualDisabled = (i == 0 && disabledSeats != null && disabledSeats.Count > 0 && disabledSeats.Setup != null);
                    
                    double totalLen = allSegments.Sum(s => s.GetLength());
                    double startDistD = -1;
                    double endDistD = -1;

                    if (hasManualDisabled)
                    {
                        double dWidth = disabledSeats.Setup.PlanChairWidth;
                        double dLen = disabledSeats.Count * dWidth;
                        double slack = totalLen - dLen;
                        double distParam = disabledSeats.Distribution;
                        startDistD = slack * distParam;
                        endDistD = startDistD + dLen;
                    }

                    double currentDistAcrossRow = 0.0;
                    foreach (var seg in allSegments)
                    {
                        if (seg == null) continue;
                        double segLen = seg.GetLength();

                        // 1. Calculate how many chairs fit in this segment to center them.
                        // We need to keep track of the setups for the sequence.
                        var setupsForThisSeg = new List<AudienceSetup>();
                        var typesForThisSeg = new List<int>();
                        double currentRequiredLen = 0;
                        int currentSeatCount = 0;

                        while (true)
                        {
                            int seatType = rowTypeIdx;
                            if (hasManualDisabled)
                            {
                                // We need a way to predict the global position. 
                                // Let's estimate using axial width of the NEXT seat.
                                double predictedGlobalEnd = currentDistAcrossRow + currentRequiredLen;
                                // Wait, the "fit" loop is tricky because we don't know the exact position until we know how many chairs.
                                // But if we assume we're packing them, we can estimate.
                                // Actually, let's just use the logic: AxialWidth is the distance between centers.
                                // Footprint is ActualWidth.
                            }

                            // To avoid complex lookahead, let's just simulate the placement.
                            int nextSeatType = rowTypeIdx;
                            if (hasManualDisabled)
                            {
                                // Estimation: if we add a chair, where does its center fall?
                                // If first: center = ActualWidth/2.
                                // If subsequent: center = prevCenter + (prevAxial + currAxial)/2.
                            }

                            // Let's simplify: Get the setup first.
                            // We need to know the type for seatIndexInRow + currentSeatCount.
                            // But Rowan indices are not passed here.
                            // For now, let's use the local seat index within row.
                        
                            // Correct logic for manual disabled:
                            Func<double, int> getSeatTypeAtPos = (globalPos) => {
                                if (hasManualDisabled && globalPos >= startDistD - tol && globalPos < endDistD - tol) return 0;
                                return rowTypeIdx;
                            };

                            // Simulation loop
                            double lastCenter = 0;
                            double lastAxial = 0;
                            double currentFootprintEnd = 0;
                            var tempSetups = new List<AudienceSetup>();
                            var tempTypes = new List<int>();

                            while (true)
                            {
                                int sType = getSeatTypeAtPos(currentDistAcrossRow + currentFootprintEnd); // Rough estimate
                                AudienceSetup s = (sType == 0 && disabledSeats != null) ? disabledSeats.Setup : (sType > 0 && sType <= audiences.Count ? audiences[sType - 1] : null);
                                if (s == null) break;

                                double thisCenter;
                                if (tempSetups.Count == 0) thisCenter = s.ActualWidth / 2.0;
                                else thisCenter = lastCenter + (lastAxial + s.PlanChairWidth) / 2.0;

                                double thisEnd = thisCenter + s.ActualWidth / 2.0;
                                if (thisEnd > segLen + tol) break;

                                lastCenter = thisCenter;
                                lastAxial = s.PlanChairWidth;
                                currentFootprintEnd = thisEnd;
                                tempSetups.Add(s);
                                tempTypes.Add(sType);
                            }

                            // 2. Centering and Placement
                            double slack = segLen - currentFootprintEnd;
                            double shift = slack / 2.0;
                            if (shift < 0) shift = 0;

                            double placingCenter = shift;
                            AudienceSetup prevS = null;

                            for (int k = 0; k < tempSetups.Count; k++)
                            {
                                var aud = tempSetups[k];
                                var sType = tempTypes[k];

                                if (prevS == null) placingCenter += aud.ActualWidth / 2.0;
                                else placingCenter += (prevS.PlanChairWidth + aud.PlanChairWidth) / 2.0;

                                double t;
                                if (seg.LengthParameter(placingCenter, out t))
                                {
                                    Point3d pt = seg.PointAt(t);
                                    Plane pp = new Plane(pt, forward, Vector3d.YAxis);
                                    addChair(pp, aud, sType);
                                }

                                prevS = aud;
                                seatIndexInRow++;
                            }

                            break; // We did the whole segment in one nested logic
                        }
                        currentDistAcrossRow += segLen;
                    }

                    solution.PlanChairs.Add(rowChairs);
                    solution.PlanChairPlanes.Add(rowPlanes);
                }
            }
        }
        
        private List<Curve> TrimCurve(Curve curve, List<Curve> regions, bool keepInside, Plane plane, double tol)
        {
             if (regions == null || regions.Count == 0) return keepInside ? new List<Curve>() : new List<Curve> { curve };
             
             var events = new List<double>();
             foreach(var reg in regions)
             {
                 var ccx = Intersection.CurveCurve(curve, reg, tol, tol);
                 if (ccx != null) events.AddRange(ccx.Select(e => e.ParameterA));
             }
             
             var splits = curve.Split(events);
             if (splits == null || splits.Length == 0) splits = new[] { curve };
             
             var result = new List<Curve>();
             foreach(var seg in splits)
             {
                 var pt = seg.PointAtNormalizedLength(0.5);
                 bool inside = false;
                 foreach(var reg in regions)
                 {
                     if (reg.Contains(pt, plane, tol) == PointContainment.Inside)
                     {
                         inside = true;
                         break;
                     }
                 }
                 
                 if (keepInside == inside) result.Add(seg);
             }
             return result;
        }

        private Point3d GetOrigin()
        {
            // Helper to access origin passed to Solve? 
            // Actually I don't store it in class. It's in Solve args.
            // I need to pass it to GeneratePlan or store locally.
            // Returning Point3d.Origin for now, effectively ignoring it in the helper 
            // since I implemented "Cut Plane" logic which works in World Space directly.
            return Point3d.Origin; 
        }

        private static Curve RectFromSpine(Curve spineCrv, double width, double tol, bool flip)
        {
            if (spineCrv == null || !spineCrv.IsValid) return null;
            double L = spineCrv.GetLength();
            if (L <= tol) return null;

            var t0 = spineCrv.Domain.T0;
            var t1 = spineCrv.Domain.T1;
            var tangent = spineCrv.TangentAt(t0); 
            // Taking tangent at start. If straight line, it's fine.

            // Spine is in Y direction usually.
            // Width is in X direction (depth).
            
            // Tangent ~ Y.
            // Cross(Z, Tangent) ~ -X.
            
            Vector3d yDir = tangent;
            Vector3d zAxis = Vector3d.ZAxis;
            Vector3d xDir = Vector3d.CrossProduct(yDir, zAxis); // Points Right relative to Tangent
            
            xDir.Unitize();
            
            // Width is specified.
            // Rect is centered on spine?
            // "Railing Width" is usually thicknes... in X?
            // Wait. Section Railing Width = X dimension.
            // Plan Railing (Rect from Spine) -> Spine is in Y. Width should be X.
            // So we offset by xDir * width/2.
            
            // Wait, existing code:
            // pA = origin - xDir*hx - yDir*hy
            // hx = Length/2 ?? No.
            
            // Let's rely on Offset.
            try {
                var o1 = spineCrv.Offset(Plane.WorldXY, width/2.0, tol, CurveOffsetCornerStyle.Sharp);
                var o2 = spineCrv.Offset(Plane.WorldXY, -width/2.0, tol, CurveOffsetCornerStyle.Sharp);
                
                // Close ends
                if (o1 != null && o2 != null && o1.Length > 0 && o2.Length > 0)
                {
                    // Assuming single curves
                    var c1 = o1[0];
                    var c2 = o2[0];
                    // Reverse c2
                    c2.Reverse();
                    
                    return new Polyline(new[] { c1.PointAtStart, c1.PointAtEnd, c2.PointAtStart, c2.PointAtEnd, c1.PointAtStart }).ToNurbsCurve();
                }
            }
            catch {}
            
            return null; // Fallback
        }
    }
}
