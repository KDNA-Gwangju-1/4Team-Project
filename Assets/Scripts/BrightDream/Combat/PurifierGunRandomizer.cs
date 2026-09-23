using UnityEngine;

namespace BrightDream.Combat
{
    /// <summary>
    /// 씬이 로드될 때(=한 판 시작 시) 정화총 디자인 후보 중 하나를 랜덤으로 골라
    /// 픽업 오브젝트와 장착 오브젝트 양쪽에 동일하게 적용한다.
    /// 두 대상 모두 이미 MeshFilter/MeshRenderer를 직접 들고 있는 구조라 메시·머티리얼만 교체한다.
    /// 원본 모델마다 forward/up 축이 제각각이라, FPS 손모델(equippedTarget)에는 디자인별
    /// view 위치/회전 보정을 추가로 적용해 총구가 크로스헤어 쪽을 향하도록 맞춘다.
    /// 월드 픽업(pickupTarget)의 위치/회전은 건드리지 않는다 - view 보정은 손모델 전용.
    /// 발사 판정(Raycast)은 이 스크립트와 무관하게 항상 카메라/크로스헤어 기준으로 이뤄진다 -
    /// 여기서 바꾸는 것은 시각적인 총 모델 배치일 뿐이다.
    /// </summary>
    public class PurifierGunRandomizer : MonoBehaviour
    {
        [System.Serializable]
        public class GunDesign
        {
            [Tooltip("디자인 원본 프리팹(glTF/FBX). 루트 또는 자식에 MeshFilter/MeshRenderer가 있어야 한다.")]
            public GameObject sourcePrefab;
            [Tooltip("이 디자인을 적용할 때 pickupTarget/equippedTarget에 그대로 설정할 localScale(각 축 동일). " +
                     "원본 메시 크기가 디자인마다 달라서 절대값으로 둔다 - 상대 배율이 아니다.")]
            public float uniformScale = 1f;
            [Tooltip("FPS 손모델(equippedTarget) 전용 localPosition - 총 몸체가 화면 오른쪽 아래에 오도록 맞춘다.")]
            public Vector3 viewPositionOffset = new Vector3(0.28f, -0.28f, 0.55f);
            [Tooltip("FPS 손모델(equippedTarget) 전용 localRotation(Euler) - 원본 모델마다 forward/up 축이 달라서, " +
                     "총구가 카메라 forward(크로스헤어) 쪽을 향하도록 디자인별로 보정한다.")]
            public Vector3 viewRotationOffset;
            [Tooltip("총구 끝의 위치 - 원본 메시 좌표 기준이다. 회전·스케일은 부모(장착 오브젝트)를 " +
                     "따라가므로 여기에는 보정 전 좌표를 그대로 넣는다. 손잡이를 기준으로 총열이 " +
                     "어느 쪽으로 뻗었는지 재서 구한 값이다.")]
            public Vector3 muzzleLocalOffset;
        }

        [SerializeField] private GunDesign[] designs;
        [SerializeField] private MeshFilter pickupTarget;
        [SerializeField] private MeshFilter equippedTarget;
        [Tooltip("총알이 나가는 자리. 고른 디자인의 총구 끝으로 옮겨 준다. " +
                 "PurifierGunController 의 Muzzle Point 와 같은 오브젝트를 연결한다.")]
        [SerializeField] private Transform muzzlePoint;

        private void Awake()
        {
            if (designs == null || designs.Length == 0) return;

            GunDesign chosen = designs[Random.Range(0, designs.Length)];
            if (chosen.sourcePrefab == null) return;

            MeshFilter sourceMf = chosen.sourcePrefab.GetComponentInChildren<MeshFilter>(true);
            MeshRenderer sourceMr = chosen.sourcePrefab.GetComponentInChildren<MeshRenderer>(true);
            if (sourceMf == null || sourceMr == null) return;

            ApplyMesh(pickupTarget, sourceMf, sourceMr, chosen.uniformScale);
            ApplyMesh(equippedTarget, sourceMf, sourceMr, chosen.uniformScale);

            if (equippedTarget != null)
            {
                equippedTarget.transform.localPosition = chosen.viewPositionOffset;
                equippedTarget.transform.localRotation = Quaternion.Euler(chosen.viewRotationOffset);
            }

            // 총구를 고른 디자인의 총열 끝으로 옮긴다. 장착 오브젝트의 자식이라
            // 회전과 스케일은 부모를 그대로 따라간다.
            if (muzzlePoint != null && equippedTarget != null)
            {
                muzzlePoint.SetParent(equippedTarget.transform, false);
                muzzlePoint.localPosition = chosen.muzzleLocalOffset;
                muzzlePoint.localRotation = Quaternion.identity;
            }
        }

        private static void ApplyMesh(MeshFilter target, MeshFilter sourceMf, MeshRenderer sourceMr, float uniformScale)
        {
            if (target == null) return;

            target.sharedMesh = sourceMf.sharedMesh;
            MeshRenderer targetMr = target.GetComponent<MeshRenderer>();
            if (targetMr != null) targetMr.sharedMaterials = sourceMr.sharedMaterials;

            target.transform.localScale = Vector3.one * uniformScale;
        }
    }
}
