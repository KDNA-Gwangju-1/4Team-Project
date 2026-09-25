using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BrightDream.Combat
{
    /// <summary>
    /// 보스 약점 노출과 명중 진행도를 관리한다. 평소엔 무적이라 정화총이 통하지 않고,
    /// 정화 대상 몬스터를 일정 마리 수(purifyCountToOpen) 정화할 때마다 약점이 짧게 열려
    /// 그 순간에만 정화총이 보스 본체에 통한다.
    /// 노출 중에 정화한 몬스터는 다음 카운트에 포함되지 않고, 창이 닫힌 시점부터 다시 0부터 센다.
    /// 노출 구간 하나당 명중 진행도는 최대 1회만 인정된다 (정확도가 아니라 "구간을 놓치지 않았는가"가 기준).
    /// </summary>
    [RequireComponent(typeof(Collider), typeof(Rigidbody))]
    public class BossWeakpointController : MonoBehaviour
    {
        public static BossWeakpointController Instance { get; private set; }

        /// <summary>보스가 목표 명중 횟수를 채워 처치된 순간 1회 발생. BossArenaLockdown이 구독해 아레나를 연다.</summary>
        public static event Action OnBossDefeated;

        [SerializeField] private int purifyCountToOpen = 3;
        [Tooltip("약점이 열려 있는 시간. 너무 짧으면 조준할 틈이 없다.")]
        [SerializeField] private float windowDuration = 3.5f;
        [SerializeField] private int hitsToDefeat = 6;
        [Tooltip("약점 노출 시 켜줄 오브젝트(뿔 주변 검은 반점 등 전용 비주얼이 생기면 연결). 비워두면 아래 색 변화만으로 표시한다.")]
        [SerializeField] private GameObject weakpointVisual;
        [Tooltip("전용 비주얼(검은 반점 등)이 아직 없어서 임시로 넣은 노출 표시 - 노출되는 동안 보스 본체를 이 색으로 물들인다.")]
        [SerializeField] private Color exposedTint = new Color(0.25f, 0.05f, 0.35f, 1f);
        [SerializeField] private Renderer[] bodyRenderers;
        [Tooltip("화면 중앙 상단에 보스 정화(약점 명중) 진행도를 보여줄 텍스트.")]
        [SerializeField] private Text progressText;
        [Tooltip("보스 정화 텍스트 위에 - 다음 약점이 열리기까지 정화한 마릿수를 보여줄 텍스트.")]
        [SerializeField] private Text weakpointGaugeText;
        [Tooltip("StageProgressManager가 이 스테이지에 도달하면(= Boss Stage) 진행도 UI를 켠다.")]
        [SerializeField] private int stageIndex = 3;

        public bool IsExposed { get; private set; }
        public int HitProgress { get; private set; }

        private int purifiedSinceWindowClosed;
        private bool hitRegisteredThisWindow;
        private bool isDefeated;
        private Coroutine autoCloseRoutine;
        private Color[] originalColors;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // 정화총 총알(PurifierProjectile)도 Rigidbody 없이 트리거 콜라이더만 가지고 있어서,
            // 이쪽 중 하나는 Rigidbody가 있어야 OnTriggerEnter가 발생한다 - MonsterCombat과 같은 이유.
            Rigidbody rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            if (weakpointVisual != null) weakpointVisual.SetActive(false);

            if (bodyRenderers == null || bodyRenderers.Length == 0)
                bodyRenderers = GetComponentsInChildren<Renderer>(true);
            originalColors = new Color[bodyRenderers.Length];
            for (int i = 0; i < bodyRenderers.Length; i++)
                if (bodyRenderers[i] != null) originalColors[i] = ReadColor(bodyRenderers[i].material);

            ChapterObjectiveStyle.Apply(progressText, ChapterDialogueSkin.Theme.BrightDream, 2);
            ChapterObjectiveStyle.Apply(weakpointGaugeText, ChapterDialogueSkin.Theme.BrightDream, 3);
            UpdateProgressUI();
            UpdateGaugeUI();
            if (progressText != null) progressText.gameObject.SetActive(false);
            if (weakpointGaugeText != null) weakpointGaugeText.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            StageProgressManager.OnStageChanged += HandleStageChanged;
        }

        private void OnDisable()
        {
            StageProgressManager.OnStageChanged -= HandleStageChanged;
        }

        private void HandleStageChanged(int currentStage)
        {
            if (currentStage != stageIndex) return;
            if (progressText != null) progressText.gameObject.SetActive(true);
            if (weakpointGaugeText != null) weakpointGaugeText.gameObject.SetActive(true);
        }

        private void UpdateProgressUI()
        {
            if (progressText != null) progressText.text = $"<b>보스 정화</b>   <color=#3E86B0><b>{HitProgress} / {hitsToDefeat}</b></color>";
        }

        /// <summary>다음 약점이 열리기까지 (노출 중이 아닐 때) 정화한 마릿수 - purifyCountToOpen에 도달하면 약점이 열린다.</summary>
        private void UpdateGaugeUI()
        {
            if (weakpointGaugeText != null) weakpointGaugeText.text = $"<b>약점 게이지</b>   <color=#3E86B0><b>{purifiedSinceWindowClosed} / {purifyCountToOpen}</b></color>\n<color=#6B7C88>인형을 정화하면 차오름</color>";
        }

        /// <summary>정화 대상 몬스터가 (약점이 닫힌 상태에서) 정화될 때마다 BossArenaMonsterSpawner가 호출한다.
        /// 공격 패턴 도중이어도 그대로 열린다 - 패턴과 노출은 서로 독립적이라 겹칠 수 있다.</summary>
        public void RegisterMonsterPurified()
        {
            if (isDefeated || IsExposed) return;

            purifiedSinceWindowClosed++;
            UpdateGaugeUI();
            if (purifiedSinceWindowClosed >= purifyCountToOpen) OpenWindow();
        }

        private void OpenWindow()
        {
            purifiedSinceWindowClosed = 0;
            UpdateGaugeUI();
            IsExposed = true;
            GameSfx.Play("Weakpoint", .6f);
            hitRegisteredThisWindow = false;
            if (weakpointVisual != null) weakpointVisual.SetActive(true);
            // The entire body is the valid target. Restore its unmistakable exposed tint.
            SetTint(exposedTint);
            if (weakpointGaugeText != null) weakpointGaugeText.text = "<b>약점 노출</b>   <color=#258B80>지금 보스 본체를 정화하세요</color>";
            autoCloseRoutine = StartCoroutine(AutoCloseAfterDelay());
        }

        private IEnumerator AutoCloseAfterDelay()
        {
            yield return new WaitForSeconds(windowDuration);
            CloseWindow();
        }

        private void CloseWindow()
        {
            IsExposed = false;
            autoCloseRoutine = null;
            if (weakpointVisual != null) weakpointVisual.SetActive(false);
            RestoreTint();
            UpdateGaugeUI();
        }

        /// <summary>전용 약점 비주얼이 생기기 전까지 쓰는 임시 표시 - 노출/평시 색을 본체 머티리얼에 직접 칠한다.</summary>
        private void SetTint(Color color)
        {
            foreach (Renderer r in bodyRenderers) if (r != null) WriteColor(r.material, color);
        }

        private void RestoreTint()
        {
            for (int i = 0; i < bodyRenderers.Length; i++)
                if (bodyRenderers[i] != null) WriteColor(bodyRenderers[i].material, originalColors[i]);
        }

        private static Color ReadColor(Material m)
        {
            if (m.HasProperty("_BaseColor")) return m.GetColor("_BaseColor");
            if (m.HasProperty("_Color")) return m.color;
            return Color.white;
        }

        private static void WriteColor(Material m, Color color)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            else if (m.HasProperty("_Color")) m.color = color;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isDefeated) return;

            PurifierProjectile bullet = other.GetComponentInParent<PurifierProjectile>();
            if (bullet == null) return;
            if (!bullet.TryConsume()) return; // 이미 다른 대상을 맞힌 총알 - 보스는 못 맞는다.

            if (!IsExposed || hitRegisteredThisWindow) return;

            hitRegisteredThisWindow = true;
            GameSfx.Play("Purify", .6f);
            HitProgress++;
            UpdateProgressUI();
            if (weakpointGaugeText != null) weakpointGaugeText.text = "<b>정화 성공</b>   다음 노출을 준비하세요";
            if (HitProgress >= hitsToDefeat) Defeat();
        }

        /// <summary>
        /// 테스트용 즉시 처치. 명중 진행도를 채운 것과 같은 경로(Defeat)를 그대로 타므로
        /// 아레나 개방·클리어 메시지·균열 연출이 실제 플레이와 똑같이 이어진다.
        /// </summary>
        public void DebugDefeat()
        {
            if (isDefeated) return;
            HitProgress = hitsToDefeat;
            UpdateProgressUI();
            Defeat();
        }

        private void Defeat()
        {
            isDefeated = true;
            GameSfx.Play("Victory", .5f);
            CloseWindow();
            if (progressText != null) progressText.gameObject.SetActive(false);
            if (weakpointGaugeText != null) weakpointGaugeText.gameObject.SetActive(false);
            OnBossDefeated?.Invoke();
        }
    }
}
