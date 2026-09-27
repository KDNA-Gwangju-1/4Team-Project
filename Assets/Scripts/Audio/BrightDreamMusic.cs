using BrightDream.Combat;
using UnityEngine;

/// <summary>
/// 밝은 꿈의 음악 전환. GameSfx 가 씬에 들어올 때 하나 만들어 붙인다.
///   탐색 (BGM_Garden) → 보스 스테이지 진입 (BGM_GardenBoss) → 보스 정화 (음악을 걷고 균열 연출음만 남긴다)
/// </summary>
public sealed class BrightDreamMusic : MonoBehaviour
{
    private const int BossStage = 3;

    private void OnEnable()
    {
        StageProgressManager.OnStageChanged += HandleStageChanged;
        BossWeakpointController.OnBossDefeated += HandleBossDefeated;
    }

    private void OnDisable()
    {
        StageProgressManager.OnStageChanged -= HandleStageChanged;
        BossWeakpointController.OnBossDefeated -= HandleBossDefeated;
    }

    private void HandleStageChanged(int stage)
    {
        if (stage == BossStage) GameSfx.PlayMusic("BGM_GardenBoss", 2f);
    }

    private void HandleBossDefeated()
    {
        GameSfx.StopMusic(2.5f);
    }
}
