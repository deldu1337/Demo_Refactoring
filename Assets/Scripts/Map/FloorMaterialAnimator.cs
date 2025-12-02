using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class FloorMaterialAnimator : MonoBehaviour
{
    [Header("Frames (1)")]
    [Tooltip("지정된 순서로 사용할 바닥 텍스처 목록입니다")]
    public Texture2D[] textures;

    [Tooltip("필요할 때 사용할 스프라이트 목록입니다")]
    public Sprite[] sprites;

    [Header("Playback")]
    public float fps = 8f;
    public bool loop = true;
    public bool randomStartFrame = true;

    [Header("Auto Load ()")]
    public bool autoLoadFromResources = false;
    [Tooltip("Resources 폴더 안에서 텍스처를 불러올 경로입니다")]
    public string resourcesFolder = "Textures/Floor";
    [Tooltip("자동으로 불러올 파일 이름의 접두사입니다")]
    public string namePrefix = "Floor_";

    [Header("Shader Property")]
    [Tooltip("URP에서는 _BaseMap, Standard에서는 _MainTex를 사용합니다")]
    public string texturePropertyName = "_BaseMap";

    private Renderer rend;
    private MaterialPropertyBlock mpb;
    private int propId;
    private float timeAcc;
    private int index;

    /// <summary>
    /// 렌더러와 머티리얼 속성을 준비하고 필요한 경우 리소스에서 프레임을 불러옵니다.
    /// </summary>
    private void Awake()
    {
        rend = GetComponent<Renderer>();
        mpb = new MaterialPropertyBlock();
        propId = Shader.PropertyToID(texturePropertyName);

        if (autoLoadFromResources)
        {
            // 리소스에서 텍스처를 찾습니다
            var texAll = Resources.LoadAll<Texture2D>(resourcesFolder);
            textures = System.Array.FindAll(texAll, t => t.name.StartsWith(namePrefix));
            System.Array.Sort(textures, (a, b) => string.CompareOrdinal(a.name, b.name));

            // 텍스처가 없을 때 스프라이트를 대신 사용합니다
            if (textures == null || textures.Length == 0)
            {
                var sprAll = Resources.LoadAll<Sprite>(resourcesFolder);
                sprites = System.Array.FindAll(sprAll, s => s.name.StartsWith(namePrefix));
                System.Array.Sort(sprites, (a, b) => string.CompareOrdinal(a.name, b.name));
            }
        }

        int count = GetFrameCount();
        if (count == 0)
        {
            enabled = false;
            Debug.LogWarning($"{name}: FloorMaterialAnimator 프레임을 찾을 수 없어 비활성화합니다");
            return;
        }

        if (randomStartFrame)
        {
            index = Random.Range(0, count);
            timeAcc = Random.value / Mathf.Max(fps, 0.0001f);
        }

        ApplyFrame(index);
    }

    /// <summary>
    /// 누적된 시간을 기반으로 다음 프레임을 적용합니다.
    /// </summary>
    private void Update()
    {
        int count = GetFrameCount();
        if (count == 0 || fps <= 0f) return;

        timeAcc += Time.deltaTime;
        float frameDur = 1f / fps;

        while (timeAcc >= frameDur)
        {
            timeAcc -= frameDur;
            index++;
            if (index >= count)
            {
                if (loop)
                {
                    index = 0;
                }
                else
                {
                    index = count - 1;
                    enabled = false;
                    break;
                }
            }

            ApplyFrame(index);
        }
    }

    /// <summary>
    /// 사용할 수 있는 프레임의 개수를 반환합니다.
    /// </summary>
    private int GetFrameCount()
    {
        if (textures != null && textures.Length > 0) return textures.Length;
        if (sprites != null && sprites.Length > 0) return sprites.Length;
        return 0;
    }

    /// <summary>
    /// 인덱스에 해당하는 텍스처를 반환합니다.
    /// </summary>
    private Texture GetFrameTexture(int i)
    {
        if (textures != null && textures.Length > 0) return textures[i];
        if (sprites != null && sprites.Length > 0)
        {
            // 스프라이트에서 텍스처를 가져옵니다
            return sprites[i] ? sprites[i].texture : null;
        }
        return null;
    }

    /// <summary>
    /// 선택한 프레임 텍스처를 머티리얼에 적용합니다.
    /// </summary>
    private void ApplyFrame(int i)
    {
        var tex = GetFrameTexture(i);
        if (tex == null) return;

        // 기본 맵 속성에 텍스처를 설정합니다
        rend.GetPropertyBlock(mpb);
        mpb.SetTexture(propId, tex);
        rend.SetPropertyBlock(mpb);

        // 표준 셰이더 호환을 위해 메인 텍스처에도 동일하게 설정합니다
        int mainId = Shader.PropertyToID("_MainTex");
        rend.GetPropertyBlock(mpb);
        mpb.SetTexture(mainId, tex);
        rend.SetPropertyBlock(mpb);
    }
}
