using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Owl.Core.Primitives;

namespace Owl.Grasshopper.Components
{
    public class SightlineComponent : GH_Component
    {
        public SightlineComponent()
          : base("Sightline Analysis", "SightLines",
              "Evaluates sightlines from eyes to a target over obstacles.",
              "Owl", "Analysis")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("EyePoints", "Eyes", "List of observer eye points", GH_ParamAccess.list);
            pManager.AddPointParameter("HeadPoints", "Heads", "List of ALL head points (potential obstacles)", GH_ParamAccess.list);
            pManager.AddPointParameter("Target", "Target", "Focal point", GH_ParamAccess.item);
            pManager.AddNumberParameter("RequiredC", "Req", "Required C-Value clearance", GH_ParamAccess.item, 0.12);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("CValues", "C", "Calculated C-Values for each eye", GH_ParamAccess.list);
            pManager.AddLineParameter("Rays", "Rays", "Sightline rays", GH_ParamAccess.list);
            pManager.AddColourParameter("Colors", "Col", "Analysis colors (Green=Pass, Red=Fail)", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Point3d> eyes = new List<Point3d>();
            List<Point3d> heads = new List<Point3d>();
            Point3d target = Point3d.Unset;
            double reqC = 0.12;

            if (!DA.GetDataList(0, eyes)) return;
            if (!DA.GetDataList(1, heads)) return;
            if (!DA.GetData(2, ref target)) return;
            DA.GetData(3, ref reqC);

            var cValues = new List<double>();
            var rays = new List<Line>();
            var colors = new List<Color>();

            double distTargetSq = target.X * target.X + target.Y * target.Y + target.Z * target.Z; // Not useful, distances are relative

            foreach (var eye in eyes)
            {
                // Optimization: Filter heads that are DEFINITELY behind the eye.
                // Distance(Eye to Target) vs Distance(Head to Target).
                // If Head is closer to Target than Eye is, it MIGHT be an obstacle.
                // If Head is farther, it is behind.
                
                double distEyeTarget = eye.DistanceTo(target);
                var relevantHeads = new List<Point3d>();
                
                foreach(var head in heads)
                {
                    double distHeadTarget = head.DistanceTo(target);
                    if (distHeadTarget < distEyeTarget - 0.1) // 10cm tolerance
                    {
                        relevantHeads.Add(head);
                    }
                }

                var analysis = new OWL_SightlineAnalysis(eye, target, relevantHeads);
                
                cValues.Add(analysis.MinCValue);
                rays.Add(analysis.Ray);
                
                if (analysis.MinCValue >= reqC || analysis.MinCValue > 900)
                {
                    colors.Add(Color.Green);
                }
                else
                {
                    colors.Add(Color.Red);
                }
            }

            DA.SetDataList(0, cValues);
            DA.SetDataList(1, rays);
            DA.SetDataList(2, colors);
        }

        protected override System.Drawing.Bitmap Icon => null;

        public override Guid ComponentGuid => new Guid("1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d");
    }
}
