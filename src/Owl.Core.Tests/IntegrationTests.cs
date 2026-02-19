using NUnit.Framework;
using Rhino.Geometry;
using Owl.Core.Primitives;
using Owl.Core.Solvers;
using Owl.Core.Visualization;
using System.Collections.Generic;

namespace Owl.Core.Tests
{
    [TestFixture]
    public class IntegrationTests
    {
        [Test]
        public void FullWorkflow_StandardTribune_ShouldValidateAndVisualize()
        {
            // 1. SETUP
            // ---------------------------------------------------------
            var stairTribune = new StairTribuneSetup(
                new TribuneSetup(10, new List<double> { 0.9 }, new List<int> { 2 }, new List<bool> { true }),
                new StairSetup(0.15, 0.30)
            );
            
            var railings = new RailingSetup(1.1, 0.05);
            
            // Hall with some constraints
            var hall = new HallSetup(
                new Rectangle3d(Plane.WorldXY, 20, 20).ToNurbsCurve(), // Boundary
                new List<Curve>(), // Aisles
                new List<Curve>(), // Tunnels
                Plane.WorldXY,
                Plane.WorldXY, // PlanFrame
                new Point3d(10, 0, 5), // Projector
                new LineCurve(new Point3d(5, 10, 2), new Point3d(15, 10, 2)) // Screen
            );

            // Audiences
            var audiences = new List<AudienceSetup>
            {
                new AudienceSetup(
                    new Point3d(0,0,1.2), 
                    Plane.WorldXY, 
                    null, 
                    1.0, 
                    0.5, 
                    0.2, 
                    null, 
                    Plane.WorldXY, 
                    0.5
                )
            };

            // 2. SOLVE
            // ---------------------------------------------------------
            var solver = new TribuneSolver(stairTribune, railings);
            var solution = solver.Solve(false, null, audiences, null, hall);

            Assert.That(solution.IsValid, Is.True, "Solution should be valid.");
            Assert.That(solution.RowPoints.Count, Is.EqualTo(10), "Should have 10 rows.");

            // 3. VALIDATE
            // ---------------------------------------------------------
            TribuneValidator.Validate(
                solution,
                out List<double> landings,
                out List<double> clearances,
                out List<double> cValues,
                out List<string> errors,
                out List<Point3d> clashes
            );

            // Expect some valid outputs
            Assert.That(landings.Count, Is.EqualTo(10), "Should check landings for all rows.");
            Assert.That(clearances.Count, Is.EqualTo(10), "Should check clearances for all rows.");
            
            // We might have errors if the dummy geometry is bad, but the list should exist
            Assert.That(errors, Is.Not.Null);

            // 4. VISUALIZE
            // ---------------------------------------------------------
            // Section
            TribuneVisualizer.Visualize(
                solution,
                1, // Section
                out List<Curve> secTrib,
                out List<Curve> secStairs,
                out List<Curve> secRailings,
                out List<Curve> secChairs,
                out List<Plane> secFrames,
                out Brep secCone,
                out List<Curve> secDims,
                out Curve secSafetyArc
            );

            Assert.That(secTrib.Count, Is.GreaterThan(0), "Should generate section tribune curves.");
            Assert.That(secCone, Is.Not.Null, "Should generate projector cone in section.");
            Assert.That(secDims.Count, Is.GreaterThan(0), "Should generate dimensions.");
            
            // Plan
            TribuneVisualizer.Visualize(
                solution,
                0, // Plan
                out List<Curve> planTrib,
                out List<Curve> planStairs,
                out List<Curve> planRailings,
                out List<Curve> planChairs,
                out List<Plane> planFrames,
                out Brep planCone,
                out List<Curve> planDims,
                out Curve planSafetyArc
            );

            Assert.That(planTrib.Count, Is.GreaterThan(0), "Should generate plan tribune curves.");
        }
    }
}
