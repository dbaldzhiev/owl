using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Owl.Core.Primitives;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace Owl.Grasshopper.Components.Setup
{
    public class Owl_ReclinerChair : GH_Component
    {
        private static List<Curve> _internalChairs = null;

        public Owl_ReclinerChair()
          : base("Recliner Chair", "Recliner",
              "Audience Setup with a specific Recliner Chair geometry.",
              "Owl", "Setup")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Origin", "Origin", "Origin of the chair-eye config", GH_ParamAccess.item, Point3d.Origin);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("AudienceSetup", "Audience", "Audience Setup Object", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Point3d origin = Point3d.Origin;

            DA.GetData(0, ref origin);

            Point3d eye = new Point3d(155, 97, 0);

            if (_internalChairs == null)
            {
                _internalChairs = GetInternalGeometry();
            }

            var chairs = _internalChairs.Select(c => c.DuplicateCurve()).ToList();

            // Move chairs to origin
            var moveXform = Transform.Translation(origin - Point3d.Origin);
            foreach (var c in chairs) c.Transform(moveXform);

            var setup = new AudienceSetup(eye, origin, chairs);
            DA.SetData(0, setup);
        }

        private List<Curve> GetInternalGeometry()
        {
            var assembly = Assembly.GetExecutingAssembly();
            // Note: Namespace might be Owl.Grasshopper.Components.Setup or just Owl.Grasshopper depending on where the resource is logically placed. 
            // Usually it is AssemblyName.Folder.Filename
            // The csproj has <EmbeddedResource Include="recliner.json" />
            // So it should be Owl.Grasshopper.recliner.json
            var resourceName = "Owl.Grasshopper.recliner.json";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null) return new List<Curve>();

                List<Curve> curves = new List<Curve>();
                using (StreamReader reader = new StreamReader(stream))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        try
                        {
                            using (JsonDocument doc = JsonDocument.Parse(line))
                            {
                                if (doc.RootElement.TryGetProperty("data", out JsonElement dataElement))
                                {
                                    string b64 = dataElement.GetString();
                                    if (!string.IsNullOrEmpty(b64))
                                    {
                                        byte[] bytes = Convert.FromBase64String(b64);
                                        using (var archive = Rhino.FileIO.File3dm.FromByteArray(bytes))
                                        {
                                            var objs = archive.Objects
                                                .Select(obj => obj.Geometry as Curve)
                                                .Where(c => c != null);
                                            curves.AddRange(objs);
                                        }
                                    }
                                }
                            }
                        }
                        catch
                        {
                            // Continue to next line if parsing fails
                            continue;
                        }
                    }
                }
                return curves;
            }
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // Reusing icon for now or null
                return null;
            }
        }

        public override Guid ComponentGuid => new Guid("7D90AF43-3456-6789-0123-45678901CDEF");
    }
}

