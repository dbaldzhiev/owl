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

        public void Solve(
            out Curve tribuneProfile,
            out Curve stairsProfile,
            out List<Curve> railingProfiles,
            out List<Point3d> railingMidpoints,
            out SerializedTribune serializedTribune,
            out List<Line> tribRows,
            out List<Point3d> rowSpine,
            out List<List<Curve>> placedChairs,
            out List<List<Line>> limitLines,
            bool flip = false,
            Point3d origin = default,
            List<bool> railingToggles = null,
            List<AudienceSetup> audiences = null,
            List<double> audienceOffsets = null)
        {
            tribuneProfile = null;
            stairsProfile = null;
            railingProfiles = new List<Curve>();
            railingMidpoints = new List<Point3d>();
            var gaps = new List<double>();
            tribRows = new List<Line>();
            rowSpine = new List<Point3d>();
            placedChairs = new List<List<Curve>>();
            limitLines = new List<List<Line>>();

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
                if (_tribune.RowWidths.Count == 0) return 0.8;
                return _tribune.RowWidths[i % _tribune.RowWidths.Count];
            };

            // Capture Row 0 Data
            double railW_0 = _railings.RailWidth;
            Point3d r0Start = new Point3d(currX + railW_0, 0, currZ);
            Point3d r0End = new Point3d(currX + row0Width, 0, currZ);

            rowPoints.Add(r0Start);
            rowSpine.Add(r0Start);
            tribRows.Add(new Line(new Point3d(currX, 0, currZ), r0End));

            currX += getRowWidth(0);
            tribPts.Add(new Point3d(currX, 0, currZ));

            // Track resolved railing toggles per row (for Validator)
            var resolvedToggles = new List<bool>();
            resolvedToggles.Add(true); // Row 0 always has front railing

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

                // Determine if railing is ON for this row
                bool showRailing = true;
                if (railingToggles != null && railingToggles.Count > 0)
                {
                    showRailing = railingToggles[r % railingToggles.Count];
                }
                resolvedToggles.Add(showRailing);

                // Capture Row Data
                double railW = _railings.RailWidth;
                Point3d rStart = new Point3d(currX + railW, 0, currZ);
                Point3d rEnd = new Point3d(currX + thisRowWidth, 0, currZ);

                rowPoints.Add(rStart);
                tribRows.Add(new Line(new Point3d(currX, 0, currZ), rEnd));

                // RowSpine: row-railing intersection when railing is ON,
                // previous row's hard back limit intersection when railing is OFF
                if (showRailing)
                {
                    rowSpine.Add(rStart);
                }
                else if (audiences != null && audiences.Count > 0 && r > 0)
                {
                    var prevAud = audiences[(r - 1) % audiences.Count];
                    double prevHardBackX = rowPoints[r - 1].X + (prevAud != null ? prevAud.SecHBL : 0);
                    rowSpine.Add(new Point3d(prevHardBackX, 0, currZ));
                }
                else
                {
                    rowSpine.Add(rStart); // fallback
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
                        railingProfiles.Add(railRec.ToNurbsCurve());

                        Point3d midAxis = new Point3d(currX + railW / 2.0, 0, rTopZ);
                        railingMidpoints.Add(midAxis);
                    }
                }

                // 2. Tread FORWARD
                currX += thisRowWidth;
                tribPts.Add(new Point3d(currX, 0, currZ));
            }

            if (tribPts.Count > 1)
                tribuneProfile = new Polyline(tribPts).ToNurbsCurve();

            serializedTribune = new SerializedTribune(rowPoints, gaps, railingToggles: resolvedToggles);

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

                double prevRiserX = currentBaseX - getRowWidth(r);
                if (r == 0) prevRiserX = 0;

                bool showRailing = true;
                if (railingToggles != null && railingToggles.Count > 0)
                {
                    showRailing = railingToggles[r % railingToggles.Count];
                }

                double backOffset = _railings.RailWidth;

                if (!showRailing)
                {
                    if (audiences != null && audiences.Count > 0)
                    {
                        var aud = audiences[r % audiences.Count];
                        if (aud != null)
                        {
                            backOffset = aud.SecHBL;
                        }
                    }
                    else
                    {
                        backOffset = 0.0;
                    }
                }

                double railInnerFace = prevRiserX + backOffset;
                if (r == 0) railInnerFace = 0;

                double thisGap = startX - railInnerFace;
                gaps.Add(thisGap);

                currentBaseX += getRowWidth(r + 1);
                currentBaseZ = targetLandingZ;
            }

            if (stairPts.Count > 1)
                stairsProfile = new Polyline(stairPts).ToNurbsCurve();


            // -----------------------------
            // D) FLIP LOGIC
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

                // Flip RowSpine Points
                for (int i = 0; i < rowSpine.Count; i++)
                {
                    var pt = rowSpine[i];
                    pt.Transform(mirror);
                    rowSpine[i] = pt;
                }

                // Flip Railing Midpoints
                for (int i = 0; i < railingMidpoints.Count; i++)
                {
                    var pt = railingMidpoints[i];
                    pt.Transform(mirror);
                    railingMidpoints[i] = pt;
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
            }

            // -----------------------------
            // E) ORIGIN TRANSLATION
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

                for (int i = 0; i < rowSpine.Count; i++)
                {
                    var pt = rowSpine[i];
                    pt.Transform(move);
                    rowSpine[i] = pt;
                }

                // Move Railing Midpoints
                for (int i = 0; i < railingMidpoints.Count; i++)
                {
                    var pt = railingMidpoints[i];
                    pt.Transform(move);
                    railingMidpoints[i] = pt;
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
            }

            // -----------------------------
            // F) CHAIR PLACEMENT & LIMIT LINES (after flip/origin so RowPoints are final)
            // -----------------------------
            if (audiences != null && audiences.Count > 0 && serializedTribune.RowPoints != null)
            {
                bool isFlipped = serializedTribune.Flip;

                for (int i = 0; i < serializedTribune.RowPoints.Count; i++)
                {
                    AudienceSetup currentAudience = audiences[i % audiences.Count];
                    if (currentAudience == null)
                    {
                        placedChairs.Add(new List<Curve>());
                        limitLines.Add(new List<Line>());
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
                    limitLines.Add(rowLimits);

                    // Place Chairs
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
                    placedChairs.Add(rowChairs);
                }
            }
        }
    }
}
