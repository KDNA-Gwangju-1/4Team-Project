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

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
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

        public void RestartScene()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
