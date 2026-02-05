using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace Owl.Grasshopper
{
    public class OwlInfo : GH_AssemblyInfo
    {
        public override string Name => "Owl";

        // TODO: Add icon
        public override Bitmap Icon => null;

        public override string Description => "Auditorium and Cinema design tools tailored for Rhino 8.";

        public override Guid Id => new Guid("29853909-7776-4735-867c-179361093121");

        public override string AuthorName => "Owl Team";

        public override string AuthorContact => "";
    }
}
