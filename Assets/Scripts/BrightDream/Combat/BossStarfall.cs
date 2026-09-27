using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BrightDream.Combat
{
    /// <summary>
    /// 별똥별 낙하 (2페이즈부터). 보스가 제자리에서 몇 초 동안 별을 불러내는 동안(Channel),
    /// 일정 간격으로 금빛 원이 하나씩 생기고 원이 가득 차는 순간 하늘에서 별이 떨어진다.
    /// 몇 개 걸러 하나는 그 순간 플레이어가 선 자리를 노려서, 소환이 끝날 때까지 계속 움직여야 한다.
    ///
    /// 회피는 원 밖으로 걸어 나가는 것뿐이다 - 점프 착지 공격과 달리 점프로는 피할 수 없다
    /// (hitHeight 안이면 공중이어도 맞는다). 두 패턴의 회피 방법이 겹치지 않게 하려는 의도.
    /// </summary>
    public class BossStarfall : MonoBehaviour
    {
        [Tooltip("낙하 지점을 고를 바닥 - 05_boss_platform. 이 위에만 떨어진다.")]
        [SerializeField] private Collider groundCollider;
        [Tooltip("BrightDream/SlamIndicator - 빌드에서도 찾도록 직접 연결.")]
        [SerializeField] private Shader indicatorShader;
        [Tooltip("떨어지는 별 모델(37_star).")]
        [SerializeField] private GameObject starPrefab;
        [Tooltip("착지 순간 튀는 빛가루용 가산 머티리얼.")]
        [SerializeField] private Material impactMaterial;

        [SerializeField] private float markerRadius = 1.8f;
        [Tooltip("원이 차오르는 시간 - 가득 차는 순간 별이 닿는다. 원이 뜬 뒤 피할 수 있는 시간이기도 하다.")]
        [SerializeField] private float telegraphDuration = 1.6f;
        [SerializeField] private float fallHeight = 14f;
        [Tooltip("별이 하늘에서 떨어지는 데 걸리는 시간. 길수록 떨어지는 모습이 잘 보인다.")]
        [SerializeField] private float fallDuration = 1f;
        [Tooltip("낙하 가속 정도. 1이면 일정한 속도, 클수록 끝에서 확 빨라진다.")]
        [SerializeField] private float fallAcceleration = 1.5f;
        [Tooltip("몇 개마다 하나씩 플레이어가 선 자리를 노릴지. 2면 하나 걸러 하나.")]
        [SerializeField] private int targetPlayerEvery = 2;
        [Tooltip("나머지 별은 플레이어 주변 이 반경 안에 흩뿌린다.")]
        [SerializeField] private float scatterRadius = 7f;
        [Tooltip("아직 떨어지지 않은 다른 원과 이만큼은 떨어뜨린다.")]
        [SerializeField] private float minSeparation = 2.6f;
        [SerializeField] private float damage = 20f;
        [Tooltip("바닥에서 이 높이 안이면 공중에 있어도 맞는다 (점프 회피 불가).")]
        [SerializeField] private float hitHeight = 2.5f;
        [Tooltip("별 모델의 가장 긴 변을 이 길이(m)에 맞춘다.")]
        [SerializeField] private float starSize = 1.3f;
        [SerializeField] private Color fillColor = new Color(1f, 0.74f, 0.3f, 1f);
        [SerializeField] private Color edgeColor = new Color(1f, 0.95f, 0.7f, 1f);

        private readonly List<BossSlamIndicator> markers = new List<BossSlamIndicator>();
        private readonly List<bool> markerBusy = new List<bool>();
        private readonly List<GameObject> liveStars = new List<GameObject>();
        private readonly List<Vector3> pendingPoints = new List<Vector3>();
        private int running;

        /// <summary>아직 떨어지지 않은 별이 있는지.</summary>
        public bool IsRunning => running > 0;

        /// <summary>
        /// summonDuration 동안 count개의 별을 고르게 나눠 불러낸다. 마지막 별을 부르는 순간 끝난다
        /// (그 별이 실제로 떨어지는 건 telegraphDuration 뒤). 보스는 이 코루틴이 끝날 때까지 제자리에 선다.
        /// </summary>
        public IEnumerator Channel(float summonDuration, int count, Transform player)
        {
            if (player == null || count <= 0) yield break;
            GameSfx.Play("Warning", .45f);
            float interval = count > 1 ? summonDuration / (count - 1) : 0f;
            for (int i = 0; i < count; i++)
            {
                bool aimAtPlayer = targetPlayerEvery > 0 && i % targetPlayerEvery == 0;
                if (TryPickPoint(player.position, aimAtPlayer, out Vector3 point))
                    StartCoroutine(StarRoutine(point, player));
                if (i < count - 1) yield return new WaitForSeconds(interval);
            }
        }

        public void Cancel()
        {
            StopAllCoroutines();
            running = 0;
            pendingPoints.Clear();
            for (int i = 0; i < markers.Count; i++) { if (markers[i] != null) markers[i].Hide(); markerBusy[i] = false; }
            foreach (var s in liveStars) if (s != null) Destroy(s);
            liveStars.Clear();
        }

        private void OnDisable() => Cancel();

        /// <summary>플레이어가 선 자리(aimAtPlayer) 또는 주변 무작위 지점. 아직 안 떨어진 다른 원과 겹치지 않게 고른다.</summary>
        private bool TryPickPoint(Vector3 playerPos, bool aimAtPlayer, out Vector3 point)
        {
            if (aimAtPlayer && TryGround(playerPos, out point) && !TooCloseToPending(point)) return true;
            for (int attempt = 0; attempt < 25; attempt++)
            {
                Vector2 r = Random.insideUnitCircle * scatterRadius;
                if (TryGround(playerPos + new Vector3(r.x, 0f, r.y), out point) && !TooCloseToPending(point)) return true;
            }
            point = playerPos;
            return false;
        }

        private bool TooCloseToPending(Vector3 point)
        {
            foreach (var p in pendingPoints)
                if (Vector2.Distance(new Vector2(p.x, p.z), new Vector2(point.x, point.z)) < minSeparation) return true;
            return false;
        }

        private bool TryGround(Vector3 point, out Vector3 grounded)
        {
            grounded = point;
            if (groundCollider == null) return true;
            Bounds b = groundCollider.bounds;
            var ray = new Ray(new Vector3(point.x, b.max.y + 5f, point.z), Vector3.down);
            if (!groundCollider.Raycast(ray, out RaycastHit hit, b.size.y + 10f)) return false;
            grounded = hit.point;
            return true;
        }

        /// <summary>쉬고 있는 원 표시를 빌린다. 동시에 떠 있는 원 수만큼만 만들어 돌려쓴다.</summary>
        private int AcquireMarker()
        {
            for (int i = 0; i < markers.Count; i++)
                if (!markerBusy[i]) { markerBusy[i] = true; return i; }

            var go = new GameObject("StarfallMarker_" + markers.Count);
            go.transform.SetParent(transform, false);
            var m = go.AddComponent<BossSlamIndicator>();
            m.SetShader(indicatorShader);
            m.SetGroundCollider(groundCollider);
            m.SetColors(fillColor, edgeColor);
            m.SetResolution(16); // 반경 1.8m라 촘촘할 필요가 없다
            markers.Add(m);
            markerBusy.Add(true);
            return markers.Count - 1;
        }

        private IEnumerator StarRoutine(Vector3 point, Transform player)
        {
            running++;
            pendingPoints.Add(point);
            int markerIndex = AcquireMarker();
            BossSlamIndicator marker = markers[markerIndex];

            marker.Show(point, markerRadius);
            GameObject star = null;
            float elapsed = 0f;
            Vector3 top = point + Vector3.up * fallHeight;
            float spin = Random.Range(360f, 720f) * (Random.value < 0.5f ? -1f : 1f);
            while (elapsed < telegraphDuration)
            {
                elapsed += Time.deltaTime;
                marker.SetFill(elapsed / telegraphDuration);

                float fallStart = Mathf.Max(0f, telegraphDuration - fallDuration);
                if (elapsed >= fallStart)
                {
                    if (star == null) star = SpawnStar(top);
                    if (star != null)
                    {
                        float f = Mathf.Clamp01((elapsed - fallStart) / Mathf.Max(telegraphDuration - fallStart, 0.0001f));
                        star.transform.position = Vector3.Lerp(top, point, Mathf.Pow(f, fallAcceleration)); // 점점 빨라진다
                        star.transform.Rotate(Vector3.up, spin * Time.deltaTime, Space.World);
                    }
                }
                yield return null;
            }

            if (star != null) { liveStars.Remove(star); Destroy(star); }
            pendingPoints.Remove(point);
            Impact(point, player);

            // 원은 짧게 흐려지며 사라진다.
            float fadeT = 0f;
            while (fadeT < 0.25f)
            {
                fadeT += Time.deltaTime;
                marker.SetAlpha(1f - fadeT / 0.25f);
                yield return null;
            }
            marker.Hide();
            markerBusy[markerIndex] = false;
            running--;
        }

        private GameObject SpawnStar(Vector3 at)
        {
            if (starPrefab == null) return null;
            var star = Instantiate(starPrefab, at, Quaternion.Euler(Random.Range(-20f, 20f), Random.Range(0f, 360f), Random.Range(-20f, 20f)));
            foreach (var c in star.GetComponentsInChildren<Collider>()) Destroy(c);
            // 모델 원본 크기와 무관하게 가장 긴 변을 starSize에 맞춘다.
            var renderers = star.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds b = renderers[0].bounds;
                foreach (var r in renderers) b.Encapsulate(r.bounds);
                float longest = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                if (longest > 0.0001f) star.transform.localScale *= starSize / longest;
            }
            liveStars.Add(star);
            return star;
        }

        private void Impact(Vector3 point, Transform player)
        {
            GameSfx.At("StarImpact", point, .5f);
            CameraShake.Instance?.Shake();
            SpawnBurst(point);

            if (player == null || PlayerHealth.Instance == null || PlayerHealth.Instance.IsInvincible) return;
            Vector2 flat = new Vector2(player.position.x - point.x, player.position.z - point.z);
            if (flat.magnitude > markerRadius) return;              // 원 밖 - 회피 성공
            if (player.position.y - point.y > hitHeight) return;     // 아주 높은 곳 (점프로는 여기까지 못 간다)
            PlayerHealth.Instance.TakeDamage(damage, grantInvincibility: true);
        }

        private void SpawnBurst(Vector3 point)
        {
            if (impactMaterial == null) return;
            var go = new GameObject("StarImpactBurst");
            go.transform.position = point + Vector3.up * 0.2f;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false;
            main.duration = 0.5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.3f);
            main.startColor = new ParticleSystem.MinMaxGradient(fillColor, edgeColor);
            main.gravityModifier = 0.6f;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 26) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.4f;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = impactMaterial;
            ps.Play();
            Destroy(go, 1.5f);
        }
    }
}
