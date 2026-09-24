using UnityEngine;
using UnityEngine.UI;

namespace BrightDream.Combat
{
    /// <summary>
    /// 기존 Health Bar(Slider/Fill Image + Text)를 대신해 하트로 HP를 표시한다.
    /// 하트는 항상 heartsShown(5)개이고, 현재 HP / 최대 HP 비율만큼 채운다
    /// - 보스 스테이지에서 최대 체력이 100 -> 200이 되어도 하트는 5개 그대로, 한 칸이 40 HP를 맡는다.
    /// heartFills에는 10개가 연결되어 있지만 heartsShown 개만 켜고 나머지는 꺼 둔다.
    /// 각 하트는 Shadow/Empty(배경) 위에 Fill(Image, Type=Filled, Horizontal), 그 위에 Outline을 겹쳐 놓고
    /// fillAmount 로 부분 채움을 표현한다 - heartFills 배열에는 Fill Image만 연결하면 된다.
    ///
    /// 연출: 줄어든 칸은 곧장 뚝 떨어지지 않고 잠깐에 걸쳐 흘러내리듯 줄어들고, 피해를 입은 하트는 한 번
    /// 크게 튀어오른다(punch). 하트가 1개 이하로 남으면 마지막 하트가 심장박동처럼 계속 두근거린다.
    /// 대사 중에는 Time.timeScale이 0이 되므로 모든 연출은 unscaled time으로 돈다.
    /// </summary>
    public class HeartHealthUI : MonoBehaviour
    {
        [Tooltip("왼쪽부터 순서대로 최대 개수(10개) - 각 하트의 'Fill' Image(Image Type=Filled, Fill Method=Horizontal). " +
                 "실제로 몇 개가 보일지는 PlayerHealth.MaxHealth에 따라 정해진다.")]
        [SerializeField] private Image[] heartFills = new Image[10];

        [Header("연출")]
        [Tooltip("줄어든 하트가 실제 값까지 따라 내려오는 속도 (하트 칸/초).")]
        [SerializeField] private float drainSpeed = 3.5f;
        [Tooltip("피해를 입은 하트가 순간적으로 커지는 배율.")]
        [SerializeField] private float punchScale = 1.35f;
        [SerializeField] private float punchDuration = 0.28f;
        [Tooltip("남은 하트가 이 수치 이하이면 마지막 하트가 계속 두근거린다.")]
        [SerializeField] private float lowHealthHearts = 1f;
        [SerializeField] private float heartbeatScale = 1.12f;
        [SerializeField] private float heartbeatsPerSecond = 1.6f;

        [Tooltip("항상 보여 줄 하트 개수. 모든 챕터가 5개로 통일되어 있다 (BadDream 의 PlayerHeartsUI2D 와 같은 값). " +
                 "최대 체력이 바뀌어도(보스 스테이지 100 -> 200) 개수는 그대로고, 하트 한 칸이 맡는 양만 달라진다.")]
        [SerializeField] private int heartsShown = ChapterHudStyle.HeartCount;

        /// <summary>현재 표시 중인 하트 개수. PlayerHealth.MaxHealth가 바뀌면 따라 바뀐다.</summary>
        private int heartCount;

        // PlayerHealth 의 OnHealthChanged 는 인스턴스 이벤트라 PlayerHealth.Instance 가
        // 아직 없을 때(스크립트 실행 순서 문제 - 이 프로젝트의 다른 스크립트들도 같은 이유로
        // 매 프레임 재확인하는 패턴을 쓴다) 구독을 걸어 둘 수 없다. 그래서 Instance 가 처음
        // 나타나는 프레임에 한 번만 구독한다.
        private PlayerHealth subscribedTo;

        /// <summary>실제 HP가 가리키는 하트 칸 수 (0 ~ heartCount).</summary>
        private float targetHearts;
        /// <summary>화면에 그려지는 하트 칸 수 - targetHearts를 따라 부드럽게 내려온다.</summary>
        private float displayedHearts;
        private bool hasInitialValue;

        private Transform[] heartRoots;
        private float[] punchTimers;

        private void Awake()
        {
            ChapterHudStyle.TopLeft(transform as RectTransform, ChapterHudStyle.LeftColumnY(0));
            heartRoots = new Transform[heartFills.Length];
            punchTimers = new float[heartFills.Length];
            for (int i = 0; i < heartFills.Length; i++)
            {
                if (heartFills[i] == null) continue;
                // Fill 만 키우면 테두리/그림자와 따로 놀아서, 하트 묶음(부모)을 통째로 키운다.
                Transform fill = heartFills[i].transform;
                heartRoots[i] = fill.parent != null ? fill.parent : fill;
                if (heartRoots[i] is RectTransform heart)
                {
                    heart.anchorMin = heart.anchorMax = new Vector2(0,1);
                    heart.pivot = new Vector2(.5f,.5f);
                    // 크기·간격은 ChapterHudStyle 공용 값 (2D PlayerHeartsUI2D 와 같다)
                    float s = ChapterHudStyle.HeartSize;
                    heart.sizeDelta = new Vector2(s, s);
                    heart.anchoredPosition = new Vector2(ChapterHudStyle.HeartLeft + s * .5f + i * ChapterHudStyle.HeartStep,
                                                         -(ChapterHudStyle.HeartTop + s * .5f));
                    foreach (var image in heart.GetComponentsInChildren<Image>(true))
                    {
                        if (image.transform == heart) continue;
                        image.rectTransform.anchorMin = Vector2.zero;
                        image.rectTransform.anchorMax = Vector2.one;
                        image.rectTransform.offsetMin = image.rectTransform.offsetMax = Vector2.zero;
                    }
                }
            }
        }

        private void OnDisable()
        {
            if (subscribedTo != null)
            {
                subscribedTo.OnHealthChanged -= HandleHealthChanged;
                subscribedTo = null;
            }
        }

        private void Update()
        {
            TrySubscribe();
            if (!hasInitialValue) return;

            RefreshHeartCount();

            float dt = Time.unscaledDeltaTime;
            displayedHearts = Mathf.MoveTowards(displayedHearts, targetHearts, drainSpeed * dt);
            ApplyFill(displayedHearts);
            ApplyScale(dt);
        }

        private void TrySubscribe()
        {
            if (subscribedTo != null || PlayerHealth.Instance == null) return;

            subscribedTo = PlayerHealth.Instance;
            subscribedTo.OnHealthChanged += HandleHealthChanged;
            RefreshHeartCount();
            // 최초 구독 시점의 현재 HP로 즉시 동기화 (이벤트는 '변화'에만 발생하므로).
            targetHearts = Mathf.Clamp01(subscribedTo.CurrentHealth / subscribedTo.MaxHealth) * heartCount;
            displayedHearts = targetHearts;
            hasInitialValue = true;
            ApplyFill(displayedHearts);
        }

        /// <summary>
        /// 최대 체력에 맞춰 보여줄 하트 개수를 다시 계산하고, 늘어난 만큼 하트 오브젝트를 켜 준다.
        /// 보스 스테이지 진입처럼 최대 체력이 바뀌는 순간에만 실제로 뭔가가 바뀐다.
        /// </summary>
        private void RefreshHeartCount()
        {
            int wanted = Mathf.Clamp(heartsShown, 1, heartFills.Length);
            if (wanted == heartCount) return;

            heartCount = wanted;
            ChapterHudStyle.Frame(transform as RectTransform, true, ChapterHudStyle.CardWidth, ChapterHudStyle.HeartCardHeight, "체력");
            for (int i = 0; i < heartFills.Length; i++)
            {
                if (heartRoots[i] == null) continue;
                heartRoots[i].gameObject.SetActive(i < heartCount);
            }
            // 하트가 늘어난 직후에는 표시값도 곧바로 새 최대치에 맞춰 둔다 (흘러내리는 연출 없이).
            targetHearts = Mathf.Clamp01(subscribedTo.CurrentHealth / subscribedTo.MaxHealth) * heartCount;
            displayedHearts = targetHearts;
            ApplyFill(displayedHearts);
        }

        private void HandleHealthChanged(float ratio)
        {
            float previous = targetHearts;
            targetHearts = Mathf.Clamp01(ratio) * heartCount;
            if (!hasInitialValue)
            {
                displayedHearts = targetHearts;
                hasInitialValue = true;
                ApplyFill(displayedHearts);
                return;
            }

            if (targetHearts < previous) PunchRange(targetHearts, previous);
        }

        /// <summary>이번에 깎여 나간 구간에 걸친 하트들을 한 번 튀어오르게 한다.</summary>
        private void PunchRange(float from, float to)
        {
            int first = Mathf.Clamp(Mathf.FloorToInt(from), 0, heartCount - 1);
            int last = Mathf.Clamp(Mathf.CeilToInt(to) - 1, 0, heartCount - 1);
            for (int i = first; i <= last && i < punchTimers.Length; i++) punchTimers[i] = punchDuration;
        }

        private void ApplyFill(float heartsFilled)
        {
            for (int i = 0; i < heartFills.Length; i++)
            {
                if (heartFills[i] == null) continue;
                heartFills[i].fillAmount = Mathf.Clamp01(heartsFilled - i);
            }
        }

        private void ApplyScale(float dt)
        {
            // 마지막 남은 하트만 두근거린다 - 어느 칸인지는 표시값 기준으로 매 프레임 다시 고른다.
            int beatingIndex = targetHearts > 0f && targetHearts <= lowHealthHearts
                ? Mathf.Clamp(Mathf.CeilToInt(displayedHearts) - 1, 0, heartCount - 1)
                : -1;
            float beat = 1f + (heartbeatScale - 1f) *
                         Mathf.Abs(Mathf.Sin(Time.unscaledTime * Mathf.PI * heartbeatsPerSecond));

            for (int i = 0; i < heartRoots.Length; i++)
            {
                if (heartRoots[i] == null) continue;

                float scale = i == beatingIndex ? beat : 1f;
                if (punchTimers[i] > 0f)
                {
                    punchTimers[i] = Mathf.Max(0f, punchTimers[i] - dt);
                    // 남은 시간 비율을 사인 한 번으로 태워 0 → 최대 → 0 으로 부풀렸다 가라앉힌다.
                    float t = 1f - punchTimers[i] / punchDuration;
                    scale = Mathf.Max(scale, 1f + (punchScale - 1f) * Mathf.Sin(t * Mathf.PI));
                }
                heartRoots[i].localScale = Vector3.one * scale;
            }
        }
    }
}
