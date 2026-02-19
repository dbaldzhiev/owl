using NUnit.Framework;
using Owl.Core.Solvers;
using Owl.Core.Primitives;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace Owl.Core.Tests
{
    [Rhino.Testing.Fixtures.RhinoTestFixture]
    public class TribuneSolverTests
    {
        private TribuneSolver _solver;
        private StairTribuneSetup _stairTrib;
        private RailingSetup _rail;
        private TribuneSetup _trib;
        private StairSetup _stair;

        [SetUp]
        public void Setup()
        {
            // Default Setup
            _trib = new TribuneSetup(3, new List<double> { 80.0, 80.0, 80.0 }, new List<int> { 2, 2, 2 }, new List<bool> { false });
            _stair = new StairSetup(15.0, 30.0);
            _stairTrib = new StairTribuneSetup(_trib, _stair);
            _rail = new RailingSetup(90.0, 5.0);
            _solver = new TribuneSolver(_stairTrib, _rail);
        }

        [Test]
        public void Solve_Basic3Row_ValidStructure()
        {
            var sol = _solver.Solve();
            Assert.That(sol.IsValid, Is.True);
            Assert.That(sol.RowPoints.Count, Is.EqualTo(3));
            Assert.That(sol.SectionTribuneProfile, Is.Not.Null);
            // 3 rows * 2 steps * 15 height = 90 total rise?
            // Row 0 is at 0.
            // Row 1 is at 2*15 = 30.
            // Row 2 is at 2*15 + 30 = 60.
            // Top is at 60.
            // RowPoints[2] Z should be 60.
            Assert.That(sol.RowPoints[0].Z, Is.EqualTo(0));
            Assert.That(sol.RowPoints[1].Z, Is.EqualTo(30));
            Assert.That(sol.RowPoints[2].Z, Is.EqualTo(60));
        }

        [Test]
        public void Solve_VaryingElevation_GeneratesCorrectHeights()
        {
            // Row 0: Base
            // Row 1: Risers=1 -> +15
            // Row 2: Risers=3 -> +45
            _trib = new TribuneSetup(3, new List<double> { 80.0 }, new List<int> { 1, 3 }, new List<bool> { false });
            _stairTrib = new StairTribuneSetup(_trib, _stair);
            _solver = new TribuneSolver(_stairTrib, _rail);

            var sol = _solver.Solve();
            
            Assert.That(sol.RowPoints[0].Z, Is.EqualTo(0));
            Assert.That(sol.RowPoints[1].Z, Is.EqualTo(15)); // 1 step
            Assert.That(sol.RowPoints[2].Z, Is.EqualTo(15 + 45)); // 1 + 3 steps
        }

        [Test]
        public void Solve_VaryingRowDepths_GeneratesCorrectX()
        {
            // Row 0 width: 100
            // Row 1 width: 50
            // Row 2 width: 200
            _trib = new TribuneSetup(3, new List<double> { 100.0, 50.0, 200.0 }, new List<int> { 2 }, new List<bool> { false });
            _stairTrib = new StairTribuneSetup(_trib, _stair);
            _solver = new TribuneSolver(_stairTrib, _rail);

            var sol = _solver.Solve();
            
            // X positions of row STARTS:
            // R0: 0 + railWidth (5) = 5
            // R1: 0 + 100 + railWidth = 105
            // R2: 100 + 50 + railWidth = 155
            
            Assert.That(sol.RowLocalPoints[0].X, Is.EqualTo(5.0).Within(0.001));
            Assert.That(sol.RowLocalPoints[1].X, Is.EqualTo(105.0).Within(0.001));
            Assert.That(sol.RowLocalPoints[2].X, Is.EqualTo(155.0).Within(0.001));
        }

        [Test]
        public void Solve_RailingToggles_CreatesRailingsOnlyWhereTrue()
        {
            // Row 0: default true (start)
            // Row 1: True
            // Row 2: False
            // Row 3: True
            _trib = new TribuneSetup(4, new List<double> { 80.0 }, new List<int> { 2 }, new List<bool> { true, false, true });
            _stairTrib = new StairTribuneSetup(_trib, _stair);
            _solver = new TribuneSolver(_stairTrib, _rail);
       
            // Passed Toggles: [T, F, T] for rows 1, 2, 3... 
            // The solver treats toggles logic: 
            // R0: Always true? Logic: "resolvedToggles.Add(true)" for Row 0.
            // R1: idx=1. list[1%3] = F? No, list index logic is `r % count`.
            // Let's pass explicit list to Solve method.
            
            var toggles = new List<bool> { true, false, true, false }; 
            // R0: T (forced)
            // R1: F
            // R2: T
            // R3: F
            
            var sol = _solver.Solve(railingToggles: toggles);
            
            Assert.That(sol.SectionRailings.Count, Is.EqualTo(2)); // R0 and R2
            // R0 should have railing.
            // R1 should NOT.
            // R2 should have.
            // R3 should NOT.
            
            // Check spine points relative to rows
            // R0 Spine Z: 0 + RailHeight(90) = 90
            // R2 Spine Z: R2_Z(60) + 90 = 150
            
            // We can check SectionRailingsSpine count or logic.
            // Actually SectionRailingsSpine is added ONLY when railing is added.
            Assert.That(sol.SectionRailingsSpine.Count, Is.EqualTo(2));
        }

        [Test]
        public void Solve_Flip_MirrorsX()
        {
            var sol = _solver.Solve(flip: true);
            
            // Normal R1 X: 80 + 5 = 85.
            // Flipped R1 X: -(80 + 5) = -85.
            Assert.That(sol.RowPoints[1].X, Is.EqualTo(-85.0).Within(0.001));
            Assert.That(sol.BasePlane.XAxis.X, Is.LessThan(0)); // (-1, 0, 0)
        }

        [Test]
        public void Solve_Audiences_GeneratesChairsAndLimits()
        {
            // Create dummy chair curve
            var chair = new LineCurve(Point3d.Origin, new Point3d(0, 0, 50));
            var aud = new AudienceSetup(
                new Point3d(0, -50, 120), // Eye
                Plane.WorldXY,            // Origin
                new List<Curve> { chair },
                secFL: 30, secHBL: 10, secSBL: 20
            );

            var sol = _solver.Solve(audiences: new List<AudienceSetup> { aud });

            Assert.That(sol.SectionChairs.Count, Is.EqualTo(3)); // 3 rows
            Assert.That(sol.SectionChairs[0].Count, Is.EqualTo(1));
            Assert.That(sol.SectionLimitLines[0].Count, Is.EqualTo(3)); // FL, HBL, SBL lines
        }

        [Test]
        public void Solve_PlanGeneration_ClipsToBoundary()
        {
            // Setup a boundary that ONLY includes the first 2 rows.
            // Rows are 80 deep. Total 3 rows = 240.
            // Boundary box 0 to 120. Should cut mid-row 2?
            // Or row 2 starts at 160.
            // Row 0: 0-80. Row 1: 80-160. Row 2: 160-240.
            // Boundary 0 to 150.
            // Should include Row 0 completely, and Row 1 partially/completely?
            
            var boundary = new Polyline(new[] {
                new Point3d(0,0,0),
                new Point3d(150,0,0),
                new Point3d(150,100,0),
                new Point3d(0,100,0),
                new Point3d(0,0,0)
            }).ToNurbsCurve();

            var hall = new HallSetup(boundary, new List<Curve>(), new List<Curve>());
            
            var sol = _solver.Solve(hallSetup: hall);

            // PlanLines should exist
            Assert.That(sol.PlanTribuneLines, Is.Not.Empty);
            
            // Verify extent
            // Max X of plan lines should not exceed 150
            foreach(var crv in sol.PlanTribuneLines)
            {
                var box = crv.GetBoundingBox(true);
                Assert.That(box.Max.X, Is.LessThanOrEqualTo(150.001));
            }
        }
        
        [Test]
        public void Solve_StairGaps_Calculated()
        {
             // Row 0: gap 0
             // Row 1: no railing -> aligns to R0 HBL.
             // If railing -> aligns to R0 + RailWidth.
             
             // Case: Even rows have railing, Odd rows don't.
             var toggles = new List<bool> { true, false, true };
             
             var sol = _solver.Solve(railingToggles: toggles);
             
             Assert.That(sol.Gaps.Count, Is.EqualTo(2)); // Gaps between flights (3 rows -> 2 flights)
             Assert.That(sol.Gaps[0], Is.Not.NaN);
        }
    }
}
