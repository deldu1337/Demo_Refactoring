using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerSkeletonSnapshot
{
    private struct Bone
    {
        public Transform t;
        public Vector3 lp;
        public Quaternion lr;
        public Vector3 ls;
        public bool active;
    }

    private readonly List<Bone> bones = new();
    private readonly Transform rootCaptured;         // 어떤 루트를 기준으로 캡처했는지
    private readonly bool includeRootLocalTransform; // 루트의 local 변환도 복원할지

    private readonly Vector3 worldRootPos;           // 캡처 시 루트의 월드 위치(필요 시 사용)
    private readonly Quaternion worldRootRot;        // 캡처 시 루트의 월드 회전

    private PlayerSkeletonSnapshot(Transform root, bool includeRootLocal)
    {
        rootCaptured = root;
        includeRootLocalTransform = includeRootLocal;

        worldRootPos = root.position;
        worldRootRot = root.rotation;

        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            bones.Add(new Bone
            {
                t = t,
                lp = t.localPosition,
                lr = t.localRotation,
                ls = t.localScale,
                active = t.gameObject.activeSelf
            });
        }
    }

    /// <summary>특정 루트(poseRoot) 이하 트랜스폼을 모두 스냅샷.</summary>
    public static PlayerSkeletonSnapshot Capture(Transform poseRoot, bool includeRootLocalTransform = true)
        => new PlayerSkeletonSnapshot(poseRoot, includeRootLocalTransform);

    /// <summary>
    /// 저장된 로컬 트랜스폼을 복원. 
    /// worldPos/worldRot를 null로 주면 현재 위치/회전을 유지하고, 값을 주면 그 값으로 설정.
    /// </summary>
    public void Apply(Transform poseRoot, Vector3? worldPosOverride = null, Quaternion? worldRotOverride = null)
    {
        if (poseRoot != rootCaptured)
        {
            // 다른 루트를 전달해도 구조가 동일하면 참조가 같을 확률이 높음.
            // 본문은 Transform 레퍼런스에 직접 적용하므로 poseRoot 매개변수는 단지 월드 위치/회전 적용용.
        }

        // 1) (선택) 루트 월드 변환부터 설정
        if (worldPosOverride.HasValue || worldRotOverride.HasValue)
        {
            poseRoot.SetPositionAndRotation(
                worldPosOverride ?? poseRoot.position,
                worldRotOverride ?? poseRoot.rotation
            );
        }

        // 2) 비활성/활성 상태 먼저 맞춤 (부모-자식 의존성 최소화를 위해 자식부터 활성해도 OK)
        for (int i = 0; i < bones.Count; i++)
        {
            var b = bones[i];
            if (!b.t) continue;
            if (b.t.gameObject.activeSelf != b.active)
                b.t.gameObject.SetActive(b.active);
        }

        // 3) 로컬 트랜스폼 복원
        for (int i = 0; i < bones.Count; i++)
        {
            var b = bones[i];
            if (!b.t) continue;

            // 루트의 local 변환을 건드리고 싶지 않으면(보통 월드 위치/회전을 따로 정하는 경우) skip
            if (!includeRootLocalTransform && b.t == rootCaptured) continue;

            b.t.localPosition = b.lp;
            b.t.localRotation = b.lr;
            b.t.localScale = b.ls;
        }
    }

    /// <summary>죽는 순간 위치 그대로 되살리고 싶을 때 사용.</summary>
    public void ApplyAtCapturedWorldPose(Transform poseRoot)
        => Apply(poseRoot, worldRootPos, worldRootRot);
}
