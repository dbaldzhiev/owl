using System;
using System.Collections.Generic;
using Rhino.Geometry;
using Owl.Core.Primitives;

namespace Owl.Core.Visualization
{
    public class TribuneVisualizer
    {
        public static void Visualize(
            TribuneSolution solution,
            int mode,
            out List<Curve> outTrib,
            out List<Curve> outStairs,
            out List<Curve> outRailings,
            out List<Curve> outChairs,
            out List<Plane> outFrames,
            out Brep outProjectorCone,
            out List<Curve> outDims,
            out Curve outSafetyArc)
        {
            outTrib = new List<Curve>();
            outStairs = new List<Curve>();
            outRailings = new List<Curve>();
            outChairs = new List<Curve>();
            outFrames = new List<Plane>();
            outProjectorCone = null;
            outDims = new List<Curve>();
            outSafetyArc = null;

            if (solution == null) return;
            
            // Determine Transforms
            Transform xform = Transform.Identity;
            
            // If HallSetup is present, map Canonical to Target.
            if (solution.HallSetup != null)
            {
                if (mode == 1) // Section
                {
                    // Canonical Section: Generated in World XZ (X=Run, Z=Rise).
                    // We want to map:
                    // Run (World X) -> Target Frame X
                    // Rise (World Z) -> Target Frame Y
                    // So we treat source as a Plane where X=WorldX, Y=WorldZ.
                    Plane sourceSection = new Plane(Point3d.Origin, Vector3d.XAxis, Vector3d.ZAxis);
                    
                    xform = Transform.PlaneToPlane(sourceSection, solution.HallSetup.SectionFrame);
                }
                else // Plan
                {
                    // Canonical Plan: WorldXY -> Target: PlanFrame
                    xform = Transform.PlaneToPlane(Plane.WorldXY, solution.HallSetup.PlanFrame);
                }
            }

            // Mode 1: Section. Mode 0: Plan.
            if (mode == 1) 
            {
                if (solution.SectionTribuneProfile != null) 
                {
                     var c = solution.SectionTribuneProfile.DuplicateCurve();
                     c.Transform(xform);
                     outTrib.Add(c);
                }
                if (solution.SectionStairsProfile != null)
                {
                     var c = solution.SectionStairsProfile.DuplicateCurve();
                     c.Transform(xform);
                     outStairs.Add(c);
                }
                if (solution.SectionRailings != null)
                {
                     foreach(var c in solution.SectionRailings)
                     {
                         var dc = c.DuplicateCurve();
                         dc.Transform(xform);
                         outRailings.Add(dc);
                     }
                }
                
                foreach (var rowChairs in solution.SectionChairs)
                {
                    if (rowChairs != null)
                    {
                        var transformedRow = new List<Curve>();
                        foreach(var c in rowChairs)
                        {
                            var dc = c.DuplicateCurve();
                            dc.Transform(xform);
                            transformedRow.Add(dc);
                        }
                        outChairs.AddRange(transformedRow);
                    }
                }
                
                if (solution.SectionChairPlanes != null)
                {
                    foreach(var p in solution.SectionChairPlanes)
                    {
                        var dp = p;
                        dp.Transform(xform);
                        outFrames.Add(dp);
                    }
                }

                // Projector Cone & Arc
                // Note: solution.SectionProjector/Screen were mapped to ???
                // In Solver, we tried to map them to Canonical...
                // But Solver said "Remains in Frame for now?".
                // Actually, I updated Solver to map Projector to Canonical!
                // So we can apply xform.
                
                if (solution.SectionProjector != Point3d.Unset && solution.SectionScreen != null && solution.SectionScreen.IsValid)
                {
                    Point3d p = solution.SectionProjector; // Canonical
                    p.Transform(xform); // To Target
                    
                    Curve s = solution.SectionScreen.DuplicateCurve(); // Canonical
                    s.Transform(xform); // To Target
                    
                    Point3d p1 = s.PointAtStart;
                    Point3d p2 = s.PointAtEnd;
                    
                    // Cone
                    var lines = new List<Curve> { new LineCurve(p, p1), s.DuplicateCurve(), new LineCurve(p2, p) };
                    var joined = Curve.JoinCurves(lines);
                    if (joined != null && joined.Length > 0 && joined[0].IsClosed)
                    {
                        var breps = Brep.CreatePlanarBreps(joined[0], 0.001);
                        if (breps != null && breps.Length > 0) outProjectorCone = breps[0];
                    }

                    // Safety Arc (r=200, +10 extension)
                    Vector3d v1 = p1 - p;
                    Vector3d v2 = p2 - p;
                    
                    Plane arcPlane = new Plane(p, v1, v2); 

                    double angle = Vector3d.VectorAngle(v1, v2, arcPlane);
                    double extAngle = 10.0 / 200.0; // radians
                    double totalSweep = angle + 2 * extAngle;
                    
                    // Arc(Plane, Radius, Angle) starts at XAxis (v1)
                    var safeArc = new Arc(arcPlane, 200.0, totalSweep);
                    safeArc.Transform(Transform.Rotation(-extAngle, arcPlane.ZAxis, arcPlane.Origin));
                    
                    outSafetyArc = new ArcCurve(safeArc);
                }

                // DIMS
                if (solution.SectionTribuneProfile != null && solution.SectionTribuneProfile.TryGetPolyline(out Polyline poly))
                {
                     // Poly is Canonical. Transform points then create lines? Or transform lines?
                     // Better transform geometry.
                     // poly is struct, need to duplicate points?
                     var transPoly = poly.Duplicate();
                     transPoly.Transform(xform);
                     
                     for(int i=0; i<transPoly.Count-1; i++)
                     {
                         outDims.Add(new LineCurve(transPoly[i], transPoly[i+1]));
                     }
                }
            }
            else // Plan
            {
                if (solution.PlanTribuneLines != null) 
                    foreach(var c in solution.PlanTribuneLines) { var dc=c.DuplicateCurve(); dc.Transform(xform); outTrib.Add(dc); }
                    
                if (solution.PlanStairLines != null) 
                    foreach(var c in solution.PlanStairLines) { var dc=c.DuplicateCurve(); dc.Transform(xform); outStairs.Add(dc); }
                    
                if (solution.PlanRailings != null) 
                    foreach(var c in solution.PlanRailings) { var dc=c.DuplicateCurve(); dc.Transform(xform); outRailings.Add(dc); }
                
                foreach (var rowChairs in solution.PlanChairs)
                    if (rowChairs != null) 
                        foreach(var c in rowChairs) { var dc=c.DuplicateCurve(); dc.Transform(xform); outChairs.Add(dc); }
                
                foreach (var rowFrames in solution.PlanChairPlanes)
                    if (rowFrames != null) 
                        foreach(var p in rowFrames) { var dp=p; dp.Transform(xform); outFrames.Add(dp); }
            }
            // Removed Transform logic: Inputs are already in correct frame. 
            // ^ Old comment. New logic APPLIES transform.
        }

        // Removed TransformCurves helper as it's no longer used.
    }
}
