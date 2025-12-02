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
    private readonly Transform rootCaptured;
    private readonly bool includeRootLocalTransform;

    private readonly Vector3 worldRootPos;
    private readonly Quaternion worldRootRot;

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

    /// <summary>지정한 루트(poseRoot)의 현재 포즈를 스냅샷으로 저장합니다.</summary>
    public static PlayerSkeletonSnapshot Capture(Transform poseRoot, bool includeRootLocalTransform = true)
        => new PlayerSkeletonSnapshot(poseRoot, includeRootLocalTransform);

    /// <summary>
    /// 저장된 포즈를 지정한 루트에 적용합니다. 월드 위치나 회전을 덮어쓰실 수도 있습니다.
    /// </summary>
    public void Apply(Transform poseRoot, Vector3? worldPosOverride = null, Quaternion? worldRotOverride = null)
    {
        if (poseRoot != rootCaptured)
        {
            // 다른 루트에 적용하는 경우라면 좌표계 차이에 유의해 주세요.
        }

        if (worldPosOverride.HasValue || worldRotOverride.HasValue)
        {
            poseRoot.SetPositionAndRotation(
                worldPosOverride ?? poseRoot.position,
                worldRotOverride ?? poseRoot.rotation
            );
        }

        for (int i = 0; i < bones.Count; i++)
        {
            var b = bones[i];
            if (!b.t) continue;
            if (b.t.gameObject.activeSelf != b.active)
                b.t.gameObject.SetActive(b.active);
        }

        for (int i = 0; i < bones.Count; i++)
        {
            var b = bones[i];
            if (!b.t) continue;

            if (!includeRootLocalTransform && b.t == rootCaptured) continue;

            b.t.localPosition = b.lp;
            b.t.localRotation = b.lr;
            b.t.localScale = b.ls;
        }
    }

    /// <summary>스냅샷이 저장된 월드 위치와 회전을 그대로 적용합니다.</summary>
    public void ApplyAtCapturedWorldPose(Transform poseRoot)
        => Apply(poseRoot, worldRootPos, worldRootRot);
}
