using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class GardenDensityInstaller
{
    [MenuItem("Tools/Bright Dream/Fill Garden Planting Beds")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop play first.");
        if (GameObject.Find("GardenPlanting_Authored") != null) throw new InvalidOperationException("Planting already exists.");
        var sources = UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);
        var flowers = sources.Where(m=>m.name.StartsWith("21_flower_large")).ToArray();
        var bush = sources.First(m=>m.name=="19_bush");
        var tree = sources.First(m=>m.name=="나무4 (2)");
        var clues = UnityEngine.Object.FindObjectsByType<BrightDream.Clues.ClueInteractable>(FindObjectsSortMode.None);
        var paths = GameObject.Find("GardenPath_Continuous").GetComponent<Collider>();
        var root = new GameObject("GardenPlanting_Authored");
        Undo.RegisterCreatedObjectUndo(root,"Fill garden planting");
        var rng = new System.Random(8251);
        Func<float,float,float> random=(a,b)=>a+(float)rng.NextDouble()*(b-a);
        Func<Vector3,float,bool> clear=(p,margin)=>
        {
            if(Mathf.Pow((p.x-8.07f)/19.3f,2)+Mathf.Pow((p.z-67.61f)/17.5f,2)<1) return false;
            if(Vector2.Distance(new Vector2(p.x,p.z),new Vector2(30.4f,37.68f))<9.3f) return false;
            if(Vector2.Distance(new Vector2(p.x,p.z),new Vector2(18.5f,18.5f))<6.3f) return false;
            if(p.x>4 && p.x<14 && p.z>25 && p.z<35) return false;
            if(clues.Any(c=>Vector3.Distance(c.transform.position,p)<3.2f+margin))return false;
            for(int i=0;i<9;i++) { var offset=i==0?Vector3.zero:new Vector3(Mathf.Cos(i*Mathf.PI/4),0,Mathf.Sin(i*Mathf.PI/4))*margin; RaycastHit h;if(paths.Raycast(new Ray(p+offset+Vector3.up*8,Vector3.down),out h,10))return false; }
            return true;
        };
        Action<MeshFilter,Vector3,float,string> place=(source,p,factor,label)=>
        {
            var obj=new GameObject(label);obj.transform.SetParent(root.transform,false);
            obj.transform.position=p;obj.transform.rotation=Quaternion.AngleAxis(random(0,360),Vector3.up)*source.transform.rotation;
            obj.transform.localScale=source.transform.lossyScale*factor;
            obj.AddComponent<MeshFilter>().sharedMesh=source.sharedMesh;
            var r=obj.AddComponent<MeshRenderer>();r.sharedMaterials=source.GetComponent<Renderer>().sharedMaterials;
            obj.transform.position+=Vector3.up*(-r.bounds.min.y-.015f);
            GameObjectUtility.SetStaticEditorFlags(obj,StaticEditorFlags.BatchingStatic);
        };
        int count=0;
        for(int attempt=0;attempt<600 && count<26;attempt++)
        {
            var center=new Vector3(random(-6,35),0,random(2,57));if(!clear(center,2))continue;
            count++;
            for(int i=0;i<8;i++){float a=i*2.39996f,r=.35f*Mathf.Sqrt(i);var p=center+new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r);if(clear(p,.6f))place(flowers[rng.Next(flowers.Length)],p,random(.8f,1.25f),"FlowerDrift_"+count+"_"+i);}
            if(clear(center+Vector3.right*1.4f,1.5f))place(bush,center+Vector3.right*1.4f,random(.6f,.9f),"CompanionShrub_"+count);
        }
        for(int i=0;i<18;i++)
        {
            var p=new Vector3(i%2==0?random(-6.5f,-3.5f):random(30,35),0,3+i*2.95f);
            if(clear(p,2.8f))place(tree,p,random(.8f,1.15f),"BackgroundCanopy_"+i);
        }
        foreach(var bed in new[]{new Vector3(.5f,0,16),new Vector3(5.5f,0,16),new Vector3(9,0,18),new Vector3(13,0,19.5f),new Vector3(24,0,29),new Vector3(26,0,47),new Vector3(17,0,44),new Vector3(2,0,37),new Vector3(29,0,53),new Vector3(33,0,22)})
            for(int i=0;i<12;i++)
            {
                float a=i*2.39996f,r=.38f*Mathf.Sqrt(i);
                var p=bed+new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r);
                if(clear(p,.7f))place(flowers[rng.Next(flowers.Length)],p,random(1f,1.45f),"ForegroundFlowerBed");
            }
        EditorSceneManager.MarkSceneDirty(root.scene);EditorSceneManager.SaveScene(root.scene);
    }
}
