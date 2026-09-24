using UnityEngine;
using UnityEngine.SceneManagement;

namespace BrightDream.Combat
{
    /// <summary>
    /// HP 소진 또는 전투 구역 이탈 시 게임을 정지하고 Game Over UI를 띄운다.
    /// 다시 시작 버튼은 현재 씬을 처음부터 다시 로드해 시작 스폰 위치로 되돌린다.
    /// </summary>
    public class GameOverController : MonoBehaviour
    {
        public static GameOverController Instance { get; private set; }

        [SerializeField] private GameObject gameOverPanel;
        [Tooltip("Game Over 시 비활성화할 플레이어 조작 스크립트들 (이동/시점/상호작용/발사).")]
        [SerializeField] private MonoBehaviour[] disableOnGameOver;

        private bool isGameOver;

        /// <summary>게임 오버 화면이 떠 있으면 true. 그동안은 일시정지 메뉴가 열리지 않는다.</summary>
        public static bool IsGameOver => Instance != null && Instance.isGameOver;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            StylePanel();
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
        }

        /// <summary>HUD 와 같은 카드 디자인으로 게임 오버 화면을 꾸민다 (제목 + 카드 버튼).</summary>
        private void StylePanel()
        {
            if (gameOverPanel == null) return;
            foreach (var text in gameOverPanel.GetComponentsInChildren<UnityEngine.UI.Text>(true))
            {
                var button = text.GetComponentInParent<UnityEngine.UI.Button>(true);
                if (button == null)
                {
                    // 제목
                    text.font = HangulFont.GetEmphasis();
                    text.fontStyle = FontStyle.Normal;
                    text.fontSize = 68;
                    text.color = new Color(1f, .93f, .86f);
                    var shadow = text.GetComponent<UnityEngine.UI.Shadow>();
                    if (shadow == null) shadow = text.gameObject.AddComponent<UnityEngine.UI.Shadow>();
                    shadow.effectColor = new Color(.55f, .16f, .24f, .9f);
                    shadow.effectDistance = new Vector2(0f, -4f);
                    continue;
                }
                text.font = HangulFont.GetEmphasis();
                text.fontStyle = FontStyle.Normal;
                text.fontSize = 28;
                text.color = ChapterHudStyle.Ink(true);
                var image = button.GetComponent<UnityEngine.UI.Image>();
                if (image != null)
                {
                    ChapterHudStyle.SkinCard(image, true);
                    var rt = image.rectTransform;
                    rt.sizeDelta = new Vector2(Mathf.Max(rt.sizeDelta.x, 300f), Mathf.Max(rt.sizeDelta.y, 72f));
                }
                var colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(.9f, .96f, 1f);
                colors.pressedColor = new Color(.8f, .9f, .97f);
                colors.selectedColor = colors.highlightedColor;
                button.colors = colors;
            }
            var dim = gameOverPanel.GetComponent<UnityEngine.UI.Image>();
            if (dim != null) dim.color = new Color(.10f, .08f, .16f, .72f);
        }

        public void TriggerGameOver()
        {
            if (isGameOver) return;
            isGameOver = true;

            foreach (MonoBehaviour mb in disableOnGameOver)
                if (mb != null) mb.enabled = false;

            if (gameOverPanel != null) gameOverPanel.SetActive(true);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 0f;
        }

        private void Update()
        {
            // 재시작은 모든 챕터에서 R 키로 통일했다 (버튼 클릭도 그대로 된다).
            if (isGameOver && Input.GetKeyDown(KeyCode.R)) RestartScene();
        }

        public void RestartScene()
        {
            // 챕터2 재시도와 같은 짧은 페이드. timeScale 은 화면이 다 가려진 뒤 SceneFader 가 1 로 되돌린다.
            if (SceneFader.IsFading) return;
            SceneFader.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
