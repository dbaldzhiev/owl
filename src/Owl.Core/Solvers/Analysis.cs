using System;
using System.Collections.Generic;
using Rhino.Geometry;
using Owl.Core.Primitives;
using System.Linq;

namespace Owl.Core.Solvers
{
    public class Analysis
    {
        public static SerializedAnalysis Calculate(
            List<AudienceSetup> audiences,
            SerializedTribune serializedTribune,
            RailingSetup railing,
            ScreenSetup screen,
            ProjectorSetup projector,
            List<double> audienceOffsets,
            PlanSetup plan)
        {
            var result = new SerializedAnalysis(serializedTribune, audiences, new List<Line>(), audienceOffsets, plan);
            
            result.Sightlines = new List<Line>();
            // limitLines will be flat list in SerializedAnalysis for simplicity or we keep List<List> structure? 
            // SerializedAnalysis has List<Line> LimitLines. Let's flatten or change SerializedAnalysis.
            // Wait, looking at SerializedAnalysis update, I added List<Line> LimitLines. 
            // But usually limits are per row. 
            // In Analysis.cs it was List<List<Line>>. 
            // I should probably keep it List<Line> and just add all segments, or change property to List<List<Line>>?
            // User visualization usually wants data tree. Flattened is easier for serialization.
            // Let's stick with flattened List<Line> for now, or List<List> if I can change SerializedAnalysis again.
            // I defined `public List<Line> LimitLines { get; set; }` in previous step.
            // So I will flatten it.
            
            // result.LimitLines is List<Line>
            // result.SectionChairs is List<List<GeometryBase>>
            // result.Plan... are List<Curve>
            // result.PlanChairs is List<List<GeometryBase>>

            if (audiences == null || audiences.Count == 0 || serializedTribune == null)
                return result;

            // ... (Screen Extents Logic remains same, lines 37-50) ...
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

            // --- PLAN GEOMETRY GENERATION ---
            if (plan != null && plan.TribuneBoundary != null)
            {
                var bBox = plan.TribuneBoundary.GetBoundingBox(true);
                double yMin = bBox.Min.Y - 1000;
                double yMax = bBox.Max.Y + 1000;

                // 1. Tribune Lines (from Risers/Treads)
                // We use Risers X coordinates to draw the step edges
                var tribuneSources = serializedTribune.Risers; 
                if (tribuneSources == null || tribuneSources.Count == 0) tribuneSources = serializedTribune.Treads; // Fallback

                if (tribuneSources != null)
                {
                    foreach (var crv in tribuneSources)
                    {
                        if (crv == null) continue;
                        double x = crv.PointAtStart.X + plan.Origin.X;
                        Line rowLine = new Line(new Point3d(x, yMin, 0), new Point3d(x, yMax, 0));
                        var segs = GetClippedSegments(rowLine, new List<Curve> { plan.TribuneBoundary }, plan.TunnelBoundary != null ? new List<Curve> { plan.TunnelBoundary } : null);
                        result.PlanTribune.AddRange(segs.Select(s => s.ToNurbsCurve()));
                    }
                }

                // 2. Stair Lines (from StairPoints)
                if (serializedTribune.StairPoints != null)
                {
                    // Group/Unique X coordinates to avoid duplicates if multiple points align
                    var stairXs = serializedTribune.StairPoints.Select(p => p.X).Distinct().ToList();
                    foreach (var val in stairXs)
                    {
                        double x = val + plan.Origin.X;
                        Line rowLine = new Line(new Point3d(x, yMin, 0), new Point3d(x, yMax, 0));
                        if (plan.AisleBoundaries != null && plan.AisleBoundaries.Count > 0)
                        {
                            var segs = GetClippedSegments(rowLine, plan.AisleBoundaries, plan.TunnelBoundary != null ? new List<Curve> { plan.TunnelBoundary } : null);
                            result.PlanStairs.AddRange(segs.Select(s => s.ToNurbsCurve()));
                        }
                    }
                }
            }

            // 2. Generate Eye Points & Chairs & Railings
            if (serializedTribune.RowPoints != null)
            {
                for (int i = 0; i < serializedTribune.RowPoints.Count; i++)
                {
                    AudienceSetup currentAudience = audiences[i % audiences.Count];
                    if (currentAudience == null) continue;

                    // ... (Eye Point Logic lines 60-103) ...
                    // Vector from Chair Origin to Eye
                    Vector3d baseEyeOffset = currentAudience.EyeLocation - currentAudience.Origin;
                    Point3d rowPoint = serializedTribune.RowPoints[i];
                    double xOffsetVal = (audienceOffsets != null && audienceOffsets.Count > 0) ? audienceOffsets[i % audienceOffsets.Count] : 0;
                    Vector3d xOffsetVec = new Vector3d(xOffsetVal, 0, 0);
                    Vector3d currentEyeOffset = baseEyeOffset + xOffsetVec;

                    Transform mirrorXform = Transform.Identity;
                    if (serializedTribune.Flip)
                    {
                        currentEyeOffset.X = -currentEyeOffset.X;
                        mirrorXform = Transform.Mirror(new Plane(currentAudience.Origin, Vector3d.YAxis, Vector3d.ZAxis));
                        xOffsetVec.X = -xOffsetVec.X;
                    }
                    Point3d eye = rowPoint + currentEyeOffset;

                    if (hasScreen) result.Sightlines.Add(new Line(eye, screenBottom));

                    // Limit Lines
                    double z0 = rowPoint.Z;
                    double z1 = rowPoint.Z + 50;
                    double xF = rowPoint.X + (serializedTribune.Flip ? -currentAudience.FrontLimit : currentAudience.FrontLimit) + (serializedTribune.Flip ? -xOffsetVal : xOffsetVal);
                    double xHB = rowPoint.X + (serializedTribune.Flip ? -currentAudience.HardBackLimit : currentAudience.HardBackLimit) + (serializedTribune.Flip ? -xOffsetVal : xOffsetVal);
                    double xSB = rowPoint.X + (serializedTribune.Flip ? -currentAudience.SoftBackLimit : currentAudience.SoftBackLimit) + (serializedTribune.Flip ? -xOffsetVal : xOffsetVal);
                    
                    result.LimitLines.Add(new Line(new Point3d(xF, 0, z0), new Point3d(xF, 0, z1)));
                    result.LimitLines.Add(new Line(new Point3d(xHB, 0, z0), new Point3d(xHB, 0, z1)));
                    result.LimitLines.Add(new Line(new Point3d(xSB, 0, z0), new Point3d(xSB, 0, z1)));

                    // Place Chairs & Railings (Plan Distribution)
                    var rowChairs = new List<GeometryBase>();

                    if (plan != null && plan.TribuneBoundary != null)
                    {
                         var bBox = plan.TribuneBoundary.GetBoundingBox(true);
                         double planX = rowPoint.X + plan.Origin.X;
                         Line rowLine = new Line(new Point3d(planX, bBox.Min.Y - 1000, 0), new Point3d(planX, bBox.Max.Y + 1000, 0));
                         
                         // Get base segments for Tribune/Railings
                         var tribSegments = GetClippedSegments(rowLine, new List<Curve> { plan.TribuneBoundary }, plan.TunnelBoundary != null ? new List<Curve> { plan.TunnelBoundary } : null);
                         
                         // Railing Rectangles
                         // Subtract Aisles
                         var railSegments = Subtract_intervalsFromSegments(tribSegments, plan.AisleBoundaries, rowLine);
                         
                         // Generate Railing Rectangles
                         if (railing != null)
                         {
                             foreach (var seg in railSegments)
                             {
                                 double rW = railing.RailWidth;
                                 Point3d p0 = seg.From; 
                                 Point3d p1 = seg.To;
                                 Point3d p2 = new Point3d(p1.X + rW, p1.Y, 0);
                                 Point3d p3 = new Point3d(p0.X + rW, p0.Y, 0);
                                 
                                 var poly = new Polyline(new[] { p0, p3, p2, p1, p0 });
                                 result.PlanRailings.Add(poly.ToNurbsCurve());
                             }
                         }

                        // Distribute Chairs (existing logic)
                        if (currentAudience.PlanGeo != null)
                        {
                            foreach (var line in railSegments)
                            {
                                int count = (int)Math.Floor(line.Length / currentAudience.ChairWidth);
                                if (count > 0)
                                {
                                    for (int c = 0; c < count; c++)
                                    {
                                        double t = (c + 0.5) / count;
                                        Point3d chairPos = line.PointAt(t);
                                        chairPos.Z = 0; // Flattened

                                        Transform xform;
                                        var move = chairPos - currentAudience.PlanOriginPt; 
                                        move.Z = 0; // Ensure pure XY translation
                                        xform = Transform.Translation(move);
                                        
                                        var dup = currentAudience.PlanGeo.Duplicate();
                                        dup.Transform(xform);
                                        rowChairs.Add(dup);
                                    }
                                }
                            }
                            result.PlanChairs.Add(rowChairs);
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
                            if (serializedTribune.Flip) dup.Transform(mirrorXform);
                            dup.Transform(moveXform);
                            rowChairs.Add(dup);
                        }
                        result.SectionChairs.Add(rowChairs);
                    }
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
                        result.ProjectorCone = brep[0];
                }
            }
            
            return result;
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
