using System;
using System.Collections.Generic;
using Rhino.Geometry;
using Owl.Core.Primitives;
using System.Linq;

namespace Owl.Core.Solvers
{
    public class Analysis
    {
        public static void Calculate(
            List<AudienceSetup> audiences,
            SerializedTribune serializedTribune,
            ScreenSetup screen,
            ProjectorSetup projector,
            List<double> audienceOffsets,
            PlanSetup plan,
            out List<Line> sightlines,
            out List<List<Line>> limitLines,
            out Brep projectorCone,
            out List<List<GeometryBase>> placedChairs,
            out List<Curve> planTribuneLines,
            out List<Curve> planRailingLines,
            out List<Curve> planStairLines)
        {
            sightlines = new List<Line>();
            limitLines = new List<List<Line>>();
            projectorCone = null;
            placedChairs = new List<List<GeometryBase>>();
            planTribuneLines = new List<Curve>();
            planRailingLines = new List<Curve>();
            planStairLines = new List<Curve>();

            if (audiences == null || audiences.Count == 0 || serializedTribune == null)
                return;

            // 1. Determine Screen Extents (Top/Bottom)
            Point3d screenBottom = Point3d.Unset;
            Point3d screenTop = Point3d.Unset;
            bool hasScreen = false;

            if (screen != null && screen.ScreenCurve != null)
            {
                Curve screenCrv = screen.ScreenCurve;
                Point3d ptA = screenCrv.PointAtStart;
                Point3d ptB = screenCrv.PointAtEnd;
                screenBottom = (ptA.Z < ptB.Z) ? ptA : ptB;
                screenTop = (ptA.Z > ptB.Z) ? ptA : ptB;
                hasScreen = true;
            }

            // 2. Generate Eye Points & Chairs
            if (serializedTribune.RowPoints != null)
            {
                for (int i = 0; i < serializedTribune.RowPoints.Count; i++)
                {
                    AudienceSetup currentAudience = audiences[i % audiences.Count];
                    if (currentAudience == null) continue;

                    // Vector from Chair Origin to Eye
                    Vector3d baseEyeOffset = currentAudience.EyeLocation - currentAudience.Origin;

                    Point3d rowPoint = serializedTribune.RowPoints[i];
                    double xOffsetVal = 0;
                    if (audienceOffsets != null && audienceOffsets.Count > 0)
                    {
                        xOffsetVal = audienceOffsets[i % audienceOffsets.Count];
                    }

                    Vector3d xOffsetVec = new Vector3d(xOffsetVal, 0, 0);
                    Vector3d currentEyeOffset = baseEyeOffset + xOffsetVec;

                    Transform mirrorXform = Transform.Identity;
                    if (serializedTribune.Flip)
                    {
                        currentEyeOffset.X = -currentEyeOffset.X;
                        // Mirror across YZ plane (Normal = X) at audience.Origin
                        mirrorXform = Transform.Mirror(new Plane(currentAudience.Origin, Vector3d.YAxis, Vector3d.ZAxis));
                        xOffsetVec.X = -xOffsetVec.X;
                    }

                    // Eye Point
                    Point3d eye = rowPoint + currentEyeOffset;

                    // Sightlines
                    if (hasScreen)
                    {
                        sightlines.Add(new Line(eye, screenBottom));
                    }

                    // Limit Lines (Vertical at X=45, 182.5, 200 relative to rowPoint)
                    var rowLimits = new List<Line>();
                    double z0 = rowPoint.Z;
                    double z1 = rowPoint.Z + 50;

                    double xF = rowPoint.X + (serializedTribune.Flip ? -currentAudience.FrontLimit : currentAudience.FrontLimit) + (serializedTribune.Flip ? -xOffsetVal : xOffsetVal);
                    double xHB = rowPoint.X + (serializedTribune.Flip ? -currentAudience.HardBackLimit : currentAudience.HardBackLimit) + (serializedTribune.Flip ? -xOffsetVal : xOffsetVal);
                    double xSB = rowPoint.X + (serializedTribune.Flip ? -currentAudience.SoftBackLimit : currentAudience.SoftBackLimit) + (serializedTribune.Flip ? -xOffsetVal : xOffsetVal);

                    rowLimits.Add(new Line(new Point3d(xF, 0, z0), new Point3d(xF, 0, z1)));
                    rowLimits.Add(new Line(new Point3d(xHB, 0, z0), new Point3d(xHB, 0, z1)));
                    rowLimits.Add(new Line(new Point3d(xSB, 0, z0), new Point3d(xSB, 0, z1)));
                    limitLines.Add(rowLimits);

                    // Place Chairs (Plan Distribution)
                    var rowChairs = new List<GeometryBase>();

                    if (plan != null && plan.TribuneBoundary != null)
                    {
                        // Calculate row line in plan
                        // Assuming Section X corresponds to Plan X relative to plan.Origin
                        double planX = rowPoint.X + plan.Origin.X;
                        
                        // We need to intersect a line at planX with the boundary
                        // However, a simpler approach if we assume aligned plan:
                        // Find the Y range of the boundary at this X
                        
                        var bBox = plan.TribuneBoundary.GetBoundingBox(true);
                        Line rowLine = new Line(new Point3d(planX, bBox.Min.Y - 1000, 0), new Point3d(planX, bBox.Max.Y + 1000, 0));
                        
                        // 1. Tribune Lines: Boundary - Tunnel
                        var tribSegments = GetClippedSegments(rowLine, new List<Curve> { plan.TribuneBoundary }, plan.TunnelBoundary != null ? new List<Curve> { plan.TunnelBoundary } : null);
                        planTribuneLines.AddRange(tribSegments.Select(s => s.ToNurbsCurve()));
                        
                        // 2. Stair Lines: Aisles - Tunnel
                        List<Line> stairSegments = new List<Line>();
                        if (plan.AisleBoundaries != null && plan.AisleBoundaries.Count > 0)
                        {
                            stairSegments = GetClippedSegments(rowLine, plan.AisleBoundaries, plan.TunnelBoundary != null ? new List<Curve> { plan.TunnelBoundary } : null);
                            planStairLines.AddRange(stairSegments.Select(s => s.ToNurbsCurve()));
                        }
                        
                        // 3. Railing/Chair Lines: Boundary - Tunnel - Aisles
                        // Logic: Start with Tribune segments, subtract Aisles
                        var railSegments = Subtract_intervalsFromSegments(tribSegments, plan.AisleBoundaries, rowLine);
                        planRailingLines.AddRange(railSegments.Select(s => s.ToNurbsCurve()));

                        // Distribute Chairs
                        if (currentAudience.PlanGeo != null)
                        {
                            foreach (var line in railSegments)
                            {
                                double length = line.Length;
                                int count = (int)Math.Floor(length / currentAudience.ChairWidth);
                                if (count > 0)
                                {
                                    double spacing = length / count;
                                    for (int c = 0; c < count; c++)
                                    {
                                        double t = (c + 0.5) / count;
                                        Point3d chairPos = line.PointAt(t);
                                        chairPos.Z = rowPoint.Z;

                                        Transform xform;
                                        // If PlanGeo is instance definition logic, origin matters differently?
                                        // User: "in audience setup the input for Pgeo needs to be a block definition. if it is a block definition it is dubious if you even need an origin because you can use the origin of the block"
                                        // We treat PlanGeo as source geometry. If it's a block logic, the source geometry IS at the definition origin.
                                        // So we just move from (0,0,0) (or PlanOriginPt if provided) to target.
                                        
                                        var move = chairPos - currentAudience.PlanOriginPt; // If PlanOriginPt is 0,0,0, this works directly.
                                        xform = Transform.Translation(move);
                                        
                                        var dup = currentAudience.PlanGeo.Duplicate();
                                        dup.Transform(xform);
                                        rowChairs.Add(dup);
                                    }
                                }
                            }
                        }
                    }
                    else if (currentAudience.Chairs != null) // Fallback Section Distribution
                    {
                        Vector3d move = (rowPoint - currentAudience.Origin) + xOffsetVec;
                        var moveXform = Transform.Translation(move);
                        
                        foreach (var chairCrv in currentAudience.Chairs)
                        {
                            if (chairCrv == null) continue;
                            var dup = chairCrv.DuplicateCurve();
                            if (serializedTribune.Flip)
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

            // 3. Generate Projector Cone
            if (hasScreen && projector != null)
            {
                var conePts = new List<Point3d>
                {
                    projector.Location,
                    screenTop,
                    screenBottom,
                    projector.Location // Close loop
                };
                
                var polyline = new Polyline(conePts);
                if (polyline.IsValid)
                {
                    var brep = Brep.CreatePlanarBreps(polyline.ToNurbsCurve(), 0.001);
                    if (brep != null && brep.Length > 0)
                        projectorCone = brep[0];
                }
            }
        }

        private static List<Line> GetClippedSegments(Line baseLine, List<Curve> inclusions, List<Curve> exclusions)
        {
            // 1. Get initial segments inside inclusions (Union of intervals)
            List<Interval> includedIntervals = new List<Interval>();
            if (inclusions != null)
            {
                foreach (var curve in inclusions)
                {
                    var intersects = Rhino.Geometry.Intersect.Intersection.CurveLine(curve, baseLine, 0.001, 0.001);
                    if (intersects != null && intersects.Count >= 2)
                    {
                        // Get params on line (0 to 1)
                        var ts = intersects.Select(i => i.ParameterB).OrderBy(t => t).ToList();
                        for (int k = 0; k < ts.Count - 1; k += 2)
                        {
                            includedIntervals.Add(new Interval(ts[k], ts[k+1]));
                        }
                    }
                }
            }
            else
            {
                return new List<Line>();
            }

            // Merge included intervals
            includedIntervals = MergeIntervals(includedIntervals);
            
            // 2. Subtract exclusions
            if (exclusions != null)
            {
                foreach (var ex in exclusions)
                {
                    var intersects = Rhino.Geometry.Intersect.Intersection.CurveLine(ex, baseLine, 0.001, 0.001);
                    if (intersects != null && intersects.Count >= 2)
                    {
                         var ts = intersects.Select(i => i.ParameterB).OrderBy(t => t).ToList();
                         for (int k = 0; k < ts.Count - 1; k += 2)
                         {
                             var exInterval = new Interval(ts[k], ts[k+1]);
                             List<Interval> nextIncluded = new List<Interval>();
                             foreach (var inc in includedIntervals)
                             {
                                 // Interval subtraction: inc - ex
                                 // Cases:
                                 // 1. No overlap
                                 // 2. ex inside inc -> split
                                 // 3. ex overlaps start/end -> clip
                                 // 4. ex covers inc -> remove
                                 
                                 double t0 = inc.T0;
                                 double t1 = inc.T1;
                                 double e0 = exInterval.T0;
                                 double e1 = exInterval.T1;
                                 
                                 if (e1 <= t0 || e0 >= t1) // No overlap
                                 {
                                     nextIncluded.Add(inc);
                                 }
                                 else
                                 {
                                     if (e0 > t0) nextIncluded.Add(new Interval(t0, e0));
                                     if (e1 < t1) nextIncluded.Add(new Interval(e1, t1));
                                 }
                             }
                             includedIntervals = nextIncluded;
                         }
                    }
                }
            }

            // Convert intervals back to Lines
            var result = new List<Line>();
            foreach (var interval in includedIntervals)
            {
                if (interval.Length > 0.001)
                {
                    result.Add(new Line(baseLine.PointAt(interval.T0), baseLine.PointAt(interval.T1)));
                }
            }
            return result;
        }

        private static List<Interval> MergeIntervals(List<Interval> intervals)
        {
            if (intervals == null || intervals.Count == 0) return new List<Interval>();
            
            var sorted = intervals.OrderBy(i => i.T0).ToList();
            var merged = new List<Interval>();
            var current = sorted[0];
            
            for (int i = 1; i < sorted.Count; i++)
            {
                var next = sorted[i];
                if (next.T0 <= current.T1) // Overlap or adjacent
                {
                    current = new Interval(current.T0, Math.Max(current.T1, next.T1));
                }
                else
                {
                    merged.Add(current);
                    current = next;
                }
            }
            merged.Add(current);
            return merged;
        }

        private static List<Line> Subtract_intervalsFromSegments(List<Line> segments, List<Curve> exclusions, Line baseLine)
        {
             // Similar logic, treat segments as intervals on baseLine and subtract
             // Convert lines to intervals
             List<Interval> intervals = new List<Interval>();
             foreach(var seg in segments)
             {
                 double t0 = baseLine.ClosestParameter(seg.From);
                 double t1 = baseLine.ClosestParameter(seg.To);
                 intervals.Add(new Interval(Math.Min(t0, t1), Math.Max(t0, t1)));
             }
             
             if (exclusions != null)
             {
                foreach (var ex in exclusions)
                {
                    var intersects = Rhino.Geometry.Intersect.Intersection.CurveLine(ex, baseLine, 0.001, 0.001);
                    if (intersects != null && intersects.Count >= 2)
                    {
                         var ts = intersects.Select(i => i.ParameterB).OrderBy(t => t).ToList();
                         for (int k = 0; k < ts.Count - 1; k += 2)
                         {
                             var exInterval = new Interval(ts[k], ts[k+1]);
                             List<Interval> nextIntervals = new List<Interval>();
                             foreach (var inc in intervals)
                             {
                                 double t0 = inc.T0;
                                 double t1 = inc.T1;
                                 double e0 = exInterval.T0;
                                 double e1 = exInterval.T1;
                                 
                                 if (e1 <= t0 || e0 >= t1)
                                 {
                                     nextIntervals.Add(inc);
                                 }
                                 else
                                 {
                                     if (e0 > t0) nextIntervals.Add(new Interval(t0, e0));
                                     if (e1 < t1) nextIntervals.Add(new Interval(e1, t1));
                                 }
                             }
                             intervals = nextIntervals;
                         }
                    }
                }
             }
             
             var result = new List<Line>();
             foreach (var interval in intervals)
             {
                if (interval.Length > 0.001)
                {
                    result.Add(new Line(baseLine.PointAt(interval.T0), baseLine.PointAt(interval.T1)));
                }
             }
             return result;
        }
    }
}
