using System;
using Grasshopper.Kernel;

namespace Owl.Grasshopper.Components.Placeholders
{
    public class Owl_Audience : GH_Component
    {
        public Owl_Audience() : base("Audience", "Audience", "Audience Placeholder", "Owl", "Placeholders") { }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) { }
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) { }
        protected override void SolveInstance(IGH_DataAccess DA) { }
        protected override System.Drawing.Bitmap Icon => null;
        public override Guid ComponentGuid => new Guid("55555555-5555-5555-5555-555555555555");
    }

    public class Owl_Screen : GH_Component
    {
        public Owl_Screen() : base("Screen", "Screen", "Screen Placeholder", "Owl", "Placeholders") { }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) { }
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) { }
        protected override void SolveInstance(IGH_DataAccess DA) { }
        protected override System.Drawing.Bitmap Icon => null;
        public override Guid ComponentGuid => new Guid("66666666-6666-6666-6666-666666666666");
    }

    public class Owl_Projector : GH_Component
    {
        public Owl_Projector() : base("Projector", "Projector", "Projector Placeholder", "Owl", "Placeholders") { }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) { }
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) { }
        protected override void SolveInstance(IGH_DataAccess DA) { }
        protected override System.Drawing.Bitmap Icon => null;
        public override Guid ComponentGuid => new Guid("77777777-7777-7777-7777-777777777777");
    }

    public class Owl_Analysis : GH_Component
    {
        public Owl_Analysis() : base("Analysis", "Analysis", "Analysis Placeholder", "Owl", "Placeholders") { }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) { }
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) { }
        protected override void SolveInstance(IGH_DataAccess DA) { }
        protected override System.Drawing.Bitmap Icon => null;
        public override Guid ComponentGuid => new Guid("88888888-8888-8888-8888-888888888888");
    }
}
