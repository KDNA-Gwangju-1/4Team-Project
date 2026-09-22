using System.Collections;
using UnityEngine;

namespace BrightDream.Combat
{
    /// <summary>
    /// 균열로 빨려 들어갈 때 화면 전체를 일렁이게 한다.
    ///
    /// 붙이는 곳 : Main Camera. Built-in RP 의 OnRenderImage 로 화면을 한 번 더 거쳐
    /// 보내므로 카메라에 붙어 있어야 한다.
    ///
    /// 시간은 unscaled 로 돈다. 대사나 게임오버로 Time.timeScale 이 0 이 되면
    /// 연출이 그 자리에서 멈춰 버린다. BossRiftEntrance / BossRiftExit 과 같은 규칙.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public class ScreenWarpTransition : MonoBehaviour
    {
        [Tooltip("Hidden/BrightDream/ScreenWarp. 비우면 이름으로 찾아본다.")]
        [SerializeField] private Shader warpShader;
        [Tooltip("일렁일 때 화면에 도는 색.")]
        [SerializeField] private Color tint = new Color(0.55f, 0.30f, 0.95f, 1f);

        [Header("들어가는 느낌")]
        [Tooltip("연출이 끝날 때까지 시야각을 이만큼 넓힌다. 0 이면 쓰지 않는다.")]
        [SerializeField] private float fovPush = 10f;
        [Tooltip("최대 세기에 도달하는 데 쓰는 시간 비율. 0.7 이면 70% 지점에서 최대가 된다.")]
        [Range(0.1f, 1f)]
        [SerializeField] private float rampPortion = 0.7f;

        public bool IsPlaying { get; private set; }

        private Camera cam;
        private Material material;
        private float strength;
        private float baseFov;

        private static readonly int StrengthId = Shader.PropertyToID("_Strength");
        private static readonly int TintId = Shader.PropertyToID("_Tint");

        private void Awake()
        {
            cam = GetComponent<Camera>();

            var shader = warpShader != null ? warpShader : Shader.Find("Hidden/BrightDream/ScreenWarp");
            if (shader == null || !shader.isSupported)
            {
                Debug.LogWarning("[ScreenWarpTransition] 화면 일렁임 셰이더를 못 찾아 효과 없이 지나갑니다.", this);
                return;
            }

            // 머티리얼을 에셋으로 두지 않고 런타임에 만든다. 에디터에서 플레이할 때
            // 머티리얼 파일이 실제로 변경되는 것을 막는다.
            material = new Material(shader);
            material.hideFlags = HideFlags.HideAndDontSave;
        }

        private void OnDestroy()
        {
            if (material != null) DestroyImmediate(material);
        }

        /// <summary>duration 초에 걸쳐 일렁임을 끌어올린다.</summary>
        public void Play(float duration)
        {
            StopAllCoroutines();
            StartCoroutine(Routine(duration));
        }

        private IEnumerator Routine(float duration)
        {
            IsPlaying = true;
            baseFov = cam != null ? cam.fieldOfView : 60f;

            float t = 0f;
            float ramp = Mathf.Max(duration * rampPortion, 0.0001f);
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / ramp);
                strength = p * p * (3f - 2f * p);          // smoothstep

                if (cam != null && fovPush != 0f)
                    cam.fieldOfView = baseFov + fovPush * strength;

                yield return null;
            }

            strength = 1f;
            IsPlaying = false;
        }

        /// <summary>일렁임을 즉시 걷어 낸다.</summary>
        public void Clear()
        {
            StopAllCoroutines();
            strength = 0f;
            IsPlaying = false;
            if (cam != null && fovPush != 0f && baseFov > 0f) cam.fieldOfView = baseFov;
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            if (material == null || strength <= 0.0001f)
            {
                Graphics.Blit(src, dst);
                return;
            }

            material.SetFloat(StrengthId, strength);
            material.SetColor(TintId, tint);
            Graphics.Blit(src, dst, material);
        }
    }
}
