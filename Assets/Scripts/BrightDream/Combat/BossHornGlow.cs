using UnityEngine;

namespace BrightDream.Combat
{
    /// <summary>
    /// 유니콘 뿔 끝(Head 본 아래 HornTip)에 붙는 발광. 패턴마다 느낌을 다르게 준다.
    ///   별똥별: 따뜻한 금빛/분홍빛이 반짝이고, 작은 별가루가 위로 흩날린다 (하늘로 별을 부르는 느낌).
    ///   레이저: 보라/시안빛이 뿔 끝으로 빨려들며 모이고, 충전(SetCharge)될수록 커지고 밝아지며 떨린다.
    /// Head 본의 자식이라 애니메이션(포효로 머리를 치켜들기, 뿔 겨누기)을 그대로 따라간다.
    /// </summary>
    public class BossHornGlow : MonoBehaviour
    {
        public enum Mode { Off, Starfall, Laser }

        [Tooltip("가산(Additive) 파티클 머티리얼 - 하얗게 타는 발광 코어.")]
        [SerializeField] private Material glowMaterial;
        [Tooltip("알파 블렌드 파티클 머티리얼 - 코어 뒤의 큰 후광과 입자. 밝은 낮 하늘 위에서는 가산만으로는 묻혀서, 채도 높은 색을 덮어 준다.")]
        [SerializeField] private Material haloMaterial;
        [SerializeField] private float lightRange = 6f;
        [Tooltip("후광 크기 = 코어 크기 x 이 값.")]
        [SerializeField] private float haloScale = 2.3f;

        [Header("별똥별 - 금빛 반짝임, 위로 흩날리는 별가루")]
        [SerializeField] private Color starCoreColor = new Color(1f, 0.86f, 0.5f, 1f);
        [SerializeField] private Color starHaloColor = new Color(1f, 0.72f, 0.18f, 0.8f);
        [SerializeField] private Color starSparkColor = new Color(1f, 0.55f, 0.8f, 1f);
        [SerializeField] private float starCoreSize = 0.9f;
        [SerializeField] private float starLightIntensity = 3f;

        [Header("레이저 - 보라/시안, 빨려드는 빛, 충전될수록 밝아짐")]
        [SerializeField] private Color laserCoreColor = new Color(0.72f, 0.42f, 1f, 1f);
        [SerializeField] private Color laserChargedColor = new Color(0.55f, 1f, 1f, 1f);
        [SerializeField] private Color laserHaloColor = new Color(0.55f, 0.15f, 1f, 0.85f);
        [SerializeField] private Vector2 laserCoreSize = new Vector2(0.35f, 1.2f);
        [SerializeField] private Vector2 laserLightIntensity = new Vector2(1.2f, 4f);

        private static readonly int TintId = Shader.PropertyToID("_TintColor");

        private Light glowLight;
        private Transform core, halo;
        private Renderer coreRenderer, haloRenderer;
        private MaterialPropertyBlock mpb;
        private ParticleSystem sparks;
        private Mode mode = Mode.Off;
        private float charge;
        private float fade;
        private float lastCoreSize, lastIntensity;
        private Color lastColor = Color.white, lastHaloColor = Color.white;

        public void SetMode(Mode value)
        {
            if (value == Mode.Off && glowLight == null) return; // 켜진 적이 없으면 끌 것도 없다 (종료 중 오브젝트 생성 방지)
            Build();
            if (value == mode) return;
            mode = value;
            charge = 0f;
            ConfigureSparks();
        }

        /// <summary>레이저 충전도 0~1. 별똥별 모드에서는 쓰지 않는다.</summary>
        public void SetCharge(float value) => charge = Mathf.Clamp01(value);

        private void Awake() => Build();

        private void Build()
        {
            if (glowLight != null) return;

            glowLight = new GameObject("HornGlowLight").AddComponent<Light>();
            glowLight.transform.SetParent(transform, false);
            glowLight.type = LightType.Point;
            glowLight.range = lightRange;
            glowLight.shadows = LightShadows.None;
            glowLight.intensity = 0f;
            glowLight.enabled = false;

            // 코어/후광은 카메라를 향하는 판 한 장씩 - 크기와 색을 매 프레임 직접 조절하기 쉽다.
            // 후광을 먼저 만들어 코어가 그 위에 그려지게 한다.
            halo = MakeBillboard("HornGlowHalo", haloMaterial, out haloRenderer);
            core = MakeBillboard("HornGlowCore", glowMaterial, out coreRenderer);
            mpb = new MaterialPropertyBlock();

            var sparkGo = new GameObject("HornGlowSparks");
            sparkGo.transform.SetParent(transform, false);
            sparks = sparkGo.AddComponent<ParticleSystem>();
            sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var psr = sparkGo.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial = haloMaterial != null ? haloMaterial : glowMaterial;
            psr.renderMode = ParticleSystemRenderMode.Billboard;
            psr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private void ConfigureSparks()
        {
            sparks.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (mode == Mode.Off) return;

            var main = sparks.main;
            main.loop = true;
            main.playOnAwake = false;
            main.maxParticles = 120;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            var shape = sparks.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            var col = sparks.colorOverLifetime;
            col.enabled = true;
            var size = sparks.sizeOverLifetime;
            size.enabled = true;
            var vel = sparks.velocityOverLifetime;

            if (mode == Mode.Starfall)
            {
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.4f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.26f);
                main.startColor = new ParticleSystem.MinMaxGradient(starHaloColor, starSparkColor);
                main.gravityModifier = -0.12f; // 위로 떠오르며 흩날린다
                shape.radius = 0.15f;
                shape.radiusThickness = 1f;
                vel.enabled = true;
                vel.space = ParticleSystemSimulationSpace.World;
                vel.y = new ParticleSystem.MinMaxCurve(0.5f);
                vel.x = new ParticleSystem.MinMaxCurve(0f);
                vel.z = new ParticleSystem.MinMaxCurve(0f);
                col.color = Fade(new Color(1f, 1f, 1f, 1f), 0.15f, 0.6f);
                // 반짝임 - 커졌다 작아지기를 두 번
                size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                    new Keyframe(0f, 0.2f), new Keyframe(0.25f, 1f), new Keyframe(0.45f, 0.4f), new Keyframe(0.7f, 0.9f), new Keyframe(1f, 0f)));
                var emission = sparks.emission;
                emission.rateOverTime = 36f;
            }
            else
            {
                main.simulationSpace = ParticleSystemSimulationSpace.Local; // 머리가 움직여도 뿔 끝으로 모인다
                main.startLifetime = 0.45f;
                main.startSpeed = -2.1f; // 껍질에서 태어나 중심(뿔 끝)으로 빨려든다
                main.startSize = new ParticleSystem.MinMaxCurve(0.09f, 0.18f);
                main.startColor = new ParticleSystem.MinMaxGradient(laserHaloColor, laserChargedColor);
                main.gravityModifier = 0f;
                shape.radius = 0.9f;
                shape.radiusThickness = 0f;
                vel.enabled = false;
                col.color = Fade(new Color(1f, 1f, 1f, 1f), 0.5f, 1f);
                size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1.2f), new Keyframe(1f, 0.3f)));
            }
            sparks.Play();
        }

        private static ParticleSystem.MinMaxGradient Fade(Color c, float fadeInEnd, float fadeOutStart)
        {
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, fadeInEnd), new GradientAlphaKey(1f, fadeOutStart), new GradientAlphaKey(0f, 1f) });
            return new ParticleSystem.MinMaxGradient(g);
        }

        private void Update()
        {
            if (glowLight == null) return;
            fade = Mathf.MoveTowards(fade, mode == Mode.Off ? 0f : 1f, Time.deltaTime * 4f);

            float t = Time.time;
            float coreSize = 0f, intensity = 0f;
            Color color = Color.white, haloColor = Color.white;
            if (mode == Mode.Starfall)
            {
                // 별처럼 반짝인다 - 두 박자가 겹친 깜빡임.
                float twinkle = 0.75f + 0.25f * Mathf.Sin(t * 9f) + 0.12f * Mathf.Sin(t * 23f);
                coreSize = starCoreSize * twinkle;
                intensity = starLightIntensity * twinkle;
                color = starCoreColor;
                haloColor = starHaloColor;
            }
            else if (mode == Mode.Laser)
            {
                // 불안정하게 떨리며, 충전될수록 커지고 보라에서 시안으로 바뀐다.
                float flicker = 0.85f + 0.3f * (Mathf.PerlinNoise(t * 18f, 0.5f) - 0.5f);
                coreSize = Mathf.Lerp(laserCoreSize.x, laserCoreSize.y, charge) * flicker;
                intensity = Mathf.Lerp(laserLightIntensity.x, laserLightIntensity.y, charge) * flicker;
                color = Color.Lerp(laserCoreColor, laserChargedColor, charge * 0.6f);
                haloColor = Color.Lerp(laserHaloColor, new Color(laserChargedColor.r, laserChargedColor.g, laserChargedColor.b, laserHaloColor.a), charge * 0.5f);
                var emission = sparks.emission;
                emission.rateOverTime = Mathf.Lerp(20f, 90f, charge);
            }
            else
            {
                // 꺼지는 중 - 마지막 크기/색을 유지한 채 흐려진다.
                coreSize = lastCoreSize;
                intensity = lastIntensity;
                color = lastColor;
                haloColor = lastHaloColor;
            }
            if (mode != Mode.Off)
            {
                lastCoreSize = coreSize;
                lastIntensity = intensity;
                lastColor = color;
                lastHaloColor = haloColor;
            }

            glowLight.enabled = fade > 0.01f;
            glowLight.color = color;
            glowLight.intensity = intensity * fade;

            ApplyBillboard(core, coreRenderer, coreSize * fade, new Color(color.r, color.g, color.b, fade));
            ApplyBillboard(halo, haloRenderer, coreSize * haloScale * fade, new Color(haloColor.r, haloColor.g, haloColor.b, haloColor.a * fade));
        }

        private Transform MakeBillboard(string name, Material material, out Renderer renderer)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.enabled = false;
            return go.transform;
        }

        private void ApplyBillboard(Transform quad, Renderer renderer, float worldSize, Color tint)
        {
            renderer.enabled = fade > 0.01f && renderer.sharedMaterial != null;
            if (!renderer.enabled) return;
            Vector3 parentScale = transform.lossyScale;
            quad.localScale = new Vector3(worldSize / Mathf.Max(parentScale.x, 0.0001f), worldSize / Mathf.Max(parentScale.y, 0.0001f), 1f);
            renderer.GetPropertyBlock(mpb);
            mpb.SetColor(TintId, tint);
            renderer.SetPropertyBlock(mpb);
        }

        private void LateUpdate()
        {
            Camera cam = Camera.main;
            if (cam == null || core == null) return;
            if (coreRenderer.enabled) core.rotation = Quaternion.LookRotation(core.position - cam.transform.position, cam.transform.up);
            if (haloRenderer.enabled) halo.rotation = Quaternion.LookRotation(halo.position - cam.transform.position, cam.transform.up);
        }
    }
}
