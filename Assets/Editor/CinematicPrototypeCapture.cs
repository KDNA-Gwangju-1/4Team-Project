using UnityEngine;
using UnityEditor;
using System.Linq;

// Explicit offline preview capture. No runtime scene or opening-video wiring.
public static class CinematicPrototypeCapture
{
    public static string RenderArc(string cut, Vector3 from, Vector3 via, Vector3 to, Vector3 focus, float seconds, float fovFrom, float fovTo)
    {
        var source = Camera.main;
        var go = new GameObject("CinematicCapture_TEMP");
        var camera = go.AddComponent<Camera>();
        camera.CopyFrom(source);
        camera.enabled = false;
        camera.aspect = 16f / 9f;
        var target = new RenderTexture(1280, 720, 24);
        var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        var previous = RenderTexture.active;
        camera.targetTexture = target;
        string directory = "output/cinematic-v2-frames/" + cut;
        System.IO.Directory.CreateDirectory(directory);
        try
        {
            int count = Mathf.RoundToInt(seconds * 24);
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)(count - 1);
                float p = t * t * (3 - 2 * t);
                camera.transform.position = (1-p)*(1-p)*from + 2*(1-p)*p*via + p*p*to;
                camera.transform.LookAt(focus);
                camera.fieldOfView = Mathf.Lerp(fovFrom, fovTo, p);
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                texture.Apply();
                System.IO.File.WriteAllBytes(directory + "/" + i.ToString("D4") + ".jpg", texture.EncodeToJPG(92));
            }
        }
        finally
        {
            RenderTexture.active = previous;
            camera.targetTexture = null;
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(target);
        }
        return directory;
    }
    public static string RenderShot(string cut, Vector3 from, Vector3 to, Vector3 lookFrom, Vector3 lookTo)
    {
        var src=Camera.main; var go=new GameObject("CinematicCapture_TEMP");var c=go.AddComponent<Camera>();c.CopyFrom(src);c.enabled=false;c.fieldOfView=48;c.aspect=16f/9f;var rt=new RenderTexture(1280,720,24);var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);var old=RenderTexture.active;c.targetTexture=rt;string dir="output/cinematic-frames/"+cut;System.IO.Directory.CreateDirectory(dir);try{for(int i=0;i<96;i++){float t=i/95f;float p=t*t*(3-2*t);c.transform.position=Vector3.Lerp(from,to,p);c.transform.LookAt(Vector3.Lerp(lookFrom,lookTo,p)); c.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();System.IO.File.WriteAllBytes(dir+"/"+i.ToString("D4")+".jpg",tex.EncodeToJPG(90));}}finally{RenderTexture.active=old;c.targetTexture=null;UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);}return dir;
    }
}
