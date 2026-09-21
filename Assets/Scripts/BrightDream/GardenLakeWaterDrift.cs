using UnityEngine;

namespace BrightDream
{
    // Very slow, low-amplitude normal offset drift so the lake surface doesn't read as a static flat plane.
    // Two independent directions/speeds (base bump + detail normal) avoid a mechanical single-direction scroll.
    [RequireComponent(typeof(Renderer))]
    public class GardenLakeWaterDrift : MonoBehaviour
    {
        [SerializeField] private Vector2 bumpTiling = new Vector2(3f, 3f);
        [SerializeField] private Vector2 detailTiling = new Vector2(5f, 5f);
        [SerializeField] private Vector2 bumpSpeed = new Vector2(0.006f, 0.004f);
        [SerializeField] private Vector2 detailSpeed = new Vector2(-0.0035f, 0.0055f);

        private Renderer _renderer;
        private MaterialPropertyBlock _block;
        private static readonly int BumpMapStId = Shader.PropertyToID("_BumpMap_ST");
        private static readonly int DetailNormalMapStId = Shader.PropertyToID("_DetailNormalMap_ST");

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _block = new MaterialPropertyBlock();
        }

        private void Update()
        {
            Vector2 bumpOffset = bumpSpeed * Time.time;
            Vector2 detailOffset = detailSpeed * Time.time;

            _renderer.GetPropertyBlock(_block);
            _block.SetVector(BumpMapStId, new Vector4(bumpTiling.x, bumpTiling.y, bumpOffset.x % 1f, bumpOffset.y % 1f));
            _block.SetVector(DetailNormalMapStId, new Vector4(detailTiling.x, detailTiling.y, detailOffset.x % 1f, detailOffset.y % 1f));
            _renderer.SetPropertyBlock(_block);
        }
    }
}
