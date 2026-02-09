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
            AudienceSetup audience,
            SerializedTribune serializedTribune,
            ScreenSetup screen,
            ProjectorSetup projector,
            out List<Line> sightlines,
            out Brep projectorCone,
            out List<Curve> placedChairs)
        {
            sightlines = new List<Line>();
            projectorCone = null;
            placedChairs = new List<Curve>();

            if (audience == null || serializedTribune == null || screen == null || projector == null)
                return;

            // 1. Determine Screen Extents (Top/Bottom)
            Curve screenCrv = screen.ScreenCurve;
            if (screenCrv == null) return;

            Point3d ptA = screenCrv.PointAtStart;
            Point3d ptB = screenCrv.PointAtEnd;
            
            Point3d screenBottom = (ptA.Z < ptB.Z) ? ptA : ptB;
            Point3d screenTop = (ptA.Z > ptB.Z) ? ptA : ptB;

            // 2. Generate Eye Points & Chairs
            if (serializedTribune.RowPoints != null)
            {
                // Vector from Chair Origin to Eye
                Vector3d eyeOffset = audience.EyeLocation - audience.Origin;

                Transform mirrorXform = Transform.Identity;
                if (serializedTribune.Flip)
                {
                    eyeOffset.X = -eyeOffset.X;
                    mirrorXform = Transform.Mirror(new Plane(audience.Origin, Vector3d.XAxis, Vector3d.ZAxis));
                }

                foreach (var rowPoint in serializedTribune.RowPoints)
                {
                    // Eye Point
                    Point3d eye = rowPoint + eyeOffset;
                    sightlines.Add(new Line(eye, screenBottom));

                    // Place Chairs
                    if (audience.Chairs != null)
                    {
                        Vector3d move = rowPoint - audience.Origin;
                        var moveXform = Transform.Translation(move);
                        
                        foreach (var chairCrv in audience.Chairs)
                        {
                            if (chairCrv == null) continue;
                            var dup = chairCrv.DuplicateCurve();
                            if (serializedTribune.Flip)
                            {
                                dup.Transform(mirrorXform);
                            }
                            dup.Transform(moveXform);
                            placedChairs.Add(dup);
                        }
                    }
                }
            }

            // 3. Generate Projector Cone
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
