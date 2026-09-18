using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 로딩 화면.
///
/// 이 씬이 뜨면 다음 씬을 뒤에서 미리 읽어들이고, 다 되면 알아서 넘어간다.
///
/// 쓰는 법 — 어디서든 이렇게 부르면 된다.
/// <code>
///     LoadingScreen.Go("HospitalRoom");                  // 기본 배경으로
///     LoadingScreen.Go("Dream", dreamBackgroundSprite);  // 배경을 바꿔서
///     LoadingScreen.Go("", dreamBackgroundSprite);       // 갈 곳 없이 로딩 화면만
/// </code>
///
/// 배경 그림만 갈아 끼우는 방식이라, 전환 종류가 늘어나도 로딩 씬은 하나면 된다.
/// 달 아이콘과 표시 시간은 로딩 씬에 있는 값을 그대로 쓰므로 어디서 불러도 똑같이 나온다.
///
/// 주의 : 로딩 씬과 목적지 씬이 둘 다 Build Settings 에 들어가 있어야 한다.
/// </summary>
[DisallowMultipleComponent]
public class LoadingScreen : MonoBehaviour
{
    /// <summary>로딩 씬 이름. 씬 파일 이름을 바꾸면 여기도 같이 바꿔야 한다.</summary>
    public const string LoadingSceneName = "Loading";

    [Header("참조")]
    [Tooltip("배경 그림. Go() 에 스프라이트를 같이 넘기면 이 자리에 갈아 끼워진다.\n" +
             "비워 두면 씬에서 'Background' 라는 이름으로 찾아본다.")]
    [SerializeField] private Image background;

    [Header("설정")]
    [Tooltip("Go() 를 거치지 않고 이 씬을 직접 실행했을 때 갈 곳. 테스트용이다.")]
    [SerializeField] private string fallbackScene = "HospitalRoom";

    [Tooltip("다 읽었더라도 최소 이만큼은 보여 준다.\n" +
             "안 그러면 빠른 컴퓨터에서 로딩 화면이 번쩍 지나가 버린다.")]
    [SerializeField] private float minimumDisplayTime = 2.5f;

    /// <summary>로딩이 끝나면 갈 씬. 비어 있으면 로딩 화면에 머문다.</summary>
    public static string NextScene { get; private set; }

    /// <summary>이번 전환에 쓸 배경 그림. 비어 있으면 로딩 씬에 원래 깔린 그림을 쓴다.</summary>
    public static Sprite NextBackground { get; private set; }

    // Go() 를 거쳐 들어왔는지. 로딩 씬을 그냥 직접 실행한 경우와 구분하려고 둔다.
    private static bool requested;

    /// <summary>로딩 화면을 거쳐서 씬을 바꾼다.</summary>
    public static void Go(string sceneName)
    {
        Go(sceneName, null);
    }

    /// <summary>
    /// 배경 그림을 바꿔 가며 로딩 화면을 띄운다.
    /// sceneName 을 비워 두면 갈 곳 없이 로딩 화면만 띄우고 머문다.
    /// (목적지 씬을 아직 안 만들었을 때 쓴다)
    /// </summary>
    public static void Go(string sceneName, Sprite background)
    {
        if (!Application.CanStreamedLevelBeLoaded(LoadingSceneName))
        {
            Debug.LogError("[LoadingScreen] Loading scene is missing from Build Settings.");
            return;
        }

        bool hasTarget = !string.IsNullOrWhiteSpace(sceneName);
        if (hasTarget && (sceneName == LoadingSceneName ||
                          !Application.CanStreamedLevelBeLoaded(sceneName)))
        {
            Debug.LogError("[LoadingScreen] Invalid loading destination: " + sceneName);
            return;
        }

        NextScene = hasTarget ? sceneName : null;
        NextBackground = background;
        requested = true;

        SceneManager.LoadScene(LoadingSceneName);
    }

    private void Start()
    {
        ApplyBackground();

        bool viaGo = requested;
        requested = false;

        string target = viaGo ? NextScene : fallbackScene;
        NextScene = null;

        if (string.IsNullOrWhiteSpace(target))
        {
            // 갈 곳 없이 불린 경우. 로딩 화면만 띄운 채로 둔다.
            Debug.LogWarning("[LoadingScreen] 갈 씬이 비어 있어 로딩 화면에 머뭅니다. " +
                             "부른 쪽에 씬 이름을 채우면 넘어갑니다.", this);
            return;
        }

        if (target == LoadingSceneName || !Application.CanStreamedLevelBeLoaded(target))
        {
            Debug.LogError("[LoadingScreen] '" + target + "' 씬을 못 찾았습니다. " +
                           "Build Settings 에 들어가 있는지 확인해 주세요.", this);
            return;
        }

        StartCoroutine(LoadRoutine(target));
    }

    /// <summary>이번 전환에 넘어온 배경 그림이 있으면 갈아 끼운다.</summary>
    private void ApplyBackground()
    {
        var art = NextBackground;
        NextBackground = null;          // 다음 전환에 흘러가지 않도록 항상 비운다.

        if (art == null) return;

        var image = background;
        if (image == null)
        {
            // Inspector 연결이 비어 있으면 이름으로 찾아본다.
            var found = GameObject.Find("Background");
            if (found != null) image = found.GetComponent<Image>();
        }

        if (image != null) image.sprite = art;
        else Debug.LogWarning("[LoadingScreen] 배경 Image 를 못 찾아서 그림을 못 바꿨습니다. " +
                              "Background 칸에 연결해 주세요.", this);
    }

    private IEnumerator LoadRoutine(string target)
    {
        float started = Time.unscaledTime;
        // Let the loading artwork render before starting expensive scene deserialization.
        yield return null;

        var op = SceneManager.LoadSceneAsync(target);
        if (op == null)
        {
            Debug.LogError("[LoadingScreen] '" + target + "' 씬을 못 찾았습니다. " +
                           "Build Settings 에 들어가 있는지 확인해 주세요.");
            yield break;
        }

        // 다 읽어도 우리가 허락할 때까지 넘어가지 않게 잡아 둔다.
        op.allowSceneActivation = false;

        // allowSceneActivation 이 false 면 progress 는 0.9 에서 멈춘다. 그게 '다 읽었다' 는 뜻이다.
        while (op.progress < 0.9f) yield return null;

        float remain = minimumDisplayTime - (Time.unscaledTime - started);
        if (remain > 0f) yield return new WaitForSecondsRealtime(remain);

        op.allowSceneActivation = true;
    }
}
