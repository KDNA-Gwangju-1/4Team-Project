using System.Collections;
using UnityEngine;

namespace BrightDream.Combat
{
    /// <summary>
    /// 카메라를 짧게 흔든다. SimpleFirstPersonController가 매 프레임 localRotation만 갱신하므로
    /// 흔들림은 localPosition 오프셋으로 구현해 시점 조작과 충돌하지 않는다.
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        [SerializeField] private float defaultDuration = 0.35f;
        [SerializeField] private float defaultMagnitude = 0.25f;

        private Vector3 basePosition;
        private Coroutine shakeRoutine;

        private void Awake()
        {
            Instance = this;
            basePosition = transform.localPosition;
        }

        public void Shake() => Shake(defaultDuration, defaultMagnitude);

        public void Shake(float duration, float magnitude)
        {
            if (shakeRoutine != null) StopCoroutine(shakeRoutine);
            shakeRoutine = StartCoroutine(ShakeRoutine(duration, magnitude));
        }

        private IEnumerator ShakeRoutine(float duration, float magnitude)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float falloff = 1f - (elapsed / duration);
                transform.localPosition = basePosition + Random.insideUnitSphere * magnitude * falloff;
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.localPosition = basePosition;
            shakeRoutine = null;
        }
    }
}
