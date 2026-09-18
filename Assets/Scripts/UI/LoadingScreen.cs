using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 로딩 화면.
///
/// 이 씬이 뜨면 다음 씬을 뒤에서 미리 읽어들이고, 다 되면 알아서 넘어간다.
///
/// 쓰는 법 — 어디서든 이렇게 부르면 된다.
/// <code>
///     LoadingScreen.Go("HospitalRoom");
/// </code>
/// 로딩 씬이 먼저 뜨고, 다 읽으면 HospitalRoom 으로 넘어간다.
///
/// 주의 : 로딩 씬과 목적지 씬이 둘 다 Build Settings 에 들어가 있어야 한다.
/// </summary>
[DisallowMultipleComponent]
public class LoadingScreen : MonoBehaviour
{
    /// <summary>로딩 씬 이름. 씬 파일 이름을 바꾸면 여기도 같이 바꿔야 한다.</summary>
    public const string LoadingSceneName = "Loading";

    [Header("설정")]
    [Tooltip("Go() 를 거치지 않고 이 씬을 직접 실행했을 때 갈 곳. 테스트용이다.")]
    [SerializeField] private string fallbackScene = "HospitalRoom";

    [Tooltip("다 읽었더라도 최소 이만큼은 보여 준다.\n" +
             "안 그러면 빠른 컴퓨터에서 로딩 화면이 번쩍 지나가 버린다.")]
    [SerializeField] private float minimumDisplayTime = 2.5f;

    /// <summary>로딩이 끝나면 갈 씬. Go() 가 채워 준다.</summary>
    public static string NextScene { get; private set; }

    /// <summary>로딩 화면을 거쳐서 씬을 바꾼다.</summary>
    public static void Go(string sceneName)
    {
        NextScene = sceneName;
        SceneManager.LoadScene(LoadingSceneName);
    }

    private void Start()
    {
        string target = string.IsNullOrWhiteSpace(NextScene) ? fallbackScene : NextScene;

        if (string.IsNullOrWhiteSpace(target))
        {
            Debug.LogError("[LoadingScreen] 갈 씬이 정해져 있지 않습니다. " +
                           "LoadingScreen.Go(\"씬이름\") 으로 부르거나 Fallback Scene 을 채워 주세요.");
            return;
        }

        StartCoroutine(LoadRoutine(target));
    }

    private IEnumerator LoadRoutine(string target)
    {
        float started = Time.unscaledTime;

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

        NextScene = null;
        op.allowSceneActivation = true;
    }
}
