using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class UiButtonSound : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    private Button button;
    private void Awake() { button = GetComponent<Button>(); button.onClick.AddListener(Click); }
    private void OnDestroy() { if (button != null) button.onClick.RemoveListener(Click); }
    private void Click() { GameSfx.Play("UiClick", .32f, true); }
    private void Hover() { if (button != null && button.IsInteractable()) GameSfx.Play("UiHover", .14f, true); }
    public void OnPointerEnter(PointerEventData data) { Hover(); }
    public void OnSelect(BaseEventData data) { Hover(); }
}
