using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 플레이어와 카메라 사이의 벽을 투명하게 만들어 시야를 확보합니다.
/// </summary>
public class WallTransparency : MonoBehaviour
{
    public Transform player;
    public Camera mainCamera;
    public LayerMask wallLayer;
    public float transparency = 0.3f;
    public float checkRadius = 0.5f;

    private Dictionary<Renderer, Material[]> originalMaterials = new();
    private List<Renderer> currentlyTransparent = new();

    /// <summary>
    /// 카메라와 플레이어 사이를 검사해 가려지는 벽을 투명하게 처리합니다.
    /// </summary>
    private void Update()
    {
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

        if (Physics.SphereCast(mainCamera.transform.position, checkRadius, direction, out RaycastHit hit, distance, wallLayer))
        {
            Renderer rend = hit.collider.GetComponent<Renderer>();
            if (rend != null)
            {
                if (!originalMaterials.ContainsKey(rend))
                {
                    originalMaterials[rend] = rend.materials;
                }

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
