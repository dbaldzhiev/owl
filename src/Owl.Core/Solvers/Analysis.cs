using System;
using System.Collections.Generic;
using Rhino.Geometry;
using Owl.Core.Primitives;

namespace Owl.Core.Solvers
{
    public class Analysis
    {
        public static void Calculate(
            SerializedTribune serializedTribune,
            List<AudienceSetup> audiences,
            ScreenSetup screen,
            ProjectorSetup projector,
            List<double> audienceOffsets,
            out List<Line> sightlines,
            out Brep projectorCone)
        {
            sightlines = new List<Line>();
            projectorCone = null;

            if (serializedTribune == null) return;

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

            // 2. Generate Sightlines from Eye Points
            if (hasScreen && audiences != null && audiences.Count > 0 && serializedTribune.RowPoints != null)
            {
                for (int i = 0; i < serializedTribune.RowPoints.Count; i++)
                {
                    AudienceSetup currentAudience = audiences[i % audiences.Count];
                    if (currentAudience == null) continue;

                    Vector3d baseEyeOffset = currentAudience.EyeLocation - currentAudience.SecOrigin;

                    Point3d rowPoint = serializedTribune.RowPoints[i];
                    double xOffsetVal = 0;
                    if (audienceOffsets != null && audienceOffsets.Count > 0)
                    {
                        xOffsetVal = audienceOffsets[i % audienceOffsets.Count];
                    }

                    Vector3d xOffsetVec = new Vector3d(xOffsetVal, 0, 0);
                    Vector3d currentEyeOffset = baseEyeOffset + xOffsetVec;

                    if (serializedTribune.Flip)
                    {
                        currentEyeOffset.X = -currentEyeOffset.X;
                    }

                    Point3d eye = rowPoint + currentEyeOffset;
                    sightlines.Add(new Line(eye, screenBottom));
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
    }
}
