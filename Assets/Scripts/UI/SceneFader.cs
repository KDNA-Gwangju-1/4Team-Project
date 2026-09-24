using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 화면을 어둡게 덮고 → 씬을 바꾸고 → 다시 밝히는 짧은 전환.
/// 로딩 화면까지는 필요 없는 가까운 전환(챕터2 스테이지 사이, 사망 후 재시도)에 쓴다.
///
///     SceneFader.LoadScene("BadDream_Stage3");
///
/// 덮개는 씬이 바뀌어도 살아 있어야 하므로 DontDestroyOnLoad 오브젝트 하나를 돌려 쓴다.
/// 시간은 unscaled 로 흘러서, 사망 화면처럼 timeScale 이 0 인 상태에서 불러도 멈추지 않는다.
/// </summary>
public sealed class SceneFader : MonoBehaviour
{
    public const float FadeOutTime = 0.4f;
    public const float FadeInTime = 0.55f;

    private static SceneFader instance;

    private CanvasGroup group;
    private bool busy;

    /// <summary>덮는 중이거나 걷는 중이면 true. 그동안 같은 요청이 겹치지 않게 막는다.</summary>
    public static bool IsFading => instance != null && instance.busy;

    public static void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        Ensure().Begin(() => SceneManager.LoadScene(sceneName));
    }

    public static void LoadScene(int buildIndex)
    {
        Ensure().Begin(() => SceneManager.LoadScene(buildIndex));
    }

    private static SceneFader Ensure()
    {
        if (instance != null) return instance;

        var go = new GameObject("SceneFader");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<SceneFader>();

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;            // 대사창·HUD·사망 화면보다 위
        go.AddComponent<GraphicRaycaster>();

        var cover = new GameObject("Cover", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
        cover.transform.SetParent(go.transform, false);
        cover.rectTransform.anchorMin = Vector2.zero;
        cover.rectTransform.anchorMax = Vector2.one;
        cover.rectTransform.offsetMin = cover.rectTransform.offsetMax = Vector2.zero;
        cover.color = new Color(0.02f, 0.015f, 0.04f, 1f);   // 악몽 톤의 거의 검은 남보라

        instance.group = go.AddComponent<CanvasGroup>();
        instance.group.alpha = 0f;
        instance.group.blocksRaycasts = false;
        instance.group.interactable = false;
        return instance;
    }

    private void Begin(System.Action load)
    {
        if (busy) return;
        StartCoroutine(Run(load));
    }

    private IEnumerator Run(System.Action load)
    {
        busy = true;
        group.blocksRaycasts = true;

        yield return Fade(group.alpha, 1f, FadeOutTime);

        // LoadScene 은 timeScale 을 되돌려 주지 않는다 (사망 화면은 0 으로 멈춰 둔다).
        // 화면이 다 가려진 지금 풀어야 멈춘 채로 다음 판이 시작되지 않는다.
        Time.timeScale = 1f;
        load();
        // LoadScene 은 이번 프레임 끝에 실제로 바뀐다. 새 씬의 Awake/Start 가 한 번 돈 뒤에 걷는다.
        yield return null;
        yield return null;

        yield return Fade(1f, 0f, FadeInTime);

        group.blocksRaycasts = false;
        busy = false;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / duration));
            yield return null;
        }
        group.alpha = to;
    }
}
