using UnityEngine;
using UnityEditor;
using System.Linq;

// Explicit offline preview capture. No runtime scene or opening-video wiring.
public static class CinematicPrototypeCapture
{
    public static string RenderShot(string cut, Vector3 from, Vector3 to, Vector3 lookFrom, Vector3 lookTo)
    {
        var src=Camera.main; var go=new GameObject("CinematicCapture_TEMP");var c=go.AddComponent<Camera>();c.CopyFrom(src);c.enabled=false;c.fieldOfView=48;c.aspect=16f/9f;var rt=new RenderTexture(1280,720,24);var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);var old=RenderTexture.active;c.targetTexture=rt;string dir="output/cinematic-frames/"+cut;System.IO.Directory.CreateDirectory(dir);try{for(int i=0;i<96;i++){float t=i/95f;float p=t*t*(3-2*t);c.transform.position=Vector3.Lerp(from,to,p);c.transform.LookAt(Vector3.Lerp(lookFrom,lookTo,p)); c.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();System.IO.File.WriteAllBytes(dir+"/"+i.ToString("D4")+".jpg",tex.EncodeToJPG(90));}}finally{RenderTexture.active=old;c.targetTexture=null;UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);}return dir;
    }
}
