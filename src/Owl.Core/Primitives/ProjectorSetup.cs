using Rhino.Geometry;

namespace Owl.Core.Primitives
{
    public class ProjectorSetup
    {
        public Point3d Location { get; set; }

        public ProjectorSetup()
        {
        }

        public ProjectorSetup(Point3d location)
        {
            Location = location;
        }

        public ProjectorSetup Duplicate()
        {
            return new ProjectorSetup(Location);
        }
    }
}
