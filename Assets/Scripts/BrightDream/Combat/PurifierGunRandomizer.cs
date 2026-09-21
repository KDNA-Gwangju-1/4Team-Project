using UnityEngine;

namespace BrightDream.Combat
{
    /// <summary>
    /// 씬이 로드될 때(=한 판 시작 시) 정화총 디자인 후보 중 하나를 랜덤으로 골라
    /// 픽업 오브젝트와 장착 오브젝트 양쪽에 동일하게 적용한다.
    /// 두 대상 모두 이미 MeshFilter/MeshRenderer를 직접 들고 있는 구조라 메시·머티리얼만 교체하고,
    /// Transform(위치/부모/스케일 배율)은 각자 기존 값을 그대로 둔다.
    /// </summary>
    public class PurifierGunRandomizer : MonoBehaviour
    {
        [System.Serializable]
        public class GunDesign
        {
            [Tooltip("디자인 원본 프리팹(글TF/FBX). 루트에 MeshFilter/MeshRenderer가 있어야 한다.")]
            public GameObject sourcePrefab;
            [Tooltip("이 디자인을 적용할 때 pickupTarget/equippedTarget에 그대로 설정할 localScale(각 축 동일). " +
                     "원본 메시 크기가 디자인마다 달라서 절대값으로 둔다 - 상대 배율이 아니다.")]
            public float uniformScale = 1f;
        }

        [SerializeField] private GunDesign[] designs;
        [SerializeField] private MeshFilter pickupTarget;
        [SerializeField] private MeshFilter equippedTarget;

        private void Awake()
        {
            if (designs == null || designs.Length == 0) return;

            GunDesign chosen = designs[Random.Range(0, designs.Length)];
            if (chosen.sourcePrefab == null) return;

            MeshFilter sourceMf = chosen.sourcePrefab.GetComponentInChildren<MeshFilter>(true);
            MeshRenderer sourceMr = chosen.sourcePrefab.GetComponentInChildren<MeshRenderer>(true);
            if (sourceMf == null || sourceMr == null) return;

            Apply(pickupTarget, sourceMf, sourceMr, chosen.uniformScale);
            Apply(equippedTarget, sourceMf, sourceMr, chosen.uniformScale);
        }

        private static void Apply(MeshFilter target, MeshFilter sourceMf, MeshRenderer sourceMr, float uniformScale)
        {
            if (target == null) return;

            target.sharedMesh = sourceMf.sharedMesh;
            MeshRenderer targetMr = target.GetComponent<MeshRenderer>();
            if (targetMr != null) targetMr.sharedMaterials = sourceMr.sharedMaterials;

            target.transform.localScale = Vector3.one * uniformScale;
        }
    }
}
