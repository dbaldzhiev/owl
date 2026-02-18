using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Owl.Core.Primitives;
using Owl.Core.Visualization;

namespace Owl.Grasshopper.Components.Visualization
{
    public class Owl_Draftsman : GH_Component
    {
        public Owl_Draftsman()
          : base("Tribune Draftsman", "Draftsman",
              "Visualize Tribune Solution in Plan and Section using a frame.",
              "Owl", "Visualization")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("TribuneSolution", "Sol", "Tribune Solution container", GH_ParamAccess.item); // 0
            pManager.AddIntegerParameter("ViewMode", "Mode", "0=Plan, 1=Section", GH_ParamAccess.item, 0); // 1
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Tribune", "Trib", "Tribune profile geometry (Plan or Section)", GH_ParamAccess.list);
            pManager.AddCurveParameter("Stairs", "Stairs", "Stairs profile geometry (Plan or Section)", GH_ParamAccess.list);
            pManager.AddCurveParameter("Railings", "Rails", "Railing geometry (Plan or Section)", GH_ParamAccess.list);
            pManager.AddCurveParameter("Chairs", "Chairs", "Chair geometry (Plan or Section)", GH_ParamAccess.list);
            pManager.AddPlaneParameter("ChairFrames", "Frames", "Chair placement frames (Plan or Section)", GH_ParamAccess.list);
            pManager.AddBrepParameter("ProjectorCone", "Cone", "Projector cone visualization (Section)", GH_ParamAccess.item);
            pManager.AddCurveParameter("Dims", "Dims", "Dimension lines for clearance", GH_ParamAccess.list);
            pManager.AddCurveParameter("SafetyArc", "SafeArc", "Projector safety zone arc", GH_ParamAccess.item);
            pManager.AddTextParameter("Errors", "Err", "Validation Errors", GH_ParamAccess.list);
            pManager.AddCurveParameter("Sightlines", "Sight", "Sightlines from chair eye to screen bottom", GH_ParamAccess.list);
            pManager.AddLineParameter("Limits", "Lims", "Chair Limits (FL, HBL, SBL)", GH_ParamAccess.list);
            pManager.AddCurveParameter("ChairClrDims", "ChairClr", "Dimension lines for Chair Clearance", GH_ParamAccess.list);
            pManager.AddCurveParameter("StairClrDims", "StairClr", "Dimension lines for Stair Clearance", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            TribuneSolution solution = null;
            int mode = 0;

            if (!DA.GetData(0, ref solution) || solution == null) return;
            DA.GetData(1, ref mode);

            // Forward Errors
            DA.SetDataList(8, solution.Errors);
            if (solution.Errors.Count > 0)
            {
                 AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Solution has errors.");
            }

            TribuneVisualizer.Visualize(
                solution, 
                mode, 
                out List<Curve> outTrib, 
                out List<Curve> outStairs, 
                out List<Curve> outRailings, 
                out List<Curve> outChairs, 
                out List<Plane> outFrames, 
                out Brep outCone,
                out List<Curve> outDims,
                out Curve outSafetyArc,
                out List<Curve> outSightlines,
                out List<Line> outLimits,
                out List<Curve> outChairClDims,
                out List<Curve> outStairClDims
            );

            // Set Data
            DA.SetDataList(0, outTrib);
            DA.SetDataList(1, outStairs);
            DA.SetDataList(2, outRailings);
            DA.SetDataList(3, outChairs);
            DA.SetDataList(4, outFrames);
            if (outCone != null) DA.SetData(5, outCone);
            DA.SetDataList(6, outDims);
            if (outSafetyArc != null) DA.SetData(7, outSafetyArc);
            DA.SetDataList(9, outSightlines);
            DA.SetDataList(10, outLimits);
            DA.SetDataList(11, outChairClDims);
            DA.SetDataList(12, outStairClDims);
        }

        protected override System.Drawing.Bitmap Icon => null;
        public override Guid ComponentGuid => new Guid("A1B2C3D4-E5F6-7890-1234-56789ABCDEF0");

    }
}
