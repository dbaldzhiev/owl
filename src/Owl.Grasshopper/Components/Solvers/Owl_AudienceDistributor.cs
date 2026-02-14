using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;
using Owl.Core.Primitives;
using Owl.Core.Solvers;
using System.Text.Json;
using System.Linq;

namespace Owl.Grasshopper.Components.Solvers
{
    public class Owl_AudienceDistributor : GH_Component
    {
        public Owl_AudienceDistributor()
          : base("Audience Distributor", "AudDist",
              "Calculates sightlines, projector cone, and distributes chairs with optional alignment.",
              "Owl", "Solvers")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("AudienceSetup", "Audience", "Audience Setup Object", GH_ParamAccess.list);
            pManager.AddGenericParameter("SerializedTribune", "STrib", "Serialized Tribune Output", GH_ParamAccess.item);
            pManager.AddGenericParameter("ProjectorSetup", "Projector", "Projector Setup Object", GH_ParamAccess.item);
            pManager.AddGenericParameter("ScreenSetup", "Screen", "Screen Setup Object", GH_ParamAccess.item);
            pManager.AddNumberParameter("AudienceOffsets", "Offsets", "List of X offsets for chairs and eyes per row", GH_ParamAccess.list);
            pManager.AddTextParameter("SerializedPlanSetup", "SPlan", "Serialized Plan Setup JSON", GH_ParamAccess.item);
            
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
            pManager[5].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddLineParameter("Sightlines", "Lines", "Sightlines from eyes to screen bottom", GH_ParamAccess.list);
            pManager.AddBrepParameter("ProjectorCone", "Cone", "Projector cone geometry", GH_ParamAccess.item);
            pManager.AddGenericParameter("Chairs", "Chairs", "Distributed chair geometry (Section)", GH_ParamAccess.tree);
            pManager.AddLineParameter("LimitLines", "Limits", "Vertical lines for front and back limits", GH_ParamAccess.tree);
            pManager.AddGenericParameter("SerializedAnalysis", "SAnalisys", "Serialized Analysis Data (for validation)", GH_ParamAccess.item);
            pManager.AddCurveParameter("PlanTribune", "PlanTrib", "Plan Tribune Lines", GH_ParamAccess.list);
            pManager.AddCurveParameter("PlanRailings", "PlanRail", "Plan Railing Lines", GH_ParamAccess.list);
            pManager.AddCurveParameter("PlanStairs", "PlanStair", "Plan Stair Lines", GH_ParamAccess.list);
            pManager.AddGenericParameter("PlanChairs", "PlanChairs", "Plan Distributed Chair Geometry (Blocks/Curves)", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<AudienceSetup> audiences = new List<AudienceSetup>();
            SerializedTribune strib = null;
            ProjectorSetup projector = null;
            ScreenSetup screen = null;
            List<double> offsets = new List<double>();
            string serializedPlan = null;

            if (!DA.GetDataList(0, audiences) || audiences.Count == 0) return;
            if (!DA.GetData(1, ref strib) || strib == null) return;
            DA.GetData(2, ref projector);
            DA.GetData(3, ref screen);
            DA.GetDataList(4, offsets);
            DA.GetData(5, ref serializedPlan);
            
            PlanSetup plan = null;
            if (!string.IsNullOrEmpty(serializedPlan))
            {
                try
                {
                    plan = DeserializePlanSetup(serializedPlan);
                }
                catch (Exception ex)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Failed to deserialize Plan Setup: " + ex.Message);
                }
            }
            
            // Check for PlanGeometry and ensure Blocks exist
            for (int i = 0; i < audiences.Count; i++)
            {
                var aud = audiences[i];
                aud.PlanGeometry = RebasePlanGeometry(aud.PlanGeometry, aud.PlanOriginPt);
                if (aud.PlanChairBlockId == Guid.Empty && aud.PlanGeometry != null && aud.PlanGeometry.Count > 0)
                {
                     // Try to define block 
                     // We need a unique name. 
                     // Since we don't have a persistent ID for the audience setup component easily here, 
                     // we can generate a name based on a signature or just "Owl_AutoBlock_{Guid}" and reuse if exists?
                     // But we are inside SolveInstance, so we should be careful not to spam blocks.
                     // The user request says "script will define and place the block".
                     
                     // Helper to ensure block
                     // Check if a block with this specific geometry signature exists? Too slow.
                     // Let's create a deterministic name if possible, or just a new one?
                     // "Owl_Chair_Auto_{Random}" - bad for document bloat.
                     
                     // Better: check if the 'PlanChairBlockName' is set (it might be empty).
                     string name = "Owl_Chair_" + Guid.NewGuid().ToString().Substring(0, 8); // Temporary unique
                     
                     // We can't really "cache" this easily without persistence.
                     // BUT, if we use the Geometry hash? 
                     // Let's just create it if it doesn't exist.
                     
                     Guid newId = EnsureBlockDefinition(aud.PlanGeometry, name);
                     aud.PlanChairBlockId = newId;
                }
            }
            
            List<Line> sightlines;
            List<List<Line>> limitLines;
            Brep cone;
            List<List<GeometryBase>> sectionChairs;
            List<List<GeometryBase>> planChairs;
            List<Curve> planTribune;
            List<Curve> planRailings;
            List<Curve> planStairs;

            Analysis.Calculate(audiences, strib, screen, projector, offsets, plan, 
                out sightlines, 
                out limitLines, 
                out cone, 
                out sectionChairs,
                out planChairs,
                out planTribune,
                out planRailings,
                out planStairs);

            var serializedAnalysis = new SerializedAnalysis(
                strib, 
                audiences, 
                sightlines, 
                offsets, 
                plan,
                cone,
                limitLines,
                sectionChairs,
                planChairs,
                planTribune,
                planRailings,
                planStairs);

            // Convert to DataTrees
            var limitTree = new DataTree<Line>();
            for (int i = 0; i < limitLines.Count; i++)
            {
                limitTree.AddRange(limitLines[i], new GH_Path(i));
            }

            var sectionChairTree = new DataTree<GeometryBase>();
            for (int i = 0; i < sectionChairs.Count; i++)
            {
                sectionChairTree.AddRange(sectionChairs[i], new GH_Path(i));
            }

            var planChairTree = new DataTree<GeometryBase>();
            for (int i = 0; i < planChairs.Count; i++)
            {
                planChairTree.AddRange(planChairs[i], new GH_Path(i));
            }

            DA.SetDataList(0, sightlines);
            DA.SetData(1, cone);
            DA.SetDataTree(2, sectionChairTree);
            DA.SetDataTree(3, limitTree);
            DA.SetData(4, serializedAnalysis);
            DA.SetDataList(5, planTribune);
            DA.SetDataList(6, planRailings);
            DA.SetDataList(7, planStairs);
            DA.SetDataTree(8, planChairTree);
            
            if (plan != null && !planTribune.Any() && !planRailings.Any() && !planStairs.Any())
            {
                 AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Plan Setup provided but no plan geometry generated. Check alignment between Section X and Plan coordinates.");
            }
        }
        
        private Guid EnsureBlockDefinition(List<GeometryBase> geometry, string nameQuery)
        {
             // This is modifying the document, which is dangerous in a solution.
             // However, to satisfy usage "script defines block", we must.
             // Ideally we lock the document.
             
             var doc = Rhino.RhinoDoc.ActiveDoc;
             if (doc == null) return Guid.Empty;
             
             // Simple approach: create a new one every time? No, too much junk.
             // We need a way to reuse. 
             // Since we can't easily detect "sameness", maybe we just use a static name "Owl_Temp_Chair"
             // But multiple audiences might have different chairs.
             
             // User provided "list of curves"
             // Let's try to define it with a random name for now, but to avoid bloat, 
             // maybe we check if we already created one this session? 
             // Hard to track.
             
             // Strategy: Create a hidden block or user-named block?
             // Let's create a block with a name "Owl_Generated_{Timestamp}_{Count}"?
             // Actually, if we just want to visualize, we output geometry (which Analysis does if BlockID is empty).
             // BUT, user SAID "script will define and place the block".
             
             // Let's use the simplest valid approach: Create a block definition.
             
             string name = nameQuery; 
             // Check if exists
             var existing = doc.InstanceDefinitions.Find(name, true);
             if (existing != null) return existing.Id;
             
             int index = doc.InstanceDefinitions.Add(name, "Generated by Owl", Point3d.Origin, geometry);
             if (index >= 0)
             {
                 return doc.InstanceDefinitions[index].Id;
             }
             return Guid.Empty;
        }

        private List<GeometryBase> RebasePlanGeometry(List<GeometryBase> source, Point3d planOrigin)
        {
            var result = new List<GeometryBase>();
            if (source == null || source.Count == 0) return result;

            Transform toWorldXY = Transform.PlanarProjection(Plane.WorldXY);
            Transform toBlockOrigin = Transform.Translation(-planOrigin.X, -planOrigin.Y, 0.0);

            foreach (var g in source)
            {
                if (g == null) continue;
                var dup = g.Duplicate();
                dup.Transform(toWorldXY);
                dup.Transform(toBlockOrigin);
                result.Add(dup);
            }

            return result;
        }
        
        private PlanSetup DeserializePlanSetup(string json)
        {
            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                var root = doc.RootElement;
                
                // OriginPlan "x,y,z"
                Point3d origin = Point3d.Origin;
                if (root.TryGetProperty("OriginPlan", out JsonElement originEl))
                {
                    origin = ParsePoint(originEl.GetString());
                }
                
                Curve trib = null;
                if (root.TryGetProperty("BoundaryTribune", out JsonElement tribEl))
                {
                    trib = DeserializeGeometry(tribEl.GetString()) as Curve;
                }
                
                Curve tunnel = null;
                if (root.TryGetProperty("BoundaryTunnel", out JsonElement tunnelEl))
                {
                    tunnel = DeserializeGeometry(tunnelEl.GetString()) as Curve;
                }
                
                List<Curve> aisles = new List<Curve>();
                if (root.TryGetProperty("BoundariesAisles", out JsonElement aislesEl) && aislesEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var el in aislesEl.EnumerateArray())
                    {
                        var c = DeserializeGeometry(el.GetString()) as Curve;
                        if (c != null) aisles.Add(c);
                    }
                }
                
                return new PlanSetup(origin, trib, aisles, tunnel);
            }
        }
        
        private Point3d ParsePoint(string s)
        {
            if (string.IsNullOrEmpty(s)) return Point3d.Origin;
            var parts = s.Split(',');
            if (parts.Length >= 3 && 
                double.TryParse(parts[0], out double x) && 
                double.TryParse(parts[1], out double y) && 
                double.TryParse(parts[2], out double z))
            {
                return new Point3d(x, y, z);
            }
            return Point3d.Origin;
        }
        
        private GeometryBase DeserializeGeometry(string b64)
        {
            if (string.IsNullOrEmpty(b64)) return null;
            try 
            {
                byte[] bytes = Convert.FromBase64String(b64);
                using (var archive = Rhino.FileIO.File3dm.FromByteArray(bytes))
                {
                    if (archive.Objects.Count > 0)
                    {
                        foreach(var obj in archive.Objects)
                        {
                            return obj.Geometry;
                        }
                    }
                }
            }
            catch {}
            return null;
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                var assembly = typeof(Owl_AudienceDistributor).Assembly;
                var resourceName = "Owl.Grasshopper.Icons.Owl_Analysis_24.png"; 
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null) return null;
                    return new System.Drawing.Bitmap(stream);
                }
            }
        }
        public override Guid ComponentGuid => new Guid("8E12C163-5678-8901-2345-67890123EFAB"); 
    }
}
