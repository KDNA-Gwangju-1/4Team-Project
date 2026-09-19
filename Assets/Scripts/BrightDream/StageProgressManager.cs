using System;
using UnityEngine;

/// <summary>
/// 스테이지 진행 순서를 강제하는 싱글턴. 이전 스테이지를 먼저 밟지 않으면
/// 다음 스테이지 트리거는 무시되어 게임이 진행되지 않는다.
/// </summary>
public class StageProgressManager : MonoBehaviour
{
    public static StageProgressManager Instance { get; private set; }

    /// <summary>CurrentStage가 갱신될 때마다 새 값과 함께 발생. StageGate가 이를 구독해 잠금을 해제한다.</summary>
    public static event Action<int> OnStageChanged;

    public int CurrentStage { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>stageIndex가 바로 다음 순서일 때만 진행을 인정하고 true를 반환한다.</summary>
    public bool TryCompleteStage(int stageIndex)
    {
        if (stageIndex != CurrentStage + 1) return false;
        CurrentStage = stageIndex;
        OnStageChanged?.Invoke(CurrentStage);
        return true;
    }
}
