using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Asset-only tooling. Does not replace objects or save the open scene.
public static class CraftTreehouseAssetBuilder
{
    const string Folder = "Assets/BrightDream/Art/CraftTreehouse";
    [Serializable] public class BoxSpec { public string name; public float[] center; public float[] size; public string kind; }
    [Serializable] public class CenterSpec { public string name; public float[] center; }
    [Serializable] public class RoutePoint { public float[] point; }
    [Serializable] public class Layout { public BoxSpec[] colliders; public CenterSpec[] centers; public RoutePoint[] route; public float[] sourceShift; public float height; public float stepRise; public float stepTread; }
    [Serializable] public class Result
    {
        public bool passed;
        public int meshCount, triangleCount, materialCount, boxColliderCount;
        public Vector3 dimensions;
        public float platformHeight, controllerHeight = 1.8f, controllerRadius = .35f, stepOffset = .35f;
        public bool forwardTraversal, returnTraversal;
        public float axisFitError;
        public string coordinateMapping, note;
    }

    [MenuItem("Tools/Bright Dream/Craft Treehouse/Build and Verify Asset")]
    public static void Build()
    {
        var shader = Shader.Find("BrightDream/TreehouseCraft");
        if (!shader) throw new InvalidOperationException("TreehouseCraft shader is not imported yet.");
        var names = new[] { "TH_01_CardboardWood", "TH_02_PaintedCream", "TH_03_ButterPaper", "TH_04_FeltMint", "TH_05_CraftColors" };
        var colors = new[] { new Color(.46f,.25f,.12f), new Color(.91f,.81f,.60f), new Color(.87f,.62f,.28f), new Color(.31f,.58f,.40f), Color.white };
        var mats = new Material[5];
        for (int i = 0; i < mats.Length; i++)
        {
            string p = Folder + "/" + names[i] + ".mat";
            mats[i] = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (!mats[i]) { mats[i] = new Material(shader) { name = names[i] }; AssetDatabase.CreateAsset(mats[i], p); }
            mats[i].shader = shader; mats[i].SetColor("_Color", colors[i]); mats[i].SetFloat("_VertexTint", i == 4 ? 1 : 0); mats[i].SetFloat("_Glossiness", .15f);
            EditorUtility.SetDirty(mats[i]);
        }
        var importer = AssetImporter.GetAtPath(Folder + "/CraftTreehouse.fbx") as ModelImporter;
        if (!importer) throw new InvalidOperationException("FBX is not imported yet.");
        importer.globalScale = 1; importer.useFileScale = true; importer.bakeAxisConversion = true; importer.importAnimation = false; importer.animationType = ModelImporterAnimationType.None;
        importer.addCollider = false; importer.importCameras = false; importer.importLights = false;
        for (int i = 0; i < mats.Length; i++) importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), names[i]), mats[i]);
        importer.SaveAndReimport();
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/CraftTreehouse.fbx");
        var layout = JsonUtility.FromJson<Layout>(File.ReadAllText(Folder + "/CollisionLayout.json"));
        GameObject root = null, player = null, ground = null;
        var result = new Result { platformHeight = layout.height };
        try
        {
            root = new GameObject("CraftTreehouse_Playable");
            var visual = Object.Instantiate(model, root.transform); visual.name = "Visual";
            visual.transform.localPosition = Vector3.zero;
            // Fit handedness from imported bounds; never assume the FBX importer's X/Z signs.
            float best = float.PositiveInfinity; int sx = 1, sy = 1;
            foreach (int xsign in new[] {-1,1}) foreach (int ysign in new[] {-1,1})
            {
                float error = 0;
                foreach (var expected in layout.centers)
                {
                    var mf = visual.GetComponentsInChildren<MeshFilter>().Single(m => m.name == expected.name);
                    Vector3 actual = root.transform.InverseTransformPoint(mf.transform.TransformPoint(mf.sharedMesh.bounds.center));
                    Vector3 e = new Vector3(expected.center[0]*xsign, expected.center[2], expected.center[1]*ysign);
                    error += (actual-e).sqrMagnitude;
                }
                if (error < best) { best=error; sx=xsign; sy=ysign; }
            }
            result.axisFitError = Mathf.Sqrt(best);
            if (result.axisFitError > .02f) throw new InvalidOperationException("Unexpected model coordinate mapping: " + result.axisFitError);
            Vector3 Map(float[] v) => new Vector3(v[0]*sx,v[2],v[1]*sy);
            result.coordinateMapping = $"Unity=({sx}*BlenderX, BlenderZ, {sy}*BlenderY)";
            var collisionRoot = new GameObject("Collision - simple boxes"); collisionRoot.transform.SetParent(root.transform,false);
            foreach (var spec in layout.colliders)
            {
                var go = new GameObject(spec.name); go.transform.SetParent(collisionRoot.transform,false); go.transform.localPosition=Map(spec.center);
                var box=go.AddComponent<BoxCollider>(); box.size=new Vector3(spec.size[0],spec.size[2],spec.size[1]);
            }
            var filters = visual.GetComponentsInChildren<MeshFilter>();
            result.meshCount=filters.Length; result.triangleCount=filters.Sum(f=>f.sharedMesh.triangles.Length/3);
            var renderers=visual.GetComponentsInChildren<MeshRenderer>();
            result.materialCount=renderers.SelectMany(r=>r.sharedMaterials).Distinct().Count();
            if (renderers.SelectMany(r=>r.sharedMaterials).Any(m=>!m || m.shader!=shader)) throw new InvalidOperationException("Material remapping is incomplete.");
            var bounds=renderers[0].bounds; foreach(var r in renderers) bounds.Encapsulate(r.bounds); result.dimensions=bounds.size;
            result.boxColliderCount=root.GetComponentsInChildren<BoxCollider>().Length;
            // Synchronous CharacterController.Move calls at an isolated location; no mode switch.
            var isolation=new Vector3(10000,0,10000); root.transform.position=isolation;
            ground=new GameObject("TEMP treehouse traversal ground"); ground.transform.position=isolation+Vector3.down*.15f; ground.AddComponent<BoxCollider>().size=new Vector3(40,.3f,40);
            player=new GameObject("TEMP reference player");
            player.transform.position=isolation+Map(layout.route[0].point)+Vector3.up*.04f;
            var cc=player.AddComponent<CharacterController>(); cc.height=1.8f; cc.radius=.35f; cc.center=new Vector3(0,.9f,0); cc.stepOffset=.35f; cc.slopeLimit=45; cc.skinWidth=.025f; cc.minMoveDistance=0;
            Physics.SyncTransforms();
            bool Walk(IEnumerable<RoutePoint> route)
            {
                foreach(var waypoint in route)
                {
                    Vector3 target=isolation+Map(waypoint.point);
                    bool reached=false;
                    for(int j=0;j<450;j++)
                    {
                        Vector3 delta=target-player.transform.position; delta.y=0;
                        if(delta.magnitude<.075f) { reached=true; break; }
                        cc.Move(Vector3.ClampMagnitude(delta,.035f)+Vector3.down*.055f);
                    }
                    for(int settle=0;settle<4;settle++) cc.Move(Vector3.down*.035f);
                    if(!reached || Mathf.Abs(player.transform.position.y-target.y)>.22f)
                    {
                        result.note=$"Traversal failed at {target-isolation}; actual {player.transform.position-isolation}; horizontal reached={reached}";
                        return false;
                    }
                }
                return true;
            }
            result.forwardTraversal=Walk(layout.route.Skip(1));
            result.returnTraversal=result.forwardTraversal && Walk(layout.route.Reverse().Skip(1));
            result.passed=result.forwardTraversal && result.returnTraversal && result.meshCount==9 && result.materialCount==5;
            root.transform.position=Vector3.zero;
            PrefabUtility.SaveAsPrefabAsset(root,Folder+"/CraftTreehouse_Playable.prefab");
            if(result.passed) result.note="Reference CharacterController traversed stairs, landing, porch, doorway, interior and returned. Synchronous isolated physics check; no scene placement or interactive input/camera play-test.";
            Directory.CreateDirectory("ArtSource/CraftTreehouse");
            File.WriteAllText("ArtSource/CraftTreehouse/unity_validation.json",JsonUtility.ToJson(result,true));
            AssetDatabase.SaveAssets();
            if(!result.passed) throw new InvalidOperationException(result.note);
            Debug.Log("CraftTreehouse verification passed: "+JsonUtility.ToJson(result));
        }
        finally
        {
            if(player) Object.DestroyImmediate(player);
            if(ground) Object.DestroyImmediate(ground);
            if(root) Object.DestroyImmediate(root);
        }
    }
}
