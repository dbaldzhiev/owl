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
            out List<List<GeometryBase>> sectionChairs,
            out List<List<GeometryBase>> planChairs,
            out List<Curve> planTribuneLines,
            out List<Curve> planRailingLines,
            out List<Curve> planStairLines)
        {
            sightlines = new List<Line>();
            limitLines = new List<List<Line>>();
            projectorCone = null;
            sectionChairs = new List<List<GeometryBase>>();
            planChairs = new List<List<GeometryBase>>();
            planTribuneLines = new List<Curve>();
            planRailingLines = new List<Curve>();
            planStairLines = new List<Curve>();

            if (audiences == null || audiences.Count == 0 || serializedTribune == null)
                return;

            // 1. Determine Screen Extents
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

            // 2. Main Loop: Rows
            if (serializedTribune.RowPoints != null)
            {
                for (int i = 0; i < serializedTribune.RowPoints.Count; i++)
                {
                    AudienceSetup currentAudience = audiences[i % audiences.Count];
                    if (currentAudience == null) continue;

                    Point3d rowPoint = serializedTribune.RowPoints[i];
                    
                    // --- SECTION LOGIC ---
                    
                    Vector3d baseEyeOffset = currentAudience.EyeLocation - currentAudience.Origin;

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
                        mirrorXform = Transform.Mirror(new Plane(currentAudience.Origin, Vector3d.YAxis, Vector3d.ZAxis));
                        xOffsetVec.X = -xOffsetVec.X;
                    }

                    Point3d eye = rowPoint + currentEyeOffset;

                    if (hasScreen)
                    {
                        sightlines.Add(new Line(eye, screenBottom));
                    }

                    // Limit Lines
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
                    
                    // Place Section Chairs (ALWAYS)
                    var rowSectionChairs = new List<GeometryBase>();
                    if (currentAudience.Chairs != null)
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
                            rowSectionChairs.Add(dup);
                        }
                    }
                    sectionChairs.Add(rowSectionChairs);

                    // --- PLAN LOGIC ---
                    
                    var rowPlanChairs = new List<GeometryBase>();
                    if (plan != null && plan.TribuneBoundary != null)
                    {
                        // Mapping Section Space to Plan Space
                        // Section X -> Plan X
                        // PlanX = PlanOrigin.X + (SectionPoint.X - SectionOrigin.X)
                        
                        double relX = rowPoint.X - serializedTribune.Origin.X;
                        double planX = plan.Origin.X + relX;
                        var bBox = plan.TribuneBoundary.GetBoundingBox(true);
                        double yMin = bBox.Min.Y - 10000;
                        double yMax = bBox.Max.Y + 10000;
                        
                        Line rowLine = new Line(new Point3d(planX, yMin, 0), new Point3d(planX, yMax, 0));
                        
                        List<Curve> tunnelEx = plan.TunnelBoundary != null ? new List<Curve> { plan.TunnelBoundary } : null;
                        List<Curve> aisleEx = plan.AisleBoundaries;
                        List<Curve> allEx = new List<Curve>();
                        if (tunnelEx != null) allEx.AddRange(tunnelEx);
                        if (aisleEx != null) allEx.AddRange(aisleEx);
                        
                        var tribSegments = GetClippedSegments(rowLine, new List<Curve> { plan.TribuneBoundary }, tunnelEx);
                        planTribuneLines.AddRange(tribSegments.Select(s => s.ToNurbsCurve()));
                        
                        if (aisleEx != null && aisleEx.Count > 0)
                        {
                            var stairSegments = GetClippedSegments(rowLine, aisleEx, tunnelEx);
                            planStairLines.AddRange(stairSegments.Select(s => s.ToNurbsCurve()));
                        }
                        
                        var seatSegments = GetClippedSegments(rowLine, new List<Curve> { plan.TribuneBoundary }, allEx);
                        planRailingLines.AddRange(seatSegments.Select(s => s.ToNurbsCurve()));

                        // Distribute Chairs
                        if (currentAudience.ChairWidth > 0 && 
                           (currentAudience.PlanChairBlockId != Guid.Empty || (currentAudience.PlanGeometry != null && currentAudience.PlanGeometry.Count > 0)))
                        {
                            foreach (var segment in seatSegments)
                            {
                                double len = segment.Length;
                                int n = (int)Math.Floor(len / currentAudience.ChairWidth);
                                
                                if (n > 0)
                                {
                                    double totalChairWidth = n * currentAudience.ChairWidth;
                                    double margin = (len - totalChairWidth) / 2.0;
                                    
                                    Vector3d dir = segment.Direction;
                                    dir.Unitize();
                                    
                                    Point3d startPt = segment.From + dir * (margin + currentAudience.ChairWidth / 2.0);
                                    
                                    for (int c = 0; c < n; c++)
                                    {
                                        Point3d pt = startPt + dir * (c * currentAudience.ChairWidth);
                                        // Align center
                                        
                                        Transform xform = Transform.Translation(pt - currentAudience.PlanOriginPt); // Shift origin to pt
                                        
                                        if (currentAudience.PlanChairBlockId != Guid.Empty)
                                        {
                                            var instance = new InstanceReferenceGeometry(currentAudience.PlanChairBlockId, xform);
                                            rowPlanChairs.Add(instance);
                                        }
                                        else if (currentAudience.PlanGeometry != null)
                                        {
                                            // Copy Geometry
                                            foreach(var pg in currentAudience.PlanGeometry)
                                            {
                                                var dup = pg.Duplicate();
                                                dup.Transform(xform);
                                                rowPlanChairs.Add(dup);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    planChairs.Add(rowPlanChairs);
                }
            }
            
            // ... Projector Cone ...
            if (hasScreen && projector != null)
            {
                var conePts = new List<Point3d>
                {
                    projector.Location,
                    screenTop,
                    screenBottom,
                    projector.Location 
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
                                 
                                 double t0 = inc.T0;
                                 double t1 = inc.T1;
                                 double e0 = exInterval.T0;
                                 double e1 = exInterval.T1;
                                 
                                 if (e1 <= t0 + 0.001 || e0 >= t1 - 0.001) // No overlap or touch
                                 {
                                     nextIncluded.Add(inc);
                                 }
                                 else
                                 {
                                     if (e0 > t0 + 0.001) nextIncluded.Add(new Interval(t0, e0));
                                     if (e1 < t1 - 0.001) nextIncluded.Add(new Interval(e1, t1));
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
                if (next.T0 <= current.T1 + 0.001) // Overlap or adjacent
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
    }
}
