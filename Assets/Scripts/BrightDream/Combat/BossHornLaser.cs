using System.Collections;
using UnityEngine;

namespace BrightDream.Combat
{
    /// <summary>
    /// 회전 뿔 레이저 (3페이즈). 보스가 아레나 가운데에 선 채 머리를 숙여(HornCharge) 뿔에 빛을 모으는 동안
    /// 바닥에 빔이 나갈 첫 줄과 회전 방향 화살표를 그려 준다. 충전이 끝나면 뿔에서 쏜 빔 한 줄이
    /// 아레나 끝까지 뻗은 채 시계 또는 반시계 방향으로 한 바퀴 돈다.
    ///
    /// 회피는 달리기뿐이다 - 머리를 깊이 숙인 뿔에서 가슴 높이로 곧게 나가는 빔이라 점프로는 넘을 수 없고(hitHeight), 빔이 도는 방향과 같은 쪽으로
    /// 보스 주위를 달려 빔보다 앞서 있어야 한다. 빔은 멀수록 빨리 지나가서, 너무 멀리 있으면 따라잡히고
    /// 너무 붙으면 보스 몸에 부딪힌다.
    ///
    /// 빔은 처음에 플레이어보다 startLead 만큼 "뒤"(회전해 오는 쪽)에서 시작한다. 플레이어를 바로 겨눠
    /// 시작하면 피할 틈 없이 맞기 때문이다.
    /// </summary>
    public class BossHornLaser : MonoBehaviour
    {
        [Tooltip("뿔 끝 - Head 본의 자식이라 모션을 따라간다.")]
        [SerializeField] private Transform hornTip;
        [SerializeField] private BossHornGlow glow;
        [Tooltip("빔이 닿는 바닥 - 아레나 바닥 콜라이더. 바닥이 끝나는 곳에서 빔도 끊긴다.")]
        [SerializeField] private Collider groundCollider;
        [Tooltip("가산 머티리얼 - 빔 가운데의 하얗게 타는 심.")]
        [SerializeField] private Material beamMaterial;
        [Tooltip("알파 블렌드 머티리얼(텍스처 없음) - 바닥 표시와 빔 바깥 색. 밝은 바닥 위에서 가산은 하얗게 날아가서 색이 안 보인다.")]
        [SerializeField] private Material telegraphMaterial;

        [Tooltip("충전 시간 - 충전음(HornCharge) 길이와 같게 둔다.")]
        [SerializeField] private float chargeDuration = 1.4f;
        [Tooltip("한 바퀴 도는 데 걸리는 시간(초). 짧을수록 빨라서 보스 가까이 붙어 달려야 한다.")]
        [SerializeField] private float rotateDuration = 13f;
        [SerializeField] private float rotateDegrees = 360f;
        [Tooltip("빔이 처음에 플레이어보다 이만큼(도) 회전해 오는 쪽 뒤에서 시작한다.")]
        [SerializeField] private float startLead = 60f;
        [Tooltip("빔 최대 길이. 그 전에 아레나 바닥이 끝나면 거기서 끊긴다.")]
        [SerializeField] private float beamMaxLength = 20f;
        [Tooltip("판정이 시작되는 최소 거리(보스 앞). 실제로는 뿔 끝보다 항상 앞에서 시작한다.")]
        [SerializeField] private float beamStartDistance = 0.8f;
        [SerializeField] private float beamWidth = 0.6f;
        [Tooltip("발이 바닥에서 이 높이 안이면 맞는다 - 점프로는 못 벗어나는 높이.")]
        [SerializeField] private float hitHeight = 3f;
        [SerializeField] private float damage = 20f;
        [Tooltip("회전 방향 화살표를 그릴 반경(보스 둘레). 보스 몸에 가리지 않을 만큼 바깥.")]
        [SerializeField] private float arrowRadius = 5f;
        [SerializeField] private float arrowWidth = 0.4f;
        [SerializeField] private Color telegraphColor = new Color(0.95f, 0.18f, 0.3f, 1f);
        [SerializeField] private Color arrowColor = new Color(0.62f, 0.3f, 1f, 1f);
        [SerializeField] private Color beamColor = new Color(0.6f, 0.25f, 1f, 0.85f);
        [SerializeField] private Color beamCoreColor = new Color(0.6f, 1f, 1f, 1f);

        [Header("빔 유지음")]
        [Tooltip("빔이 도는 동안 반복 재생하는 소리. 비우면 Resources/Audio/Generated/LaserHum 을 쓴다.")]
        [SerializeField] private AudioClip humClip;
        [SerializeField] private float humVolume = 0.4f;
        [SerializeField] private float humFadeIn = 0.1f;
        [SerializeField] private float humFadeOut = 0.3f;

        private const int BeamSegments = 12;
        private AudioSource hum;
        private float humTarget; // 0이면 꺼지는 중
        private const int ArrowSegments = 40;
        private LineRenderer startLine, arrowArc, arrowHeadA, arrowHeadB, beamLine, beamCore;
        private float beamNear; // 이번 프레임 빔이 시작되는 거리(뿔 끝 앞) - 판정도 여기서부터

        public IEnumerator Run(Transform boss, Transform player, BossAnimationDriver driver)
        {
            EnsureLines();
            Vector3 toPlayer = player != null ? player.position - boss.position : boss.forward;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude < 0.0001f) toPlayer = boss.forward;
            float playerYaw = Mathf.Atan2(toPlayer.x, toPlayer.z) * Mathf.Rad2Deg;
            float sign = Random.value < 0.5f ? -1f : 1f; // +1 = 위에서 볼 때 시계 방향
            float startYaw = playerYaw - sign * startLead;

            // 충전 - 첫 줄 쪽으로 몸을 돌리며 머리를 숙이고, 첫 줄과 회전 방향을 바닥에 그린다.
            driver?.SetHornCharging(true);
            if (glow != null) glow.SetMode(BossHornGlow.Mode.Laser);
            GameSfx.At("HornCharge", hornTip != null ? hornTip.position : boss.position, .7f);
            DrawStartLine(boss.position, startYaw);
            DrawArrow(boss.position, startYaw, sign);
            Quaternion fromRot = boss.rotation;
            Quaternion aimRot = Quaternion.Euler(0f, startYaw, 0f);
            float t = 0f;
            while (t < chargeDuration)
            {
                t += Time.deltaTime;
                float p = t / chargeDuration;
                boss.rotation = Quaternion.Slerp(fromRot, aimRot, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(p * 2.5f)));
                if (glow != null) glow.SetCharge(p);
                float pulse = Mathf.Abs(Mathf.Sin(t * 10f));
                startLine.widthMultiplier = 0.22f + 0.14f * pulse;
                SetArrowAlpha(0.7f + 0.3f * pulse);
                yield return null;
            }

            // 발사 - 한 바퀴 돈다. 처음 0.6초는 천천히 가속해서, 시작하자마자 확 쓸고 오지 않게 한다.
            GameSfx.At("Charge", boss.position, .6f);
            if (glow != null) glow.SetCharge(1f);
            startLine.enabled = false;
            beamLine.enabled = true;
            beamCore.enabled = true;
            StartHum();
            SetArrowAlpha(0.5f); // 도는 동안에도 방향은 옅게 남겨 둔다
            float angle = 0f;
            float speed = rotateDegrees / Mathf.Max(rotateDuration, 0.01f);
            bool hitThisPass = false;
            t = 0f;
            while (angle < rotateDegrees)
            {
                t += Time.deltaTime;
                float ramp = Mathf.Clamp01(t / 0.6f);
                angle = Mathf.Min(rotateDegrees, angle + speed * ramp * Time.deltaTime);
                boss.rotation = Quaternion.Euler(0f, startYaw + sign * angle, 0f);
                float length = UpdateBeam(boss);
                bool inBeam = player != null && IsPlayerHit(boss, player, length);
                if (!inBeam)
                {
                    hitThisPass = false;
                }
                else if (!hitThisPass && PlayerHealth.Instance != null && !PlayerHealth.Instance.IsInvincible)
                {
                    // 빔이 한 번 지나갈 때 최대 1회만 맞는다. 빔 속도에 가깝게 따라 달리면 빔이 천천히 지나가
                    // 1초 무적이 먼저 끝나서 같은 빔에 두 번 맞던 문제를 막는다.
                    PlayerHealth.Instance.TakeDamage(damage, grantInvincibility: true);
                    CameraShake.Instance?.Shake();
                    hitThisPass = true;
                }
                yield return null;
            }

            Cancel();
            driver?.SetHornCharging(false);
        }

        /// <summary>빔·표시·뿔 발광을 즉시 끈다 (패턴 종료, 보스 처치).</summary>
        public void Cancel()
        {
            if (startLine != null)
            {
                startLine.enabled = false; arrowArc.enabled = false; arrowHeadA.enabled = false; arrowHeadB.enabled = false;
                beamLine.enabled = false; beamCore.enabled = false;
            }
            if (glow != null) glow.SetMode(BossHornGlow.Mode.Off);
            humTarget = 0f; // Update에서 부드럽게 줄여 끈다
        }

        private void OnDisable()
        {
            Cancel();
            if (hum != null) hum.Stop();
        }

        private void StartHum()
        {
            if (hum == null)
            {
                if (humClip == null) humClip = Resources.Load<AudioClip>("Audio/Generated/LaserHum");
                if (humClip == null) return;
                // GameSfx 음성과 같은 거리 감쇠 - 보스 위치에서 들린다.
                hum = gameObject.AddComponent<AudioSource>();
                hum.clip = humClip;
                hum.loop = true;
                hum.playOnAwake = false;
                hum.spatialBlend = 0.85f;
                hum.rolloffMode = AudioRolloffMode.Linear;
                hum.minDistance = 2f;
                hum.maxDistance = 30f;
            }
            hum.volume = 0f;
            humTarget = humVolume;
            if (!hum.isPlaying) hum.Play();
        }

        private void Update()
        {
            if (hum == null || !hum.isPlaying) return;
            // 게임오버 화면에서는 timeScale이 0이라 빔 코루틴이 멈춘 채 남는다 - 소리도 같이 끈다.
            float target = GameOverController.IsGameOver ? 0f : humTarget;
            float fadeTime = target > hum.volume ? humFadeIn : humFadeOut;
            float step = humVolume / Mathf.Max(fadeTime, 0.01f) * Time.unscaledDeltaTime;
            hum.volume = Mathf.MoveTowards(hum.volume, target, step);
            if (target <= 0f && hum.volume <= 0f) hum.Stop();
        }

        private bool IsPlayerHit(Transform boss, Transform player, float length)
        {
            Vector3 fwd = boss.forward; fwd.y = 0f; fwd.Normalize();
            Vector3 a = boss.position + fwd * beamNear;
            Vector3 b = boss.position + fwd * length;
            Vector2 p = new Vector2(player.position.x, player.position.z);
            Vector2 a2 = new Vector2(a.x, a.z), b2 = new Vector2(b.x, b.z);
            Vector2 ab = b2 - a2;
            float s = Mathf.Clamp01(Vector2.Dot(p - a2, ab) / Mathf.Max(ab.sqrMagnitude, 0.0001f));
            float dist = Vector2.Distance(p, a2 + ab * s);
            const float playerRadius = 0.35f;
            if (dist > beamWidth * 0.5f + playerRadius) return false;

            float groundY = GroundY(player.position, out bool onFloor);
            if (!onFloor) groundY = boss.position.y;
            return player.position.y - groundY <= hitHeight;
        }

        /// <summary>빔을 그리고 실제 길이(바닥이 끝나는 곳까지)를 돌려준다.</summary>
        private float UpdateBeam(Transform boss)
        {
            Vector3 fwd = boss.forward; fwd.y = 0f; fwd.Normalize();
            Vector3 tip = hornTip != null ? hornTip.position : boss.position + fwd * 2f + Vector3.up * 2.5f;
            // 뿔 끝이 보스 중심에서 앞으로 얼마나 나와 있는지. 빔은 반드시 그보다 앞에서 출발해야
            // 뒤로 꺾였다 나가는 "V"자가 생기지 않는다.
            float tipDistance = Mathf.Max(beamStartDistance, Vector3.Dot(tip - boss.position, fwd));
            beamNear = tipDistance;
            float length = Mathf.Max(FloorLength(boss.position, fwd), tipDistance + 0.5f);
            float flicker = 0.85f + 0.3f * Mathf.PerlinNoise(Time.time * 20f, 0f);
            beamLine.positionCount = BeamSegments + 1;
            beamCore.positionCount = BeamSegments + 1;

            // HornCharge 자세에서 뿔이 가슴 높이로 정면을 겨누므로, 빔은 뿔 끝에서 바닥과 나란히 곧게 나간다.
            // (예전엔 높은 뿔 끝에서 허리 높이로 휘어 내려와서 뿔이 휜 것처럼 보였다.)
            Vector3 end = tip + fwd * (length - tipDistance);
            for (int i = 0; i <= BeamSegments; i++)
            {
                Vector3 pos = Vector3.Lerp(tip, end, i / (float)BeamSegments);
                beamLine.SetPosition(i, pos);
                beamCore.SetPosition(i, pos);
            }
            beamLine.widthMultiplier = beamWidth * flicker;
            beamCore.widthMultiplier = beamWidth * 0.35f * flicker;
            return length;
        }

        /// <summary>보스 위치에서 dir 방향으로 바닥이 이어지는 거리 (최대 beamMaxLength). 아레나 밖 허공으로 빔이 뻗지 않게.</summary>
        private float FloorLength(Vector3 origin, Vector3 dir)
        {
            if (groundCollider == null) return beamMaxLength;
            const float step = 0.5f;
            float last = beamStartDistance;
            for (float d = beamStartDistance; d <= beamMaxLength; d += step)
            {
                GroundY(origin + dir * d, out bool hit);
                if (!hit) break;
                last = d;
            }
            return Mathf.Max(last, beamStartDistance + 0.5f);
        }

        private void DrawStartLine(Vector3 center, float yaw)
        {
            Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            float length = FloorLength(center, dir);
            // 빔과 같은 자리에서 시작하도록 뿔 끝까지의 거리만큼 띄운다 (충전 중 몸을 돌리므로 크기만 쓴다).
            float near = beamStartDistance;
            if (hornTip != null)
            {
                Vector3 offset = hornTip.position - center; offset.y = 0f;
                near = Mathf.Max(near, offset.magnitude);
            }
            startLine.positionCount = 10;
            for (int i = 0; i < 10; i++) startLine.SetPosition(i, GroundPoint(center, yaw, Mathf.Lerp(near, length, i / 9f)));
            startLine.enabled = true;
        }

        /// <summary>보스 둘레에 회전 방향을 알려 주는 굽은 화살표 (첫 줄에서 시작해 270도, 끝에 화살촉).</summary>
        private void DrawArrow(Vector3 center, float startYaw, float sign)
        {
            const float sweep = 270f;
            arrowArc.positionCount = ArrowSegments + 1;
            for (int i = 0; i <= ArrowSegments; i++)
                arrowArc.SetPosition(i, GroundPoint(center, startYaw + sign * sweep * i / ArrowSegments, arrowRadius));

            float tipYaw = startYaw + sign * sweep;
            Vector3 tip = GroundPoint(center, tipYaw, arrowRadius);
            // 화살촉 두 갈래 - 진행 방향 뒤쪽으로 벌린다
            Vector3 back1 = GroundPoint(center, tipYaw - sign * 16f, arrowRadius + 1.1f);
            Vector3 back2 = GroundPoint(center, tipYaw - sign * 16f, arrowRadius - 1.1f);
            arrowHeadA.positionCount = 2; arrowHeadA.SetPosition(0, back1); arrowHeadA.SetPosition(1, tip);
            arrowHeadB.positionCount = 2; arrowHeadB.SetPosition(0, back2); arrowHeadB.SetPosition(1, tip);
            arrowArc.widthMultiplier = arrowWidth;
            arrowHeadA.widthMultiplier = arrowWidth;
            arrowHeadB.widthMultiplier = arrowWidth;
            arrowArc.enabled = true; arrowHeadA.enabled = true; arrowHeadB.enabled = true;
        }

        private void SetArrowAlpha(float alpha)
        {
            Color c = new Color(arrowColor.r, arrowColor.g, arrowColor.b, arrowColor.a * alpha);
            foreach (var line in new[] { arrowArc, arrowHeadA, arrowHeadB }) { line.startColor = c; line.endColor = c; }
        }

        private Vector3 GroundPoint(Vector3 center, float yaw, float distance)
        {
            Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            Vector3 flat = center + dir * distance;
            float y = GroundY(flat, out bool hit);
            return new Vector3(flat.x, (hit ? y : center.y) + 0.12f, flat.z);
        }

        private float GroundY(Vector3 point, out bool hit)
        {
            hit = false;
            if (groundCollider == null) return point.y;
            Bounds b = groundCollider.bounds;
            if (groundCollider.Raycast(new Ray(new Vector3(point.x, b.max.y + 5f, point.z), Vector3.down), out RaycastHit h, b.size.y + 10f))
            {
                hit = true;
                return h.point.y;
            }
            return float.NaN;
        }

        private void EnsureLines()
        {
            if (startLine != null) return;
            Material alpha = telegraphMaterial != null ? telegraphMaterial : beamMaterial;
            startLine = MakeLine("LaserStartLine", telegraphColor, alpha);
            arrowArc = MakeLine("LaserArrowArc", arrowColor, alpha);
            arrowHeadA = MakeLine("LaserArrowHeadA", arrowColor, alpha);
            arrowHeadB = MakeLine("LaserArrowHeadB", arrowColor, alpha);
            beamLine = MakeLine("LaserBeam", beamColor, alpha);
            beamCore = MakeLine("LaserBeamCore", beamCoreColor, beamMaterial);
            // 빔 끝으로 갈수록 살짝 가늘어진다.
            // 뿔 끝에서는 가늘게 시작해 빠르게 굵어진다.
            var taper = new AnimationCurve(new Keyframe(0f, 0.3f), new Keyframe(0.03f, 1f), new Keyframe(0.92f, 1f), new Keyframe(1f, 0.4f));
            beamLine.widthCurve = taper;
            beamCore.widthCurve = taper;
        }

        private LineRenderer MakeLine(string name, Color color, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.sharedMaterial = material;
            line.startColor = color;
            line.endColor = color;
            line.numCapVertices = 4;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.alignment = LineAlignment.View;
            line.enabled = false;
            return line;
        }
    }
}
