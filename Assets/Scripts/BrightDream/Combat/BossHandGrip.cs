using UnityEngine;

namespace BrightDream.Combat
{
    // Samples the Blender-authored skeletal pose; the exit sequence owns timing.
    public sealed class BossHandGrip : MonoBehaviour
    {
        [SerializeField] private GameObject skeletonRoot;
        [SerializeField] private AnimationClip closeClip;
        public float Closure { get; private set; }

        private void Awake() { SetGrip(0f); }

        public void SetGrip(float closure)
        {
            Closure = Mathf.Clamp01(closure);
            if (skeletonRoot != null && closeClip != null)
                closeClip.SampleAnimation(skeletonRoot, closeClip.length * Closure);
        }
    }
}
