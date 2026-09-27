
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BrightDream.Combat
{
    /// <summary>
    /// Surface-bound ink. MonsterCombat.NeedsPurification is the single source of truth for Target/
    /// Innocent - this component only reads it (never writes), so it always matches the gameplay
    /// judgement (shot/contact outcomes in MonsterCombat) exactly, whatever value the spawner picked.
    /// </summary>
    [DisallowMultipleComponent]
    public class MonsterCorruptionVisual : MonoBehaviour
    {
        [SerializeField] private Texture2D[] blobTextures;
        [Tooltip("Supporting stains; one guaranteed MainStain is additional.")]
        [SerializeField, Range(1,3)] private int minBlobs=1;
        [SerializeField, Range(1,3)] private int maxBlobs=3;
        [SerializeField] private Color[] tintPalette={Color.white,new Color(.98f,.95f,1,1)};
        private MonsterCombat combat;
        private Transform container;
        private readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();
        private static Texture2D[] fallbackTextures;
        // Build after combat Awake has collected body renderers. Do not alter combat materials.
        private void Start()=>RefreshVisual();
        private void LateUpdate()=>RefreshVisual();
        private void OnDisable(){if(container)container.gameObject.SetActive(false);}
        public void RefreshVisual()
        {
            if(!combat)combat=GetComponent<MonsterCombat>();
            bool show=isActiveAndEnabled && combat && combat.NeedsPurification;
            if(!show){if(container && container.gameObject.activeSelf)container.gameObject.SetActive(false);return;}
            if(container){if(!container.gameObject.activeSelf)container.gameObject.SetActive(true);return;}
            BuildStains();
        }
        private void BuildStains()
        {
            // Visual randomness must not consume the spawner's UnityEngine.Random stream.
            var random=new System.Random(GetInstanceID());Renderer body=null;
            foreach(var r in GetComponentsInChildren<Renderer>(true))
                if((r is SkinnedMeshRenderer || r.GetComponent<MeshFilter>()) && (!body || r.bounds.size.sqrMagnitude>body.bounds.size.sqrMagnitude))body=r;
            if(!body)return;
            var skin=body as SkinnedMeshRenderer;Mesh source=skin?skin.sharedMesh:body.GetComponent<MeshFilter>().sharedMesh;
            if(!source || !source.isReadable)return;
            Mesh posed=source;if(skin){posed=new Mesh();skin.BakeMesh(posed);}
            var bind=source.vertices;var bindNormals=source.normals;var weights=source.boneWeights;var vertices=posed.vertices;var triangles=source.triangles;
            Matrix4x4 toRoot=transform.worldToLocalMatrix*body.transform.localToWorldMatrix;
            Bounds bounds=new Bounds(toRoot.MultiplyPoint3x4(vertices[0]),Vector3.zero);
            for(int i=0;i<vertices.Length;i++){vertices[i]=toRoot.MultiplyPoint3x4(vertices[i]);bounds.Encapsulate(vertices[i]);}
            if(skin)Release(posed);
            container=new GameObject("CorruptionVisual").transform;container.SetParent(body.transform,false);
            var shader=Shader.Find("Sprites/Default");if(!shader){Release(container.gameObject);container=null;return;}
            var textures=blobTextures==null?null:Array.FindAll(blobTextures,t=>t);
            if(textures==null || textures.Length==0 || Array.TrueForAll(textures,t=>!t))
            {
                if(fallbackTextures==null || !fallbackTextures[0]){fallbackTextures=new Texture2D[3];for(int i=0;i<3;i++)fallbackTextures[i]=CreateInkTexture(i);}
                textures=fallbackTextures;
            }
            float reference=Mathf.Max(bounds.size.y,Mathf.Min(bounds.size.x,bounds.size.z));
            // Transparent margins leave a visible stain about 36–40% of body height.
            Project("MainStain",Vector3.forward,bounds.center-Vector3.up*bounds.size.y*.035f,reference*.48f,reference*.48f,textures[random.Next(textures.Length)],Color.white);
            // Spread across the silhouette as well as the torso. Each region is guaranteed,
            // while its position and ink shape vary per instance without changing combat RNG.
            Spread("Head",0,.80f,.32f,.34f);
            Spread("LeftArm",-.34f,.55f,.25f,.25f);
            Spread("RightArm",.34f,.55f,.25f,.25f);
            Spread("LeftLeg",-.12f,.18f,.24f,.32f);
            Spread("RightLeg",.12f,.18f,.24f,.32f);
            Project("Spread_Back",Vector3.back,bounds.center,reference*.52f,reference*.64f,textures[random.Next(textures.Length)],Color.white);
            void Spread(string region,float x,float y,float width,float height)
            {
                var wanted=new Vector3(bounds.center.x+bounds.size.x*x,bounds.min.y+bounds.size.y*y,bounds.center.z);
                // Snap to real geometry so narrow limbs and different species proportions work.
                Vector3 nearest=wanted;float best=float.PositiveInfinity;
                foreach(var vertex in vertices)
                {
                    float score=Mathf.Pow(vertex.x-wanted.x,2)+Mathf.Pow(vertex.y-wanted.y,2);
                    if(score<best){best=score;nearest=vertex;}
                }
                nearest.y+=reference*Mathf.Lerp(-.018f,.018f,(float)random.NextDouble());
                Project("Spread_"+region,Vector3.forward,nearest,reference*width,reference*height,textures[random.Next(textures.Length)],Color.white);
            }
            int low=Mathf.Clamp(minBlobs,1,3),high=Mathf.Clamp(maxBlobs,low,3),count=random.Next(low,high+1);
            for(int i=0;i<count;i++)
            {
                float angle=((i%2==0?-65:65)+(float)random.NextDouble()*40-20)*Mathf.Deg2Rad;
                float size=reference*Mathf.Lerp(.19f,.30f,(float)random.NextDouble());
                var center=bounds.center+Vector3.up*bounds.size.y*Mathf.Lerp(-.18f,.18f,(float)random.NextDouble());
                var tint=tintPalette!=null && tintPalette.Length>0?tintPalette[random.Next(tintPalette.Length)]:Color.white;
                Project("Blob_"+(i+1).ToString("D2"),new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle)),center,size,size*1.12f,textures[random.Next(textures.Length)],tint);
            }
            void Project(string name,Vector3 outward,Vector3 center,float width,float height,Texture2D texture,Color tint)
            {
                if(!texture)return;
                Vector3 right=Vector3.Cross(Vector3.up,outward).normalized;
                var points=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();
                var skinWeights=new List<BoneWeight>();var indices=new List<int>();var reused=new Dictionary<int,int>();
                float offset=.0015f/Mathf.Max(.001f,body.transform.lossyScale.magnitude/Mathf.Sqrt(3));
                // Clip original surface triangles, retaining their curvature and bone weights.
                // A coarse projected quad grid would cut across yarn ridges and cause pinholes.
                for(int t=0;t<triangles.Length;t+=3)
                {
                    int ia=triangles[t],ib=triangles[t+1],ic=triangles[t+2];
                    Vector3 a=vertices[ia]-center,b=vertices[ib]-center,c=vertices[ic]-center;
                    if(Vector3.Dot(Vector3.Cross(b-a,c-a).normalized,outward)<.02f)continue;
                    Vector2 pa=new Vector2(Vector3.Dot(a,right)/width+.5f,a.y/height+.5f),pb=new Vector2(Vector3.Dot(b,right)/width+.5f,b.y/height+.5f),pc=new Vector2(Vector3.Dot(c,right)/width+.5f,c.y/height+.5f);
                    if(Mathf.Max(pa.x,pb.x,pc.x)<0 || Mathf.Min(pa.x,pb.x,pc.x)>1 || Mathf.Max(pa.y,pb.y,pc.y)<0 || Mathf.Min(pa.y,pb.y,pc.y)>1)continue;
                    var polygon=new List<Vector3>{Vector3.right,Vector3.up,Vector3.forward};
                    Vector2 UV(Vector3 w)=>pa*w.x+pb*w.y+pc*w.z;
                    bool needsClip=pa.x<0||pa.x>1||pa.y<0||pa.y>1||pb.x<0||pb.x>1||pb.y<0||pb.y>1||pc.x<0||pc.x>1||pc.y<0||pc.y>1;
                    for(int plane=0;needsClip && plane<4 && polygon.Count>0;plane++)
                    {
                        float Distance(Vector3 w){Vector2 v=UV(w);return plane==0?v.x:plane==1?1-v.x:plane==2?v.y:1-v.y;}
                        var clipped=new List<Vector3>();Vector3 prev=polygon[polygon.Count-1];float dp=Distance(prev);
                        foreach(var curr in polygon)
                        {
                            float dc=Distance(curr);
                            if((dc>=0)!=(dp>=0))clipped.Add(Vector3.LerpUnclamped(prev,curr,dp/(dp-dc)));
                            if(dc>=0)clipped.Add(curr);prev=curr;dp=dc;
                        }
                        polygon=clipped;
                    }
                    if(polygon.Count<3)continue;
                    var polygonIndices=new List<int>();
                    foreach(var w in polygon)
                    {
                        int original=w.x>.99999f?ia:w.y>.99999f?ib:w.z>.99999f?ic:-1;
                        if(original>=0 && reused.TryGetValue(original,out int existing)){polygonIndices.Add(existing);continue;}
                        int vertex=points.Count;polygonIndices.Add(vertex);if(original>=0)reused[original]=vertex;
                        Vector3 normal=bindNormals.Length==bind.Length?(bindNormals[ia]*w.x+bindNormals[ib]*w.y+bindNormals[ic]*w.z).normalized:Vector3.Cross(bind[ib]-bind[ia],bind[ic]-bind[ia]).normalized;
                        points.Add(bind[ia]*w.x+bind[ib]*w.y+bind[ic]*w.z+normal*offset);normals.Add(normal);uv.Add(UV(w));
                        if(skin && weights.Length==bind.Length)skinWeights.Add(BlendWeights(weights[ia],weights[ib],weights[ic],w));
                    }
                    for(int i=1;i<polygonIndices.Count-1;i++){indices.Add(polygonIndices[0]);indices.Add(polygonIndices[i]);indices.Add(polygonIndices[i+1]);}
                }
                if(indices.Count==0)return;
                var mesh=new Mesh{name=name+"_Surface"};mesh.SetVertices(points);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);mesh.RecalculateBounds();owned.Add(mesh);
                var go=new GameObject(name);go.transform.SetParent(container,false);Renderer renderer;
                if(skin)
                {
                    mesh.bindposes=source.bindposes;mesh.boneWeights=skinWeights.ToArray();
                    var sr=go.AddComponent<SkinnedMeshRenderer>();sr.sharedMesh=mesh;sr.bones=skin.bones;sr.rootBone=skin.rootBone;sr.localBounds=skin.localBounds;sr.updateWhenOffscreen=skin.updateWhenOffscreen;renderer=sr;
                }
                else{go.AddComponent<MeshFilter>().sharedMesh=mesh;renderer=go.AddComponent<MeshRenderer>();}
                var mat=new Material(shader){name=name+"_Ink",mainTexture=texture,color=tint};owned.Add(mat);renderer.sharedMaterial=mat;
                renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.lightProbeUsage=LightProbeUsage.Off;
            }
        }
        private static BoneWeight BlendWeights(BoneWeight a,BoneWeight b,BoneWeight c,Vector3 bary)
        {
            if(bary.x>.99999f)return a;if(bary.y>.99999f)return b;if(bary.z>.99999f)return c;
            var map=new Dictionary<int,float>(12);
            void Add(BoneWeight w,float f)
            {
                void One(int bone,float value){if(value>0)map[bone]=(map.TryGetValue(bone,out float prior)?prior:0)+value*f;}
                One(w.boneIndex0,w.weight0);One(w.boneIndex1,w.weight1);One(w.boneIndex2,w.weight2);One(w.boneIndex3,w.weight3);
            }
            Add(a,bary.x);Add(b,bary.y);Add(c,bary.z);
            var sorted=new List<KeyValuePair<int,float>>(map);sorted.Sort((x,y)=>y.Value.CompareTo(x.Value));
            var r=new BoneWeight();float sum=0;for(int i=0;i<Mathf.Min(4,sorted.Count);i++)sum+=sorted[i].Value;if(sum<=0)return r;
            for(int i=0;i<Mathf.Min(4,sorted.Count);i++)
            {
                int bone=sorted[i].Key;float w=sorted[i].Value/sum;
                if(i==0){r.boneIndex0=bone;r.weight0=w;}else if(i==1){r.boneIndex1=bone;r.weight1=w;}else if(i==2){r.boneIndex2=bone;r.weight2=w;}else{r.boneIndex3=bone;r.weight3=w;}
            }
            return r;
        }
        public static Texture2D CreateInkTexture(int variant)
        {
            const int size=256;var tex=new Texture2D(size,size,TextureFormat.RGBA32,true){name="Ink_"+variant,wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Trilinear};var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float u=(x+.5f)/size-.5f,v=(y+.5f)/size-.5f;
                float Ellipse(float cx,float cy,float rx,float ry)=>1-Mathf.Sqrt(Mathf.Pow((u-cx)/rx,2)+Mathf.Pow((v-cy)/ry,2));
                float field=Ellipse(-.025f,.09f,.30f,.245f);
                field=Mathf.Max(field,Ellipse(-.205f,.13f+variant*.025f,.17f,.14f));field=Mathf.Max(field,Ellipse(.17f,.04f-variant*.025f,.195f,.20f));field=Mathf.Max(field,Ellipse(.065f,.25f,.14f,.115f));
                bool drip=false;
                for(int d=0;d<variant+1;d++)
                {
                    float cx=-.17f+d*.145f+variant*.017f,bottom=-.43f+(d%2)*.085f,t=Mathf.Clamp01((-.04f-v)/(-.04f-bottom));
                    float line=cx+.014f*Mathf.Sin(t*5+variant+d),radius=Mathf.Lerp(.052f,.023f,t);
                    float f=Mathf.Min((radius-Mathf.Abs(u-line))/.11f,Mathf.Min((v-bottom)/.07f,(-.035f-v)/.07f));
                    f=Mathf.Max(f,Ellipse(cx+.014f*Mathf.Sin(5+variant+d),bottom+.014f,.028f,.035f));if(f>field){field=f;drip=true;}
                }
                field+=(Mathf.PerlinNoise(u*27+variant*9+30,v*29+20)-.5f)*.10f;
                float alpha=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.02f,.018f,field));
                float rim=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.015f,.16f,field));
                Color color=Color.Lerp(new Color(.032f,.008f,.049f,1),new Color(.36f,.20f,.43f,1),Mathf.Max(rim,drip?.42f:0));
                float wet=Mathf.Exp(-Mathf.Pow((field-.055f)/.022f,2))*.30f;
                color=Color.Lerp(color,new Color(.51f,.34f,.57f,1),wet);color.a=alpha;pixels[y*size+x]=color;
            }
            tex.SetPixels(pixels);tex.Apply(true,false);return tex;
        }
        private static void Release(UnityEngine.Object obj){if(!obj)return;if(Application.isPlaying)Destroy(obj);else DestroyImmediate(obj);}
        private void OnDestroy(){foreach(var obj in owned)Release(obj);if(container)Release(container.gameObject);}
    }
}
