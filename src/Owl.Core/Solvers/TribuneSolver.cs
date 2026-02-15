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
        private readonly TribuneSetup _tribune;
        private readonly StairSetup _stairs;
        private readonly RailingSetup _railings;

        public TribuneSolver(TribuneSetup tribune, StairSetup stairs, RailingSetup railings)
        {
            _tribune = tribune ?? throw new ArgumentNullException(nameof(tribune));
            _stairs = stairs ?? throw new ArgumentNullException(nameof(stairs));
            _railings = railings ?? throw new ArgumentNullException(nameof(railings));
        }

        public void Solve(
            // Section outputs
            out SerializedTribune serializedTribune,
            out Curve secTribune,
            out Curve secStairs,
            out List<Curve> secRailings,
            out List<Point3d> secRailingsSpine,
            out List<Point3d> secRowSpine,
            out List<List<Curve>> secChairs,
            out List<List<Line>> secLimitLines,
            // Plan outputs
            out List<Curve> planTribuneLines,
            out List<Curve> planStairLines,
            out List<Curve> planRailingLines,
            out List<Curve> planRailingsSpine,
            out List<List<Curve>> planChairs,
            // Chair placement planes (for block insertion)
            out List<Plane> secChairPlanes,
            out List<List<Plane>> planChairPlanes,
            out List<List<Curve>> planRowSpine,
            // Inputs
            bool flip = false,
            Point3d origin = default,
            List<bool> railingToggles = null,
            List<AudienceSetup> audiences = null,
            List<double> audienceOffsets = null,
            HallSetup hallSetup = null)
        {
            const double tol = 0.001;

            serializedTribune = null;
            secTribune = null;
            secStairs = null;
            secRailings = new List<Curve>();
            secRailingsSpine = new List<Point3d>();
            secRowSpine = new List<Point3d>();
            secChairs = new List<List<Curve>>();
            secLimitLines = new List<List<Line>>();

            planTribuneLines = new List<Curve>();
            planStairLines = new List<Curve>();
            planRailingLines = new List<Curve>();
            planRailingsSpine = new List<Curve>();
            planChairs = new List<List<Curve>>();
            secChairPlanes = new List<Plane>();
            planChairPlanes = new List<List<Plane>>();
            planRowSpine = new List<List<Curve>>();

            var gaps = new List<double>();
            var rowPoints = new List<Point3d>();
            var stairStartXList = new List<double>();
            var stairEndXList = new List<double>();

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

            tribPts.Add(new Point3d(currX, 0, currZ));

            Func<int, double> getRowWidth = (i) => {
                if (_tribune.RowWidths.Count == 0) return 0.8;
                return _tribune.RowWidths[i % _tribune.RowWidths.Count];
            };

            // Row 0
            double railW_0 = _railings.RailWidth;
            Point3d r0Start = new Point3d(currX + railW_0, 0, currZ);
            Point3d r0End = new Point3d(currX + getRowWidth(0), 0, currZ);

            rowPoints.Add(r0Start);
            secRowSpine.Add(r0Start);

            currX += getRowWidth(0);
            tribPts.Add(new Point3d(currX, 0, currZ));

            var resolvedToggles = new List<bool>();
            resolvedToggles.Add(true);

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

                currZ += rowRise;
                tribPts.Add(new Point3d(currX, 0, currZ));

                bool showRailing = true;
                if (railingToggles != null && railingToggles.Count > 0)
                {
                    showRailing = railingToggles[r % railingToggles.Count];
                }
                resolvedToggles.Add(showRailing);

                double railW = _railings.RailWidth;
                Point3d rStart = new Point3d(currX + railW, 0, currZ);

                rowPoints.Add(rStart);

                // RowSpine
                if (showRailing)
                {
                    secRowSpine.Add(rStart);
                }
                else if (audiences != null && audiences.Count > 0 && r > 0)
                {
                    var prevAud = audiences[(r - 1) % audiences.Count];
                    double prevHardBackX = rowPoints[r - 1].X + (prevAud != null ? prevAud.SecHBL : 0);
                    secRowSpine.Add(new Point3d(prevHardBackX, 0, currZ));
                }
                else
                {
                    secRowSpine.Add(rStart);
                }

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
                        secRailings.Add(railRec.ToNurbsCurve());

                        Point3d midAxis = new Point3d(currX + railW / 2.0, 0, rTopZ);
                        secRailingsSpine.Add(midAxis);
                    }
                }

                currX += thisRowWidth;
                tribPts.Add(new Point3d(currX, 0, currZ));
            }

            if (tribPts.Count > 1)
                secTribune = new Polyline(tribPts).ToNurbsCurve();

            serializedTribune = new SerializedTribune(rowPoints, gaps, railingToggles: resolvedToggles, secRowSpine: secRowSpine, stairFlightStartX: stairStartXList, stairFlightEndX: stairEndXList);

            // -----------------------------
            // B) STAIRS PROFILE
            // -----------------------------
            var stairPts = new List<Point3d>();

            double currentBaseX = getRowWidth(0);
            double currentBaseZ = 0.0;

            for (int r = 0; r < _tribune.Rows - 1; r++)
            {
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

                // Store stair flight positions
                stairStartXList.Add(startX);
                stairEndXList.Add(cx); // cx is the final X after all steps

                double prevRiserX = currentBaseX - getRowWidth(r);
                if (r == 0) prevRiserX = 0;

                bool showRailing = true;
                if (railingToggles != null && railingToggles.Count > 0)
                {
                    showRailing = railingToggles[r % railingToggles.Count];
                }

                double referenceX;
                if (showRailing || r == 0)
                {
                    // With railing: measure from railing inner face; first row: from origin
                    referenceX = (r == 0) ? 0.0 : prevRiserX + _railings.RailWidth;
                }
                else
                {
                    // No railing: measure from the previous row's hard back limit
                    double prevRowX = rowPoints[r - 1].X; // previous row railing inner face
                    double prevHBL = 0;
                    if (audiences != null && audiences.Count > 0)
                    {
                        var prevAud = audiences[(r - 1) % audiences.Count];
                        if (prevAud != null) prevHBL = prevAud.SecHBL;
                    }
                    referenceX = prevRowX + prevHBL;
                }

                double thisGap = startX - referenceX;
                gaps.Add(thisGap);

                currentBaseX += getRowWidth(r + 1);
                currentBaseZ = targetLandingZ;
            }

            if (stairPts.Count > 1)
                secStairs = new Polyline(stairPts).ToNurbsCurve();

            // -----------------------------
            // C) FLIP LOGIC
            // -----------------------------
            if (flip)
            {
                var mirror = Transform.Mirror(Plane.WorldYZ);

                if (secTribune != null) secTribune.Transform(mirror);
                if (secStairs != null) secStairs.Transform(mirror);

                foreach (var rv in secRailings)
                {
                    if (rv != null) rv.Transform(mirror);
                }

                for (int i = 0; i < secRowSpine.Count; i++)
                {
                    var pt = secRowSpine[i];
                    pt.Transform(mirror);
                    secRowSpine[i] = pt;
                }

                for (int i = 0; i < secRailingsSpine.Count; i++)
                {
                    var pt = secRailingsSpine[i];
                    pt.Transform(mirror);
                    secRailingsSpine[i] = pt;
                }

                var newRowPoints = new List<Point3d>();
                foreach (var pt in serializedTribune.RowPoints)
                {
                    var p = pt;
                    p.Transform(mirror);
                    newRowPoints.Add(p);
                }
                serializedTribune.RowPoints = newRowPoints;
                serializedTribune.Flip = true;
            }

            // -----------------------------
            // D) ORIGIN TRANSLATION
            // -----------------------------
            if (origin != Point3d.Origin)
            {
                var move = Transform.Translation(new Vector3d(origin));

                if (secTribune != null) secTribune.Transform(move);
                if (secStairs != null) secStairs.Transform(move);

                foreach (var rv in secRailings)
                {
                    if (rv != null) rv.Transform(move);
                }

                for (int i = 0; i < secRowSpine.Count; i++)
                {
                    var pt = secRowSpine[i];
                    pt.Transform(move);
                    secRowSpine[i] = pt;
                }

                for (int i = 0; i < secRailingsSpine.Count; i++)
                {
                    var pt = secRailingsSpine[i];
                    pt.Transform(move);
                    secRailingsSpine[i] = pt;
                }

                var movedRowPoints = new List<Point3d>();
                foreach (var pt in serializedTribune.RowPoints)
                {
                    var p = pt;
                    p.Transform(move);
                    movedRowPoints.Add(p);
                }
                serializedTribune.RowPoints = movedRowPoints;
            }

            // -----------------------------
            // E) CHAIR PLACEMENT & LIMIT LINES (after flip/origin)
            // -----------------------------
            if (audiences != null && audiences.Count > 0 && serializedTribune.RowPoints != null)
            {
                bool isFlipped = serializedTribune.Flip;

                for (int i = 0; i < serializedTribune.RowPoints.Count; i++)
                {
                    AudienceSetup currentAudience = audiences[i % audiences.Count];
                    if (currentAudience == null)
                    {
                        secChairs.Add(new List<Curve>());
                        secLimitLines.Add(new List<Line>());
                        continue;
                    }

                    Vector3d baseEyeOffset = currentAudience.EyeLocation - currentAudience.SecOrigin;

                    Point3d rowPoint = serializedTribune.RowPoints[i];
                    double xOffsetVal = 0;
                    if (audienceOffsets != null && audienceOffsets.Count > 0)
                    {
                        xOffsetVal = audienceOffsets[i % audienceOffsets.Count];
                    }

                    Vector3d xOffsetVec = new Vector3d(xOffsetVal, 0, 0);
                    Vector3d currentEyeOffset = baseEyeOffset + xOffsetVec;

                    Transform mirrorXform = Transform.Identity;
                    if (isFlipped)
                    {
                        currentEyeOffset.X = -currentEyeOffset.X;
                        mirrorXform = Transform.Mirror(new Plane(currentAudience.SecOrigin, Vector3d.YAxis, Vector3d.ZAxis));
                        xOffsetVec.X = -xOffsetVec.X;
                    }

                    // Limit Lines
                    var rowLimits = new List<Line>();
                    double z0 = rowPoint.Z;
                    double z1 = rowPoint.Z + 50;

                    double xF = rowPoint.X + (isFlipped ? -currentAudience.SecFL : currentAudience.SecFL) + (isFlipped ? -xOffsetVal : xOffsetVal);
                    double xHB = rowPoint.X + (isFlipped ? -currentAudience.SecHBL : currentAudience.SecHBL) + (isFlipped ? -xOffsetVal : xOffsetVal);
                    double xSB = rowPoint.X + (isFlipped ? -currentAudience.SecSBL : currentAudience.SecSBL) + (isFlipped ? -xOffsetVal : xOffsetVal);

                    rowLimits.Add(new Line(new Point3d(xF, 0, z0), new Point3d(xF, 0, z1)));
                    rowLimits.Add(new Line(new Point3d(xHB, 0, z0), new Point3d(xHB, 0, z1)));
                    rowLimits.Add(new Line(new Point3d(xSB, 0, z0), new Point3d(xSB, 0, z1)));
                    secLimitLines.Add(rowLimits);

                    // Place Section Chairs
                    var rowChairs = new List<Curve>();
                    if (currentAudience.SecChairGeo != null)
                    {
                        Vector3d move = (rowPoint - currentAudience.SecOrigin) + xOffsetVec;
                        var moveXform = Transform.Translation(move);

                        foreach (var chairCrv in currentAudience.SecChairGeo)
                        {
                            if (chairCrv == null) continue;
                            var dup = chairCrv.DuplicateCurve();
                            if (isFlipped)
                            {
                                dup.Transform(mirrorXform);
                            }
                            dup.Transform(moveXform);
                            rowChairs.Add(dup);
                        }
                    }
                    secChairs.Add(rowChairs);

                    // Track placement plane for block insertion
                    var insertPt = new Point3d(rowPoint.X + xOffsetVec.X, rowPoint.Y + xOffsetVec.Y, rowPoint.Z + xOffsetVec.Z);
                    var xDir = isFlipped ? -Vector3d.XAxis : Vector3d.XAxis;
                    secChairPlanes.Add(new Plane(insertPt, xDir, Vector3d.ZAxis));
                }
            }

            // -----------------------------
            // F) PLAN GEOMETRY
            // -----------------------------
            if (hallSetup != null && hallSetup.TribuneBoundary != null && hallSetup.TribuneBoundary.IsValid)
            {
                Curve tribBoundary = hallSetup.TribuneBoundary;
                Plane worldXY = Plane.WorldXY;

                // Build void union (aisle + tunnel)
                var voidInput = new List<Curve>();
                foreach (var c in hallSetup.AisleBoundaries)
                    if (c != null && c.IsValid) voidInput.Add(c);
                foreach (var c in hallSetup.TunnelBoundaries)
                    if (c != null && c.IsValid) voidInput.Add(c);

                var voidUnion = new List<Curve>();
                if (voidInput.Count > 0)
                {
                    var u = Curve.CreateBooleanUnion(voidInput, tol);
                    if (u != null && u.Length > 0)
                        voidUnion.AddRange(u);
                    else
                        voidUnion.AddRange(voidInput); // fallback: use individual curves
                }

                // Build clip regions for stairs: (tribune ∩ aisle) - tunnels
                var clipRegions = new List<Curve>();
                var validAisles = hallSetup.AisleBoundaries.Where(c => c != null && c.IsValid && c.IsClosed).ToList();
                var validTunnels = hallSetup.TunnelBoundaries.Where(c => c != null && c.IsValid && c.IsClosed).ToList();

                if (tribBoundary.IsClosed && validAisles.Count > 0)
                {
                    foreach (var aisle in validAisles)
                    {
                        var inter = Curve.CreateBooleanIntersection(tribBoundary, aisle, tol);
                        if (inter != null)
                            clipRegions.AddRange(inter);
                    }
                }

                // Subtract tunnels from clip regions
                if (clipRegions.Count > 0 && validTunnels.Count > 0)
                {
                    var cleaned = new List<Curve>();
                    foreach (var cr in clipRegions)
                    {
                        var pieces = new List<Curve> { cr };
                        foreach (var tnl in validTunnels)
                        {
                            var nextPieces = new List<Curve>();
                            foreach (var pc in pieces)
                            {
                                var diff = Curve.CreateBooleanDifference(pc, tnl, tol);
                                if (diff != null && diff.Length > 0)
                                    nextPieces.AddRange(diff);
                            }
                            pieces = nextPieces;
                            if (pieces.Count == 0) break;
                        }
                        cleaned.AddRange(pieces);
                    }
                    clipRegions = cleaned;
                }

                // Helper: section line from X through tribune boundary
                Func<double, LineCurve> sectionLineFromX = (x) =>
                {
                    var plane = new Plane(new Point3d(x, 0, 0), Vector3d.XAxis);
                    var hits = Intersection.CurvePlane(tribBoundary, plane, tol);
                    if (hits == null || hits.Count < 2) return null;

                    var hitPts = new List<Point3d>();
                    foreach (var ev in hits)
                        hitPts.Add(ev.PointA);

                    hitPts.Sort((a, b) => a.Y.CompareTo(b.Y));
                    return new LineCurve(hitPts[0], hitPts[hitPts.Count - 1]);
                };

                // Helper: point inside any of the given closed curves
                Func<Point3d, List<Curve>, bool> pointInRegion = (p, regions) =>
                {
                    foreach (var c in regions)
                    {
                        if (c != null && c.IsClosed)
                        {
                            var rel = c.Contains(p, worldXY, tol);
                            if (rel == PointContainment.Inside || rel == PointContainment.Coincident)
                                return true;
                        }
                    }
                    return false;
                };

                // Helper: trim line, keep segments OUTSIDE void
                Func<LineCurve, List<Curve>> trimOutsideVoid = (lineCrv) =>
                {
                    if (lineCrv == null) return new List<Curve>();
                    if (voidUnion.Count == 0) return new List<Curve> { lineCrv };

                    var tVals = new List<double>();
                    foreach (var uc in voidUnion)
                    {
                        if (uc == null || !uc.IsValid) continue;
                        var ccx = Intersection.CurveCurve(lineCrv, uc, tol, tol);
                        if (ccx != null)
                        {
                            foreach (var ev in ccx)
                            {
                                if (ev.IsPoint)
                                    tVals.Add(ev.ParameterA);
                            }
                        }
                    }

                    var dom = lineCrv.Domain;
                    tVals = tVals.Where(t => t > dom.T0 + 1e-9 && t < dom.T1 - 1e-9)
                                 .Select(t => Math.Round(t, 9))
                                 .Distinct().OrderBy(t => t).ToList();

                    Curve[] pieces;
                    if (tVals.Count > 0)
                        pieces = lineCrv.Split(tVals);
                    else
                        pieces = new[] { (Curve)lineCrv };

                    if (pieces == null || pieces.Length == 0) return new List<Curve>();

                    var outside = new List<Curve>();
                    foreach (var seg in pieces)
                    {
                        if (seg == null || !seg.IsValid) continue;
                        var d = seg.Domain;
                        var mid = seg.PointAt((d.T0 + d.T1) * 0.5);
                        if (!pointInRegion(mid, voidUnion))
                            outside.Add(seg);
                    }
                    return outside;
                };

                // Helper: trim line, keep segments INSIDE clip regions
                Func<LineCurve, List<Curve>> trimInsideClip = (lineCrv) =>
                {
                    if (lineCrv == null || clipRegions.Count == 0) return new List<Curve>();

                    var tVals = new List<double>();
                    foreach (var reg in clipRegions)
                    {
                        var ccx = Intersection.CurveCurve(lineCrv, reg, tol, tol);
                        if (ccx != null)
                        {
                            foreach (var ev in ccx)
                            {
                                if (ev.IsPoint)
                                    tVals.Add(ev.ParameterA);
                            }
                        }
                    }

                    var dom = lineCrv.Domain;
                    tVals = tVals.Where(t => t > dom.T0 + 1e-9 && t < dom.T1 - 1e-9)
                                 .Select(t => Math.Round(t, 9))
                                 .Distinct().OrderBy(t => t).ToList();

                    Curve[] pieces;
                    if (tVals.Count > 0)
                        pieces = lineCrv.Split(tVals);
                    else
                        pieces = new[] { (Curve)lineCrv };

                    if (pieces == null || pieces.Length == 0) return new List<Curve>();

                    var inside = new List<Curve>();
                    foreach (var seg in pieces)
                    {
                        if (seg == null || !seg.IsValid) continue;
                        var d = seg.Domain;
                        var mid = seg.PointAt((d.T0 + d.T1) * 0.5);
                        if (pointInRegion(mid, clipRegions))
                            inside.Add(seg);
                    }
                    return inside;
                };

                // --- F1) Plan Tribune Lines ---
                // Tribune profile vertices: cull first, take every other (False,True pattern)
                if (secTribune != null)
                {
                    var polyline = new Polyline();
                    if (secTribune.TryGetPolyline(out polyline) && polyline.Count > 1)
                    {
                        // Skip first vertex, then take every other (index 1,3,5,... of remaining = index 2,4,6,... of original)
                        var culled = new List<Point3d>();
                        for (int i = 1; i < polyline.Count; i++)
                        {
                            // After culling first: indices 0,1,2,3,... of remaining = 1,2,3,4,... of original
                            // Pattern False,True means take odd indices of remaining = even indices of original (2,4,6,...)
                            if ((i - 1) % 2 == 1) // False,True pattern: skip even, take odd
                                culled.Add(polyline[i]);
                        }

                        foreach (var pt in culled)
                        {
                            var line = sectionLineFromX(pt.X);
                            planTribuneLines.AddRange(trimOutsideVoid(line));
                        }
                    }
                }

                // --- F2) Plan Stair Lines ---
                // Stair profile vertices: take every other with False,True pattern
                if (secStairs != null)
                {
                    var polyline = new Polyline();
                    if (secStairs.TryGetPolyline(out polyline) && polyline.Count > 1)
                    {
                        var culled = new List<Point3d>();
                        for (int i = 0; i < polyline.Count; i++)
                        {
                            // Pattern False,True: take odd indices (1,3,5,...)
                            if (i % 2 == 1)
                                culled.Add(polyline[i]);
                        }

                        foreach (var pt in culled)
                        {
                            var line = sectionLineFromX(pt.X);
                            planStairLines.AddRange(trimInsideClip(line));
                        }
                    }
                }

                // --- F3) Plan Railing Lines (rectangles from spine) ---
                double railWidth = _railings.RailWidth;
                foreach (var railSpinePt in secRailingsSpine)
                {
                    var line = sectionLineFromX(railSpinePt.X);
                    var segments = trimOutsideVoid(line);

                    // Keep spine curves
                    foreach (var seg in segments)
                    {
                        if (seg != null && seg.IsValid)
                            planRailingsSpine.Add(seg);
                    }

                    // Build rectangle from each spine segment
                    foreach (var seg in segments)
                    {
                        var rect = RectFromSpine(seg, railWidth, tol);
                        if (rect != null)
                            planRailingLines.Add(rect);
                    }
                }

                // --- F4) Plan Chair Placement ---
                if (audiences != null && audiences.Count > 0 && serializedTribune.RowPoints != null)
                {
                    bool isFlipped = serializedTribune.Flip;

                    for (int i = 0; i < serializedTribune.RowPoints.Count; i++)
                    {
                        var audience = audiences[i % audiences.Count];
                        if (audience == null || audience.PlanChairGeo == null || audience.PlanChairGeo.Count == 0)
                        {
                            planChairs.Add(new List<Curve>());
                            planChairPlanes.Add(new List<Plane>());
                            planRowSpine.Add(new List<Curve>());
                            continue;
                        }

                        // Use secRowSpine X as the reference (matching section behavior)
                        double spineX = (i < secRowSpine.Count) ? secRowSpine[i].X : serializedTribune.RowPoints[i].X;

                        // Apply audience offset (same direction logic as section)
                        double xOffsetVal = 0;
                        if (audienceOffsets != null && audienceOffsets.Count > 0)
                            xOffsetVal = audienceOffsets[i % audienceOffsets.Count];

                        double planX = spineX + (isFlipped ? -xOffsetVal : xOffsetVal);

                        // Get the plan line at this X
                        var planLine = sectionLineFromX(planX);
                        if (planLine == null)
                        {
                            planChairs.Add(new List<Curve>());
                            planChairPlanes.Add(new List<Plane>());
                            planRowSpine.Add(new List<Curve>());
                            continue;
                        }

                        // Trim to outside-void segments
                        var segments = trimOutsideVoid(planLine);

                        // Store the row spine curves
                        planRowSpine.Add(new List<Curve>(segments.Where(s => s != null && s.IsValid)));

                        // Compute actual physical width from bounding box of PlanChairGeo
                        double axialWidth = audience.PlanChairWidth > 0 ? audience.PlanChairWidth : 500;
                        double actualWidth = axialWidth; // fallback

                        var allChairBBox = BoundingBox.Empty;
                        foreach (var cg in audience.PlanChairGeo)
                        {
                            if (cg != null && cg.IsValid)
                                allChairBBox.Union(cg.GetBoundingBox(true));
                        }
                        if (allChairBBox.IsValid)
                        {
                            // Chair width along the row is the Y dimension of the bounding box
                            actualWidth = allChairBBox.Max.Y - allChairBBox.Min.Y;
                        }

                        // Overhang: how much the physical chair extends beyond the axial slot on each side
                        double overhang = Math.Max(0, (actualWidth - axialWidth) / 2.0);

                        // Distribute chairs centered on each segment
                        var rowPlanChairs = new List<Curve>();
                        var rowPlanPlanes = new List<Plane>();

                        foreach (var seg in segments)
                        {
                            if (seg == null || !seg.IsValid) continue;
                            double segLen = seg.GetLength();

                            // N chairs fit if: (N-1)*axialWidth + actualWidth <= segLen
                            if (segLen < actualWidth) continue;
                            int N = (int)Math.Floor((segLen - actualWidth) / axialWidth) + 1;
                            if (N < 1) continue;

                            // Total physical span of N chairs
                            double totalSpan = (N - 1) * axialWidth + actualWidth;
                            // Equal margin on each side
                            double margin = (segLen - totalSpan) / 2.0;

                            for (int c = 0; c < N; c++)
                            {
                                // Center of the c-th chair along segment
                                double distAlongSeg = margin + actualWidth / 2.0 + c * axialWidth;

                                double t;
                                if (!seg.LengthParameter(distAlongSeg, out t)) continue;

                                Point3d chairPos = seg.PointAt(t);

                                // Track placement plane
                                rowPlanPlanes.Add(new Plane(chairPos, Vector3d.XAxis, Vector3d.YAxis));

                                // Place plan chair geo at this position
                                Vector3d moveVec = chairPos - audience.PlanChairOrigin;
                                var moveXform = Transform.Translation(moveVec);

                                foreach (var chairGeo in audience.PlanChairGeo)
                                {
                                    if (chairGeo == null) continue;
                                    var dup = chairGeo.DuplicateCurve();
                                    dup.Transform(moveXform);
                                    rowPlanChairs.Add(dup);
                                }
                            }
                        }
                        planChairs.Add(rowPlanChairs);
                        planChairPlanes.Add(rowPlanPlanes);
                    }
                }
            }
        }
        /// <summary>
        /// Build a closed rectangle polyline centered on a spine curve.
        /// Width is perpendicular to the spine, in the XY plane.
        /// </summary>
        private static Curve RectFromSpine(Curve spineCrv, double width, double tol)
        {
            if (spineCrv == null || !spineCrv.IsValid) return null;

            double L = spineCrv.GetLength();
            if (L <= tol || width <= tol) return null;

            var dom = spineCrv.Domain;
            double tmid = (dom.T0 + dom.T1) * 0.5;

            // Get tangent direction projected to XY
            var tangent = spineCrv.TangentAt(tmid);
            var xDir = new Vector3d(tangent.X, tangent.Y, 0);

            if (xDir.IsTiny(tol))
            {
                // Fallback: use endpoints
                var p0 = spineCrv.PointAtStart;
                var p1 = spineCrv.PointAtEnd;
                xDir = new Vector3d(p1.X - p0.X, p1.Y - p0.Y, 0);
            }
            if (xDir.IsTiny(tol)) return null;

            xDir.Unitize();
            var yDir = Vector3d.CrossProduct(Vector3d.ZAxis, xDir);
            if (yDir.IsTiny(tol)) return null;
            yDir.Unitize();

            var origin = spineCrv.PointAt(tmid);
            double hx = 0.5 * L;
            double hy = 0.5 * width;

            var pA = origin - xDir * hx - yDir * hy;
            var pB = origin + xDir * hx - yDir * hy;
            var pC = origin + xDir * hx + yDir * hy;
            var pD = origin - xDir * hx + yDir * hy;

            var pl = new Polyline(new[] { pA, pB, pC, pD, pA });
            return pl.ToNurbsCurve();
        }
    }
}
