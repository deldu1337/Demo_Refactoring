using UnityEngine;
using System.Collections.Generic;

public class WallTransparency : MonoBehaviour
{
    public Transform player;              // 플레이어 Transform
    public Camera mainCamera;             // 메인 카메라
    public LayerMask wallLayer;           // 벽 레이어
    public float transparency = 0.3f;     // 투명도
    public float checkRadius = 0.5f;      // 스피어캐스트 반지름

    private Dictionary<Renderer, Material[]> originalMaterials = new();
    private List<Renderer> currentlyTransparent = new();

    void Update()
    {
        // 이전 투명 처리 초기화
        foreach (var rend in currentlyTransparent)
        {
            if (rend != null && originalMaterials.ContainsKey(rend))
            {
                rend.materials = originalMaterials[rend];
            }
        }
        currentlyTransparent.Clear();

        Vector3 direction = player.position - mainCamera.transform.position;
        float distance = direction.magnitude;

        // SphereCast로 카메라 → 플레이어 사이 벽 감지
        if (Physics.SphereCast(mainCamera.transform.position, checkRadius, direction, out RaycastHit hit, distance, wallLayer))
        {
            Renderer rend = hit.collider.GetComponent<Renderer>();
            if (rend != null)
            {
                // 원래 재질 저장
                if (!originalMaterials.ContainsKey(rend))
                {
                    originalMaterials[rend] = rend.materials;
                }

                // 투명 재질로 변경
                Material[] mats = rend.materials;
                for (int i = 0; i < mats.Length; i++)
                {
                    Color c = mats[i].color;
                    c.a = transparency;
                    mats[i].color = c;
                    mats[i].SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mats[i].SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mats[i].SetInt("_ZWrite", 0);
                    mats[i].DisableKeyword("_ALPHATEST_ON");
                    mats[i].EnableKeyword("_ALPHABLEND_ON");
                    mats[i].DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mats[i].renderQueue = 3000;
                }
                rend.materials = mats;

                currentlyTransparent.Add(rend);
            }
        }
    }
}
