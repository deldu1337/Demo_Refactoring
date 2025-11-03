using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class FloorMaterialAnimator : MonoBehaviour
{
    [Header("Frames (택1)")]
    [Tooltip("직접 텍스처 배열로 재생")]
    public Texture2D[] textures;

    [Tooltip("스프라이트 배열로 재생(내부에서 texture 사용)")]
    public Sprite[] sprites;

    [Header("Playback")]
    public float fps = 8f;
    public bool loop = true;
    public bool randomStartFrame = true;

    [Header("Auto Load (선택)")]
    public bool autoLoadFromResources = false;
    [Tooltip("Resources 폴더 경로 (예: Textures/Floor)")]
    public string resourcesFolder = "Textures/Floor";
    [Tooltip("이 접두사로 시작하는 에셋만 로드 (예: Floor_)")]
    public string namePrefix = "Floor_";

    [Header("Shader Property")]
    [Tooltip("URP/Lit는 _BaseMap, 내장 Standard는 _MainTex")]
    public string texturePropertyName = "_BaseMap";

    private Renderer rend;
    private MaterialPropertyBlock mpb;
    private int propId;
    private float timeAcc;
    private int index;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        mpb = new MaterialPropertyBlock();
        propId = Shader.PropertyToID(texturePropertyName);

        if (autoLoadFromResources)
        {
            // 1) 텍스처 우선 로드
            var texAll = Resources.LoadAll<Texture2D>(resourcesFolder);
            textures = System.Array.FindAll(texAll, t => t.name.StartsWith(namePrefix));
            System.Array.Sort(textures, (a, b) => string.CompareOrdinal(a.name, b.name));

            // 2) 텍스처가 없으면 스프라이트 로드 시도
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
            Debug.LogWarning($"{name}: FloorMaterialAnimator frames 없음 → 비활성화");
            return;
        }

        if (randomStartFrame)
        {
            index = Random.Range(0, count);
            timeAcc = Random.value / Mathf.Max(fps, 0.0001f);
        }

        ApplyFrame(index);
    }

    void Update()
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
                if (loop) index = 0;
                else { index = count - 1; enabled = false; break; }
            }
            ApplyFrame(index);
        }
    }

    int GetFrameCount()
    {
        if (textures != null && textures.Length > 0) return textures.Length;
        if (sprites != null && sprites.Length > 0) return sprites.Length;
        return 0;
    }

    Texture GetFrameTexture(int i)
    {
        if (textures != null && textures.Length > 0) return textures[i];
        if (sprites != null && sprites.Length > 0)
        {
            // 스프라이트 시트일 경우에도 통 텍스처를 사용
            return sprites[i] ? sprites[i].texture : null;
        }
        return null;
    }

    void ApplyFrame(int i)
    {
        var tex = GetFrameTexture(i);
        if (tex == null) return;

        // _BaseMap 세팅
        rend.GetPropertyBlock(mpb);
        mpb.SetTexture(propId, tex);
        rend.SetPropertyBlock(mpb);

        // 표준 셰이더 호환 (_MainTex)도 같이 세팅(선택)
        int mainId = Shader.PropertyToID("_MainTex");
        rend.GetPropertyBlock(mpb);
        mpb.SetTexture(mainId, tex);
        rend.SetPropertyBlock(mpb);
    }
}
