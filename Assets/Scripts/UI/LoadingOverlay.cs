using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 씬을 바꾸지 않고 로딩 화면만 잠깐 덮어 주는 연출.
///
/// 같은 씬 안에서 순간이동할 때 쓴다.
/// 씬을 새로 읽어 버리면 이미 지나온 대사나 상호작용 기록이 전부 초기화되기 때문에,
/// 겉보기만 로딩 화면이고 실제로는 화면만 덮었다 걷는다.
///
/// 붙이는 곳 : 로딩 그림이 들어 있는 UI 오브젝트 (CanvasGroup 필요)
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public class LoadingOverlay : MonoBehaviour
{
    [Header("타이밍 (초)")]
    [Tooltip("화면을 덮는 데 걸리는 시간")]
    [SerializeField] private float fadeInTime = 0.35f;

    [Tooltip("화면을 걷는 데 걸리는 시간")]
    [SerializeField] private float fadeOutTime = 0.5f;
    [SerializeField] private bool useDayHospitalArtwork = true;

    private CanvasGroup _group;

    /// <summary>지금 화면을 덮고 있는지</summary>
    public bool IsShowing { get; private set; }

    private void Awake()
    {
        // Fit the artwork without changing the full-screen fade/input area.
        var background = transform.Find("Background");
        if (background != null)
        {
            var image = background.GetComponent<Image>();
            if (image != null && useDayHospitalArtwork)
            {
                var day = Resources.Load<Sprite>("HospitalDayLoadingBackground");
                if (day != null) image.sprite = day;
            }
            LoadingScreen.FitBackground(image);
        }
        _group = GetComponent<CanvasGroup>();
        _group.alpha = 0f;
        _group.blocksRaycasts = false;
        _group.interactable = false;
    }

    /// <summary>
    /// 화면을 덮고 → 가려진 동안 onCovered 를 실행하고 → hold 초만큼 두었다가 걷는다.
    /// </summary>
    public void Play(float hold, Action onCovered)
    {
        // 문으로 이동할 때마다 다른 로딩 팁을 보여 준다.
        var background = transform.Find("Background");
        if (background != null) LoadingScreen.ShowTip(background.GetComponent<Image>());
        StopAllCoroutines();
        StartCoroutine(Routine(hold, onCovered));
    }

    private IEnumerator Routine(float hold, Action onCovered)
    {
        IsShowing = true;
        _group.blocksRaycasts = true;

        yield return Fade(0f, 1f, fadeInTime);

        // 완전히 가려진 뒤에 옮긴다. 그래야 순간이동이 안 보인다.
        if (onCovered != null) onCovered();

        if (hold > 0f) yield return new WaitForSecondsRealtime(hold);

        yield return Fade(1f, 0f, fadeOutTime);

        _group.blocksRaycasts = false;
        IsShowing = false;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (duration <= 0f) { _group.alpha = to; yield break; }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            _group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        _group.alpha = to;
    }
}
