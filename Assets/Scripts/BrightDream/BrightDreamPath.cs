using UnityEngine;

/// <summary>
/// 밝은 꿈 맵의 메인 산책로 중심선을 Waypoint 배열로 들고 있는 데이터 컴포넌트.
///
/// 맵이 좌우로 꺾이는 구조라 구간 경계를 Z 좌표로 나눌 수 없다.
/// 그래서 구간은 "START 에서부터 걸어간 거리(m)" 로 구분한다.
///
/// 블록아웃 빌더(BrightDreamBlockoutBuilder)가 길을 깔면서 같은 곡선을 여기에 기록해 두고,
/// 자동 걷기 테스트가 이 Waypoint 를 따라 걸으며 구간별 소요 시간을 잰다.
/// </summary>
public class BrightDreamPath : MonoBehaviour
{
    /// <summary>구간 하나의 이름과 거리 범위(m).</summary>
    [System.Serializable]
    public struct Area
    {
        public string name;

        [Tooltip("구간 시작 - START 에서부터의 거리(m)")]
        public float startZ;

        [Tooltip("구간 끝 - START 에서부터의 거리(m)")]
        public float endZ;
    }

    [Tooltip("START 부터 유니콘까지의 길 중심선 (월드 좌표)")]
    public Vector3[] waypoints;

    [Tooltip("구간별 거리 범위 - 자동 걷기 리포트에 쓰인다")]
    public Area[] areas;

    [Tooltip("구간 경계가 Z 좌표가 아니라 이동 거리 기준이라는 표시. 꺾인 맵에서는 항상 true.")]
    public bool useArcLength = true;

    /// <summary>Waypoint 를 모두 이은 실제 이동 경로 길이(m).</summary>
    public float TotalLength
    {
        get
        {
            float total = 0f;
            for (int i = 1; i < waypoints.Length; i++)
            {
                total += Vector3.Distance(waypoints[i - 1], waypoints[i]);
            }
            return total;
        }
    }

    /// <summary>걸어온 거리가 속한 구간 이름. 어느 구간에도 없으면 빈 문자열.</summary>
    public string AreaNameAtDistance(float distance)
    {
        foreach (var area in areas)
        {
            if (distance >= area.startZ && distance < area.endZ) return area.name;
        }
        return areas.Length > 0 && distance >= areas[areas.Length - 1].endZ
            ? areas[areas.Length - 1].name
            : string.Empty;
    }

    private void OnDrawGizmosSelected()
    {
        if (waypoints == null || waypoints.Length < 2) return;

        Gizmos.color = Color.magenta;
        for (int i = 1; i < waypoints.Length; i++)
        {
            Gizmos.DrawLine(waypoints[i - 1] + Vector3.up * 0.2f, waypoints[i] + Vector3.up * 0.2f);
        }
    }
}
