using UnityEngine;
using UnityEngine.UI;

public class LaserBeamRedScreenUI : MonoBehaviour
{
    private static LaserBeamRedScreenUI instance;
    public static LaserBeamRedScreenUI Instance
    {
        get
        {
            if (instance == null)
            {
                LaserBeamRedScreenUI[] objs = Resources.FindObjectsOfTypeAll<LaserBeamRedScreenUI>();
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
                    GameObject go = new GameObject("LaserBeamRedScreenUI");
                    instance = go.AddComponent<LaserBeamRedScreenUI>();
                }
            }
            return instance;
        }
    }

    private Canvas canvas;
    private Image redOverlayImage;
    private bool isActive = false;
    private float targetAlpha = 0f;
    private float currentAlpha = 0f;
    private float fadeSpeed = 8.0f;

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
        canvas.sortingOrder = 860; // Render on top of vignette and normal HUD

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        GameObject imgObj = new GameObject("RedScreenOverlay");
        imgObj.transform.SetParent(transform, false);

        redOverlayImage = imgObj.AddComponent<Image>();
        RectTransform rt = redOverlayImage.rectTransform;
        rt.anchorMin = Vector3.zero;
        rt.anchorMax = Vector3.one;
        rt.sizeDelta = Vector2.zero;

        // Solid red tint texture
        Texture2D redTex = new Texture2D(1, 1);
        redTex.SetPixel(0, 0, Color.white);
        redTex.Apply();

        redOverlayImage.sprite = Sprite.Create(redTex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        redOverlayImage.color = new Color(0.95f, 0.05f, 0.05f, 0f);
        redOverlayImage.raycastTarget = false;
    }

    private void Update()
    {
        if (redOverlayImage == null) return;

        targetAlpha = isActive ? 0.35f : 0f;
        currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, Time.deltaTime * fadeSpeed);
        
        // Rapid subtle pulse while active
        float pulse = isActive ? Mathf.Sin(Time.time * 15f) * 0.05f : 0f;
        float finalAlpha = Mathf.Clamp01(currentAlpha + pulse);

        Color c = redOverlayImage.color;
        c.a = finalAlpha;
        redOverlayImage.color = c;
    }

    public void SetRedScreenActive(bool active)
    {
        isActive = active;
    }
}
