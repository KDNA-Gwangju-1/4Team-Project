using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BrightDream.Combat
{
    /// <summary>
    /// Stage2 안내 문구가 뜨는 시점부터 맵 오른쪽에서 5종 몬스터를 랜덤으로,
    /// 서로 겹치지 않게 계속 스폰해 플레이어 쪽으로 보낸다.
    /// 정화 목표(MonsterPurifyManager.TargetCount)를 채우면 스폰을 멈춘다.
    /// </summary>
    public class MonsterSpawner : MonoBehaviour
    {
        [Tooltip("씬에 미리 배치해 둔 5종 몬스터 템플릿(비활성 상태). 그대로 Instantiate해서 사용한다.")]
        [SerializeField] private GameObject[] monsterTemplates;
        [Tooltip("스폰 즉시 제거할 테스트 배치 몬스터(더 이상 필요 없는 원본).")]
        [SerializeField] private GameObject[] testPlacedMonstersToRemove;

        [SerializeField] private float spawnX = 41f;
        [SerializeField] private float spawnZMin = 30f;
        [SerializeField] private float spawnZMax = 45f;
        [SerializeField] private float spawnY = 0.06f;
        [SerializeField] private float spawnInterval = 1.2f;
        [SerializeField] private float minSpawnSpacing = 3f;
        [SerializeField] private int maxSpawnAttempts = 8;

        [Tooltip("전투 구역 중심 (기본값: CombatArena 바닥 중심).")]
        [SerializeField] private Vector3 arenaCenter = new Vector3(30.3987f, 0f, 37.6758f);
        [Tooltip("이 거리보다 플레이어가 중심에서 멀어지면 전투를 이탈한 것으로 보고 Game Over 처리한다.")]
        [SerializeField] private float leashDistance = 30f;

        private readonly List<Transform> activeMonsters = new List<Transform>();
        private bool spawning;

        private void OnEnable()
        {
            StageProgressManager.OnStageChanged += HandleStageChanged;
        }

        private void OnDisable()
        {
            StageProgressManager.OnStageChanged -= HandleStageChanged;
        }

        private void HandleStageChanged(int currentStage)
        {
            if (currentStage != 2 || spawning) return;
            spawning = true;

            foreach (GameObject go in testPlacedMonstersToRemove)
                if (go != null) Destroy(go);

            StartCoroutine(SpawnLoop());
        }

        private void Update()
        {
            if (!spawning) return;
            if (MonsterPurifyManager.Instance != null &&
                MonsterPurifyManager.Instance.PurifiedCount >= MonsterPurifyManager.TargetCount) return;

            Transform player = PlayerHealth.Instance != null ? PlayerHealth.Instance.transform : null;
            if (player == null) return;

            Vector3 flatPlayerPos = new Vector3(player.position.x, arenaCenter.y, player.position.z);
            if (Vector3.Distance(flatPlayerPos, arenaCenter) > leashDistance)
            {
                GameOverController.Instance?.TriggerGameOver();
            }
        }

        private IEnumerator SpawnLoop()
        {
            while (monsterTemplates.Length > 0 &&
                   (MonsterPurifyManager.Instance == null ||
                    MonsterPurifyManager.Instance.PurifiedCount < MonsterPurifyManager.TargetCount))
            {
                activeMonsters.RemoveAll(t => t == null);

                GameObject template = monsterTemplates[Random.Range(0, monsterTemplates.Length)];
                Vector3 spawnPos = FindSpawnPosition();

                GameObject instance = Instantiate(template, spawnPos, Quaternion.identity);
                instance.SetActive(true);
                activeMonsters.Add(instance.transform);

                yield return new WaitForSeconds(spawnInterval);
            }
        }

        private Vector3 FindSpawnPosition()
        {
            for (int i = 0; i < maxSpawnAttempts; i++)
            {
                Vector3 candidate = new Vector3(spawnX, spawnY, Random.Range(spawnZMin, spawnZMax));
                bool overlaps = false;
                foreach (Transform t in activeMonsters)
                {
                    if (t == null) continue;
                    if (Vector3.Distance(candidate, t.position) < minSpawnSpacing)
                    {
                        overlaps = true;
                        break;
                    }
                }
                if (!overlaps) return candidate;
            }
            return new Vector3(spawnX, spawnY, Random.Range(spawnZMin, spawnZMax));
        }
    }
}
