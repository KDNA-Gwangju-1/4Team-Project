using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using BrightDream.Combat;
using Object = UnityEngine.Object;

public static class CorruptionReadabilityReview
{
    const string Output="ArtSource/CorruptionReadability/";
    static readonly string[] Species={"cotton_soldier","patchwork_scarecrow","button_knight","yarn_spiderling","felt_golem"};
    [MenuItem("Tools/Bright Dream/Corruption/Capture Before")]
    public static void Before()=>Capture("Before");
    [MenuItem("Tools/Bright Dream/Corruption/Capture After")]
    public static void After()=>Capture("After");
    [MenuItem("Tools/Bright Dream/Corruption/Capture Animated")]
    public static void Animated()=>Capture("Animated");
    [MenuItem("Tools/Bright Dream/Corruption/Install On Combat Templates")]
    public static void Install()
    {
        var scene=SceneManager.GetActiveScene();int count=0;
        foreach(var combat in Object.FindObjectsByType<MonsterCombat>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(c=>c.gameObject.scene==scene))
        {
            var visual=combat.GetComponent<MonsterCorruptionVisual>();if(!visual)visual=Undo.AddComponent<MonsterCorruptionVisual>(combat.gameObject);
            var so=new SerializedObject(visual);var textures=so.FindProperty("blobTextures");textures.arraySize=3;
            for(int i=0;i<3;i++)textures.GetArrayElementAtIndex(i).objectReferenceValue=AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/BrightDream/Textures/UI/Blob_{i+1:D2}.png");
            so.FindProperty("minBlobs").intValue=1;so.FindProperty("maxBlobs").intValue=3;so.ApplyModifiedProperties();count++;
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        Debug.Log("Corruption visual configured on "+count+" combat templates; combat state unchanged.");
    }
    [MenuItem("Tools/Bright Dream/Corruption/Update Ink Textures")]
    public static void UpdateInk()
    {
        for(int i=0;i<3;i++)
        {
            var tex=MonsterCorruptionVisual.CreateInkTexture(i);
            string path=$"Assets/BrightDream/Textures/UI/Blob_{i+1:D2}.png";
            File.WriteAllBytes(path,tex.EncodeToPNG());Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Default;importer.alphaIsTransparency=true;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Trilinear;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        }
    }
    [MenuItem("Tools/Bright Dream/Corruption/Validate State")]
    public static void ValidateState()
    {
        var sources=Object.FindObjectsByType<MonsterCombat>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(c=>c.gameObject.scene==SceneManager.GetActiveScene()).ToArray();
        var preview=EditorSceneManager.NewPreviewScene();var report=new System.Text.StringBuilder();
        try
        {
            foreach(var source in sources)
            {
                if(!source.GetComponent<MonsterCorruptionVisual>())throw new Exception("Missing visual: "+source.name);
                var clone=Object.Instantiate(source.gameObject);SceneManager.MoveGameObjectToScene(clone,preview);clone.SetActive(true);
                var combat=clone.GetComponent<MonsterCombat>();combat.enabled=false;var visual=clone.GetComponent<MonsterCorruptionVisual>();
                var so=new SerializedObject(combat);int colliders=clone.GetComponentsInChildren<Collider>(true).Length;
                so.FindProperty("needsPurification").boolValue=false;so.ApplyModifiedPropertiesWithoutUndo();visual.RefreshVisual();
                if(clone.GetComponentsInChildren<Renderer>().Any(r=>r.name=="MainStain"))throw new Exception("Innocent showed corruption");
                var rng=UnityEngine.Random.state;so.FindProperty("needsPurification").boolValue=true;so.ApplyModifiedPropertiesWithoutUndo();
                var timer=System.Diagnostics.Stopwatch.StartNew();visual.RefreshVisual();timer.Stop();
                var stains=clone.GetComponentsInChildren<Renderer>().Where(r=>r.name=="MainStain"||r.name.StartsWith("Blob_")).ToArray();
                if(stains.Count(r=>r.name=="MainStain")!=1 || stains.Length<2 || stains.Length>4)throw new Exception("Invalid stain count "+source.name);
                var spread=clone.GetComponentsInChildren<Renderer>().Where(r=>r.name.StartsWith("Spread_")).ToArray();
                if(spread.Length!=6)throw new Exception("Missing full-body region "+source.name);
                if(!rng.Equals(UnityEngine.Random.state))throw new Exception("Visual changed gameplay random state");
                foreach(var r in stains)if(r is SkinnedMeshRenderer skin && skin.bones.Length==0)throw new Exception("Stain lost animation bones");
                so.FindProperty("needsPurification").boolValue=false;so.ApplyModifiedPropertiesWithoutUndo();visual.RefreshVisual();
                if(clone.GetComponentsInChildren<Renderer>().Any(r=>r.name=="MainStain"))throw new Exception("State transition did not hide corruption");
                so.FindProperty("needsPurification").boolValue=true;so.ApplyModifiedPropertiesWithoutUndo();visual.RefreshVisual();
                if(clone.GetComponentsInChildren<Renderer>().Count(r=>r.name=="MainStain")!=1)throw new Exception("State transition duplicated stain");
                if(clone.GetComponentsInChildren<Collider>(true).Length!=colliders)throw new Exception("Visual changed colliders");
                report.AppendLine($"PASS {source.name}: Innocent -> Target -> Innocent -> Target; {stains.Length} stains + {spread.Length} full-body regions; colliders unchanged; RNG unchanged; first build {timer.ElapsedMilliseconds}ms");
                Object.DestroyImmediate(clone);
            }
            File.WriteAllText(Output+"validation.txt",report.ToString());Debug.Log(report.ToString());
        }
        finally{EditorSceneManager.ClosePreviewScene(preview);}
    }

    static void Capture(string phase)
    {
        Directory.CreateDirectory(Output+phase);
        var originals=Object.FindObjectsByType<MonsterCombat>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(c=>c.gameObject.scene==SceneManager.GetActiveScene()).ToArray();
        var preview=EditorSceneManager.NewPreviewScene();
        GameObject rig=new GameObject("Corruption visual review only");SceneManager.MoveGameObjectToScene(rig,preview);
        var report=new System.Text.StringBuilder();
        try
        {
            var cameraObject=new GameObject("Review Camera",typeof(Camera));cameraObject.transform.SetParent(rig.transform,false);
            var camera=cameraObject.GetComponent<Camera>();camera.scene=preview;camera.fieldOfView=60;camera.nearClipPlane=.03f;camera.farClipPlane=40;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.32f,.38f,.34f);camera.allowHDR=false;camera.allowMSAA=true;
            var lightObject=new GameObject("Review Sun",typeof(Light));lightObject.transform.SetParent(rig.transform,false);lightObject.transform.rotation=Quaternion.Euler(35,-25,0);var light=lightObject.GetComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;
            var fillObject=new GameObject("Review Fill",typeof(Light));fillObject.transform.SetParent(rig.transform,false);fillObject.transform.rotation=Quaternion.Euler(15,150,0);fillObject.GetComponent<Light>().type=LightType.Directional;fillObject.GetComponent<Light>().intensity=.45f;
            var floor=GameObject.CreatePrimitive(PrimitiveType.Plane);floor.transform.SetParent(rig.transform,false);floor.transform.localScale=Vector3.one*3;var floorMaterial=new Material(Shader.Find("Standard"));floorMaterial.color=new Color(.52f,.61f,.44f);floor.GetComponent<Renderer>().sharedMaterial=floorMaterial;
            foreach(string species in Species)
            {
                var source=originals.First(c=>c.name.Contains(species));
                var pair=new GameObject[2];
                for(int i=0;i<2;i++)
                {
                    pair[i]=Object.Instantiate(source.gameObject,rig.transform);pair[i].name=i==0?"Target":"Innocent";
                    pair[i].transform.localPosition=new Vector3(i==0?-.85f:.85f,0,0);pair[i].transform.localRotation=Quaternion.Euler(0,180,0);pair[i].SetActive(true);
                    var combat=pair[i].GetComponent<MonsterCombat>();combat.enabled=false;
                    var so=new SerializedObject(combat);so.FindProperty("needsPurification").boolValue=i==0;so.ApplyModifiedPropertiesWithoutUndo();
                    var visual=pair[i].GetComponent<MonsterCorruptionVisual>();
                    var refresh=typeof(MonsterCorruptionVisual).GetMethod("RefreshVisual");
                    if(refresh!=null)
                    {
                        if(!visual)visual=pair[i].AddComponent<MonsterCorruptionVisual>();refresh.Invoke(visual,null);
                    }
                    else if(visual && i==0)typeof(MonsterCorruptionVisual).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(visual,null);
                    else if(visual)Object.DestroyImmediate(visual);
                    if(phase=="Animated")
                    {
                        var animation=pair[i].GetComponent<Animation>();
                        if(animation && animation.clip){animation.clip.SampleAnimation(pair[i],.35f);report.AppendLine(species+" sampled animation "+animation.clip.name);}
                        pair[i].transform.localPosition=new Vector3(i==0?-.85f:.85f,0,0);pair[i].transform.localRotation=Quaternion.Euler(0,180,0);
                    }
                    foreach(var mesh in pair[i].GetComponentsInChildren<MeshFilter>())report.AppendLine($"{species} {i} {mesh.name} readable={mesh.sharedMesh.isReadable} vertices={mesh.sharedMesh.vertexCount}");
                    foreach(var skin in pair[i].GetComponentsInChildren<SkinnedMeshRenderer>())report.AppendLine($"{species} skinned {skin.name} vertices={skin.sharedMesh.vertexCount} bones={skin.bones.Length}");
                }
                var rs=pair[0].GetComponentsInChildren<Renderer>().Where(r=>!r.transform.parent.name.Contains("Corruption")).ToArray();
                Bounds b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
                report.AppendLine($"{species} world bounds {b.size} root scale {pair[0].transform.lossyScale}");
                foreach(float distance in new[]{3f,7f,10f})
                {
                    camera.transform.position=new Vector3(0,b.center.y,-distance);camera.transform.rotation=Quaternion.identity;
                    var rt=RenderTexture.GetTemporary(1920,1080,24,RenderTextureFormat.ARGB32);rt.antiAliasing=4;camera.targetTexture=rt;
                    camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;
                    var texture=new Texture2D(1920,1080,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1920,1080),0,0);texture.Apply();
                    File.WriteAllBytes(Output+phase+"/"+species+"_"+distance+"m.png",texture.EncodeToPNG());
                    RenderTexture.active=previous;camera.targetTexture=null;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(texture);
                }
                foreach(var p in pair)Object.DestroyImmediate(p);
            }
            Object.DestroyImmediate(floorMaterial);
            File.WriteAllText(Output+phase+"/review.txt",report.ToString());
            Debug.Log("Corruption review captures completed: "+phase);
        }
        finally {Object.DestroyImmediate(rig);EditorSceneManager.ClosePreviewScene(preview);}
    }
}
