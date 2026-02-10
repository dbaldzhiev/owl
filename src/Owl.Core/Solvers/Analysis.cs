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
            out List<Line> sightlines,
            out List<List<Line>> limitLines,
            out Brep projectorCone,
            out List<List<Curve>> placedChairs)
        {
            sightlines = new List<Line>();
            limitLines = new List<List<Line>>();
            projectorCone = null;
            placedChairs = new List<List<Curve>>();

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

                    // Place Chairs
                    var rowChairs = new List<Curve>();
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
    }
}
