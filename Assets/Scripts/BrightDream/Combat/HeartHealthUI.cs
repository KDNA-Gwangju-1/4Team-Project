using UnityEngine;
using UnityEngine.UI;

namespace BrightDream.Combat
{
    /// <summary>
    /// 기존 Health Bar(Slider/Fill Image + Text)를 대신해 하트 5개로 HP를 표시한다.
    /// 하트 1개 = 20 HP. PlayerHealth의 기존 OnHealthChanged(비율 0~1) 이벤트만 구독하고,
    /// PlayerHealth 자체는 전혀 수정하지 않는다.
    /// 각 하트는 Empty(배경) 위에 Filled(Image, Type=Filled, Horizontal) 를 겹쳐 놓고
    /// fillAmount 로 부분 채움을 표현한다 - heartFills 배열에는 Filled Image만 연결하면 된다.
    /// </summary>
    public class HeartHealthUI : MonoBehaviour
    {
        [Tooltip("왼쪽부터 순서대로 5개 - 각 하트의 'Filled' Image(Image Type=Filled, Fill Method=Horizontal).")]
        [SerializeField] private Image[] heartFills = new Image[5];

        private const int HeartCount = 5;
        private const float HpPerHeart = 20f;

        // PlayerHealth 의 OnHealthChanged 는 인스턴스 이벤트라 PlayerHealth.Instance 가
        // 아직 없을 때(스크립트 실행 순서 문제 - 이 프로젝트의 다른 스크립트들도 같은 이유로
        // 매 프레임 재확인하는 패턴을 쓴다) 구독을 걸어 둘 수 없다. 그래서 Instance 가 처음
        // 나타나는 프레임에 한 번만 구독한다.
        private PlayerHealth subscribedTo;

        private void OnDisable()
        {
            if (subscribedTo != null)
            {
                subscribedTo.OnHealthChanged -= ApplyRatio;
                subscribedTo = null;
            }
        }

        private void Update()
        {
            if (subscribedTo != null) return;
            if (PlayerHealth.Instance == null) return;

            subscribedTo = PlayerHealth.Instance;
            subscribedTo.OnHealthChanged += ApplyRatio;
            // 최초 구독 시점의 현재 HP로 즉시 동기화 (이벤트는 '변화'에만 발생하므로).
            ApplyRatio(subscribedTo.CurrentHealth / (HeartCount * HpPerHeart));
        }

        private void ApplyRatio(float ratio)
        {
            float heartsFilled = Mathf.Clamp01(ratio) * HeartCount;
            for (int i = 0; i < heartFills.Length; i++)
            {
                if (heartFills[i] == null) continue;
                heartFills[i].fillAmount = Mathf.Clamp01(heartsFilled - i);
            }
        }
    }
}
