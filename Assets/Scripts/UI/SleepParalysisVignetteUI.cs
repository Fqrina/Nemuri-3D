using UnityEngine;
using UnityEngine.UI;

public class SleepParalysisVignetteUI : MonoBehaviour
{
    private static SleepParalysisVignetteUI instance;
    public static SleepParalysisVignetteUI Instance
    {
        get
        {
            if (instance == null)
            {
                SleepParalysisVignetteUI[] objs = Resources.FindObjectsOfTypeAll<SleepParalysisVignetteUI>();
                foreach (var obj in objs)
                {
                    if (obj.gameObject.scene.isLoaded)
                    {
                        instance = obj;
                        break;
                    }
                }
                if (instance == null)
                {
                    GameObject go = new GameObject("SleepParalysisVignetteUI");
                    instance = go.AddComponent<SleepParalysisVignetteUI>();
                }
            }
            return instance;
        }
    }

    private Canvas canvas;
    private Image vignetteImage;
    private bool isActive = false;
    private float targetAlpha = 0f;
    private float currentAlpha = 0f;
    private float fadeSpeed = 3.0f;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        CreateUI();
    }

    private void CreateUI()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 850; // Above normal HUD, below dialogue

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        GameObject imgObj = new GameObject("VignetteImage");
        imgObj.transform.SetParent(transform, false);

        vignetteImage = imgObj.AddComponent<Image>();
        RectTransform rt = vignetteImage.rectTransform;
        rt.anchorMin = Vector3.zero;
        rt.anchorMax = Vector3.one;
        rt.sizeDelta = Vector2.zero;

        // Generate procedural vignette texture with dark purple/black radial gradient
        Texture2D vignetteTex = CreateVignetteTexture(256, 256);
        Sprite vignetteSprite = Sprite.Create(vignetteTex, new Rect(0, 0, vignetteTex.width, vignetteTex.height), new Vector2(0.5f, 0.5f));
        vignetteImage.sprite = vignetteSprite;
        vignetteImage.color = new Color(0.1f, 0.02f, 0.15f, 0f);
        vignetteImage.raycastTarget = false;
    }

    private Texture2D CreateVignetteTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(width * 0.5f, height * 0.5f);
        float maxDist = center.magnitude;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float normDist = Mathf.Clamp01(dist / maxDist);
                
                // Exponential curve: center is transparent, outer edges are dark
                float alpha = Mathf.Pow(normDist, 2.2f);
                tex.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
            }
        }
        tex.Apply();
        return tex;
    }

    private void Update()
    {
        if (vignetteImage == null) return;

        targetAlpha = isActive ? 0.92f : 0f;
        currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, Time.deltaTime * fadeSpeed);
        
        // Add subtle pulse when active
        float pulse = isActive ? Mathf.Sin(Time.time * 4f) * 0.05f : 0f;
        float finalAlpha = Mathf.Clamp01(currentAlpha + pulse);

        Color c = vignetteImage.color;
        c.a = finalAlpha;
        vignetteImage.color = c;
    }

    public void SetVignetteActive(bool active)
    {
        isActive = active;
    }
}
