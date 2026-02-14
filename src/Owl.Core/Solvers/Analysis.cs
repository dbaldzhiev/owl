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

            bool canProcessPlan = plan != null && plan.TribuneBoundary != null;
            Vector3d planDepthAxis = serializedTribune.Flip ? -Vector3d.XAxis : Vector3d.XAxis;
            double planRowMinY = 0.0;
            double planRowMaxY = 0.0;

            if (canProcessPlan)
            {
                // Plan drawing is expected in World XY.
                var tribBox = plan.TribuneBoundary.GetBoundingBox(true);
                if (Math.Abs(tribBox.Min.Z) > 0.001 || Math.Abs(tribBox.Max.Z) > 0.001)
                {
                    canProcessPlan = false;
                }
                else
                {
                    planRowMinY = tribBox.Min.Y - 10.0;
                    planRowMaxY = tribBox.Max.Y + 10.0;

                    // Validate that section depth axis points into boundary.
                    var axisProbe = new Line(plan.Origin, plan.Origin + (planDepthAxis * 100000.0));
                    var axisHits = Rhino.Geometry.Intersect.Intersection.CurveLine(plan.TribuneBoundary, axisProbe, 0.001, 0.001);
                    if (axisHits == null || axisHits.Count == 0)
                    {
                        canProcessPlan = false;
                    }
                }
            }

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
                    
                    if (canProcessPlan)
                    {
                         double dX = Math.Abs(rowPoint.X - serializedTribune.Origin.X);
                         
                         // Station Point
                         Point3d stationPoint = plan.Origin + (planDepthAxis * dX);
                         stationPoint.Z = 0;

                         Line rowLine = new Line(new Point3d(stationPoint.X, planRowMinY, 0), new Point3d(stationPoint.X, planRowMaxY, 0));
                         
                         List<Curve> tribInclusions = new List<Curve> { plan.TribuneBoundary };
                         List<Curve> tunnelEx = plan.TunnelBoundary != null ? new List<Curve> { plan.TunnelBoundary } : null;
                         List<Curve> aisleEx = plan.AisleBoundaries;
                         
                         // 1. Tribune Segments (Valid Row parts) - (Tribune - Tunnel)
                         var tribSegments = GetClippedSegments(rowLine, tribInclusions, tunnelEx ?? new List<Curve>());
                         planTribuneLines.AddRange(tribSegments.Select(s => s.ToNurbsCurve()));
                         
                         // 2. Stairs (Inside Aisles AND Inside Tribune)
                         if (aisleEx != null && aisleEx.Count > 0)
                         {
                             // Intersect Tribune segments with Aisles
                             foreach (var tSeg in tribSegments)
                             {
                                 var sSegs = GetClippedSegments(tSeg, aisleEx, new List<Curve>());
                                 planStairLines.AddRange(sSegs.Select(s => s.ToNurbsCurve()));
                             }
                         }
                         
                         // 3. Chairs and Railings (Tribune - Tunnel - Aisles)
                         List<Curve> allEx = new List<Curve>();
                         if (tunnelEx != null) allEx.AddRange(tunnelEx);
                         if (aisleEx != null) allEx.AddRange(aisleEx);
                         
                         // Re-clip original rowLine against (Tribune) minus (Tunnel + Aisles)
                         var seatSegments = GetClippedSegments(rowLine, tribInclusions, allEx);
                         planRailingLines.AddRange(seatSegments.Select(s => s.ToNurbsCurve()));
                         
                         // Chair Placement
                         if (currentAudience.ChairWidth > 0.001)
                         {
                             foreach (var segment in seatSegments)
                             {
                                 double len = segment.Length;
                                 int count = (int)(len / currentAudience.ChairWidth);
                                 
                                 if (count > 0)
                                 {
                                     double totalWidth = count * currentAudience.ChairWidth;
                                     double gap = (len - totalWidth) / 2.0; // margins
                                     
                                     Vector3d segDir = segment.Direction;
                                     segDir.Unitize();
                                     
                                     Point3d startP = segment.From + segDir * (gap + currentAudience.ChairWidth/2.0);
                                     
                                     for (int k = 0; k < count; k++)
                                     {
                                         Point3d pt = startP + segDir * (k * currentAudience.ChairWidth);
                                         Transform xform = GetChairPlacementTransform(pt, segDir);
                                         
                                         if (currentAudience.PlanChairBlockId != Guid.Empty)
                                         {
                                             var instance = new InstanceReferenceGeometry(currentAudience.PlanChairBlockId, xform);
                                             rowPlanChairs.Add(instance);
                                         }
                                         else if (currentAudience.PlanGeometry != null)
                                         {
                                              foreach(var geom in currentAudience.PlanGeometry)
                                              {
                                                  var dup = geom.Duplicate();
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
            if (inclusions == null || inclusions.Count == 0)
            {
                return new List<Line>();
            }

            var includedIntervals = new List<Interval>();
            foreach (var inc in inclusions)
            {
                includedIntervals.AddRange(GetLineIntervalsInsideCurve(baseLine, inc));
            }
            includedIntervals = MergeIntervals(includedIntervals);

            if (exclusions != null)
            {
                foreach (var ex in exclusions)
                {
                    var exIntervals = GetLineIntervalsInsideCurve(baseLine, ex);
                    foreach (var exInterval in exIntervals)
                    {
                        List<Interval> nextIncluded = new List<Interval>();
                        foreach (var incInterval in includedIntervals)
                        {
                            nextIncluded.AddRange(SubtractInterval(incInterval, exInterval));
                        }
                        includedIntervals = nextIncluded;
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

        private static IEnumerable<Interval> GetLineIntervalsInsideCurve(Line baseLine, Curve boundary)
        {
            const double tol = 0.001;
            if (boundary == null || !boundary.IsClosed) yield break;

            var parameters = new List<double> { 0.0, 1.0 };
            var intersections = Rhino.Geometry.Intersect.Intersection.CurveLine(boundary, baseLine, tol, tol);
            if (intersections != null)
            {
                foreach (var x in intersections)
                {
                    if (x.ParameterB > tol && x.ParameterB < 1.0 - tol)
                    {
                        parameters.Add(x.ParameterB);
                    }
                }
            }

            parameters = parameters.Distinct().OrderBy(t => t).ToList();
            for (int i = 0; i < parameters.Count - 1; i++)
            {
                double t0 = parameters[i];
                double t1 = parameters[i + 1];
                if (t1 - t0 <= tol) continue;

                double tm = (t0 + t1) * 0.5;
                var mid = baseLine.PointAt(tm);
                var inside = boundary.Contains(mid, Plane.WorldXY, tol);
                if (inside == PointContainment.Inside || inside == PointContainment.Coincident)
                {
                    yield return new Interval(t0, t1);
                }
            }
        }

        private static IEnumerable<Interval> SubtractInterval(Interval source, Interval cut)
        {
            const double tol = 0.001;

            double s0 = source.T0;
            double s1 = source.T1;
            double c0 = cut.T0;
            double c1 = cut.T1;

            if (c1 <= s0 + tol || c0 >= s1 - tol)
            {
                yield return source;
                yield break;
            }

            if (c0 > s0 + tol)
                yield return new Interval(s0, c0);

            if (c1 < s1 - tol)
                yield return new Interval(c1, s1);
        }

        private static Transform GetChairPlacementTransform(Point3d insertPoint, Vector3d rowDirection)
        {
            if (!rowDirection.Unitize())
            {
                return Transform.Translation(insertPoint - Point3d.Origin);
            }

            // Chairs are authored in local XY where local +Y follows row direction.
            Transform orient = Transform.Rotation(Vector3d.YAxis, rowDirection, Point3d.Origin);
            Transform move = Transform.Translation(insertPoint - Point3d.Origin);
            return move * orient;
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
