using UnityEngine;
using BrightDream.Clues;

/// <summary>
/// 단서 4개를 모두 모으면 모습을 드러내는 정화총 픽업.
/// 드러난 뒤 플레이어가 걸어서 닿으면 자동으로 획득 처리된다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class WeaponPickup : MonoBehaviour
{
    [Tooltip("단서를 다 모으기 전까지 숨겨둘 실제 모델 렌더러. 비워두면 자식의 Renderer를 자동으로 찾는다.")]
    [SerializeField] private Renderer[] visualRenderers;
    [TextArea(1, 2)]
    [SerializeField] private string pickupMessage = "정화총을 획득했다!";
    [SerializeField] private float messageDuration = 4f;
    [Tooltip("획득 시 활성화할 플레이어 시점(카메라)의 손에 든 정화총 오브젝트.")]
    [SerializeField] private GameObject equippedWeaponVisual;

    /// <summary>다른 시스템(전투 전환 등)이 참조할 수 있는 최소한의 상태 플래그.</summary>
    public static bool PlayerHasWeapon { get; private set; }

    private Collider pickupCollider;
    private bool revealed;

    private void Awake()
    {
        // static이라 씬을 다시 불러와도(게임 오버 → 다시 시작) 값이 남는다. 픽업이 새로 생길 때 되돌린다.
        PlayerHasWeapon = false;

        if (visualRenderers == null || visualRenderers.Length == 0)
            visualRenderers = GetComponentsInChildren<Renderer>(true);

        pickupCollider = GetComponent<Collider>();
        pickupCollider.isTrigger = true;
        pickupCollider.enabled = false;
        SetVisualActive(false);
    }

    private void Update()
    {
        // ClueManager 초기화 순서에 의존하지 않도록 매 프레임 조건을 재확인한다 (오브젝트 하나뿐이라 비용 무시 가능).
        if (!revealed && ClueManager.Instance != null &&
            ClueManager.Instance.CollectedCount >= ClueManager.TotalClueCount)
        {
            Reveal();
        }
    }

    private void Reveal()
    {
        revealed = true;
        SetVisualActive(true);
        pickupCollider.enabled = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!revealed) return;
        if (other.GetComponentInParent<CharacterController>() == null) return;

        Grant(showMessage: true);
    }

    /// <summary>디버그 스테이지 스킵 등, 정상적인 픽업 동선을 건너뛰고 즉시 총을 지급할 때 쓴다.</summary>
    public void DebugGrant() => Grant(showMessage: false);

    private void Grant(bool showMessage)
    {
        if (PlayerHasWeapon) return;

        PlayerHasWeapon = true;
        revealed = false;
        if (equippedWeaponVisual != null) equippedWeaponVisual.SetActive(true);
        if (showMessage) StageMessageUI.Instance?.ShowMessage(pickupMessage, messageDuration);
        // 오브젝트 자체를 꺼서 시각/콜라이더를 한 번에 확실히 정리한다 (재상호작용 방지 포함).
        gameObject.SetActive(false);
    }

    private void SetVisualActive(bool active)
    {
        foreach (Renderer r in visualRenderers) if (r != null) r.enabled = active;
    }
}
