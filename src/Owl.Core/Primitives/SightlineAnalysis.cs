using System;
using System.Collections.Generic;
using Rhino.Geometry;

namespace Owl.Core.Primitives
{
    public class OWL_SightlineAnalysis
    {
        public Point3d Eye { get; private set; }
        public Point3d Target { get; private set; }
        public List<Point3d> Obstacles { get; private set; }
        
        public double MinCValue { get; private set; }
        public Line Ray { get; private set; }
        public Point3d CriticalObstacle { get; private set; }

        public OWL_SightlineAnalysis(Point3d eye, Point3d target, List<Point3d> obstacles)
        {
            Eye = eye;
            Target = target;
            Obstacles = obstacles ?? new List<Point3d>();
            Calculate();
        }

        private void Calculate()
        {
            Ray = new Line(Eye, Target);
            MinCValue = double.MaxValue;
            
            // Distance from Eye to Target on XY plane (for projection ratio)
            double distEyeTargetXY = Eye.DistanceTo(new Point3d(Target.X, Target.Y, Eye.Z));

            if (distEyeTargetXY < 0.001) 
            {
                MinCValue = 0; // Eye is on target? Invalid.
                return;
            }

            foreach (var obs in Obstacles)
            {
                // Distance from Eye to Obstacle on XY plane
                double distEyeObsXY = Eye.DistanceTo(new Point3d(obs.X, obs.Y, Eye.Z));
                
                // Factor t along the line (0 at Eye, 1 at Target)
                double t = distEyeObsXY / distEyeTargetXY;
                
                // We only care about obstacles strictly between eye and screen.
                // t > 0 (in front of eye) and t < 1 (before screen)
                if (t <= 0.05 || t >= 0.99) continue; // Ignore self or very close heads, and ignore obstacle at screen
                       
                // Z of Ray at this distance
                double rayZ = Eye.Z + (Target.Z - Eye.Z) * t;
                
                // C-Value = Vertical Clearance = RayZ - ObstacleZ
                // Positive C-Value means Ray is ABOVE obstacle (Good)
                double cVal = rayZ - obs.Z;
                
                if (cVal < MinCValue)
                {
                    MinCValue = cVal;
                    CriticalObstacle = obs;
                }
            }
            
            if (MinCValue == double.MaxValue) MinCValue = 1000.0; // Infinite clearance
        }
    }
}
