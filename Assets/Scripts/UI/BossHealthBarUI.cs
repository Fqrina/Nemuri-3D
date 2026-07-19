using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BossHealthBarUI : MonoBehaviour
{
    private static BossHealthBarUI instance;
    public static BossHealthBarUI Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByTypeAll<BossHealthBarUI>();
                if (instance == null)
                {
                    GameObject go = new GameObject("BossHealthBarUI");
                    instance = go.AddComponent<BossHealthBarUI>();
                }
            }
            return instance;
        }
    }

    [Header("Settings")]
    public string bossName = "EVIL RABBIT";
    public float smoothTrailSpeed = 1.0f;
    public float fadeDuration = 0.8f;

    // UI References created dynamically
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private RectTransform mainPanel;
    private Text nameText;
    private Image bgBar;
    private RectTransform trailBarRect;
    private RectTransform fillBarRect;

    private float maxHealth = 100f;
    private float currentHealth = 100f;
    private float targetFillAmount = 1f;
    private float trailFillAmount = 1f;
    private bool isVisible = false;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        CreateUIElements();
    }

    private void Start()
    {
        // Start hidden
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }

    private void Update()
    {
        if (!isVisible && (canvasGroup == null || canvasGroup.alpha <= 0.001f)) return;

        // Smoothly update the trailing damage bar (white/yellow catches up slowly)
        if (trailFillAmount > targetFillAmount)
        {
            trailFillAmount = Mathf.MoveTowards(trailFillAmount, targetFillAmount, smoothTrailSpeed * Time.deltaTime);
            SetBarFillWidth(trailBarRect, trailFillAmount);
        }
        else
        {
            trailFillAmount = targetFillAmount;
            SetBarFillWidth(trailBarRect, trailFillAmount);
        }
    }

    public void ShowBossHealthBar(string name, float maxHp)
    {
        bossName = name;
        maxHealth = maxHp;
        currentHealth = maxHp;
        targetFillAmount = 1f;
        trailFillAmount = 1f;

        if (nameText != null) nameText.text = bossName;
        SetBarFillWidth(fillBarRect, 1f);
        SetBarFillWidth(trailBarRect, 1f);

        isVisible = true;
        StopAllCoroutines();
        StartCoroutine(FadeRoutine(1f));
    }

    public void UpdateHealth(float currentHp)
    {
        currentHealth = Mathf.Clamp(currentHp, 0f, maxHealth);
        targetFillAmount = currentHealth / maxHealth;
        SetBarFillWidth(fillBarRect, targetFillAmount);
    }

    private void SetBarFillWidth(RectTransform barRect, float fillAmount)
    {
        if (barRect != null)
        {
            Vector2 anchorMax = barRect.anchorMax;
            anchorMax.x = fillAmount;
            barRect.anchorMax = anchorMax;
        }
    }

    public void HideBossHealthBar()
    {
        isVisible = false;
        StopAllCoroutines();
        StartCoroutine(FadeRoutine(0f));
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        if (canvasGroup == null) yield break;

        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }

    private void CreateUIElements()
    {
        // 1. Create Canvas
        GameObject canvasGo = new GameObject("BossHealthBarCanvas");
        canvasGo.transform.SetParent(transform);
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        canvasGroup = canvasGo.AddComponent<CanvasGroup>();

        // 2. Create Main Container Panel at the bottom center of the screen
        GameObject panelGo = new GameObject("MainPanel");
        panelGo.transform.SetParent(canvasGo.transform);
        mainPanel = panelGo.AddComponent<RectTransform>();
        
        // Anchor to bottom center
        mainPanel.anchorMin = new Vector2(0.5f, 0f);
        mainPanel.anchorMax = new Vector2(0.5f, 0f);
        mainPanel.pivot = new Vector2(0.5f, 0f);
        mainPanel.anchoredPosition = new Vector2(0f, 60f); // 60 pixels above bottom
        mainPanel.sizeDelta = new Vector2(650f, 60f);      // Width: 650px, Height: 60px

        // 3. Create Name Text
        GameObject nameGo = new GameObject("BossNameText");
        nameGo.transform.SetParent(panelGo.transform);
        RectTransform nameRect = nameGo.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.5f, 1f);
        nameRect.anchorMax = new Vector2(0.5f, 1f);
        nameRect.pivot = new Vector2(0.5f, 1f);
        nameRect.anchoredPosition = new Vector2(0f, 10f); // 10px above the bar
        nameRect.sizeDelta = new Vector2(600f, 25f);

        nameText = nameGo.AddComponent<Text>();
        nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (nameText.font == null)
        {
            nameText.font = Font.CreateDynamicFontFromOSFont("Arial", 18);
        }
        nameText.text = bossName;
        nameText.fontSize = 18;
        nameText.fontStyle = FontStyle.Bold;
        nameText.alignment = TextAnchor.MiddleCenter;
        nameText.color = Color.white;

        // Shadow effect for the text to make it look premium
        Shadow textShadow = nameGo.AddComponent<Shadow>();
        textShadow.effectColor = Color.black;
        textShadow.effectDistance = new Vector2(1.5f, -1.5f);

        // 4. Create Background Bar (dark grey)
        GameObject bgGo = new GameObject("BackgroundBar");
        bgGo.transform.SetParent(panelGo.transform);
        RectTransform bgRect = bgGo.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0f, 0f);
        bgRect.anchorMax = new Vector2(1f, 0.5f); // Takes bottom half of container
        bgRect.pivot = new Vector2(0.5f, 0.5f);
        bgRect.anchoredPosition = Vector2.zero;
        bgRect.sizeDelta = Vector2.zero;

        bgBar = bgGo.AddComponent<Image>();
        bgBar.color = new Color(0.1f, 0.1f, 0.1f, 0.85f); // Semi-transparent dark grey

        // 5. Create Trailing Damage Bar (yellow/white)
        GameObject trailGo = new GameObject("TrailBar");
        trailGo.transform.SetParent(bgGo.transform);
        trailBarRect = trailGo.AddComponent<RectTransform>();
        
        // Pivot to left edge so it scales from left to right
        trailBarRect.anchorMin = Vector2.zero;
        trailBarRect.anchorMax = Vector2.one;
        trailBarRect.pivot = new Vector2(0f, 0.5f);
        trailBarRect.anchoredPosition = Vector2.zero;
        trailBarRect.sizeDelta = Vector2.zero;

        Image trailBarImg = trailGo.AddComponent<Image>();
        trailBarImg.color = new Color(0.85f, 0.65f, 0.15f, 1f); // Dark gold/yellow

        // 6. Create Main Fill Bar (red)
        GameObject fillGo = new GameObject("FillBar");
        fillGo.transform.SetParent(bgGo.transform);
        fillBarRect = fillGo.AddComponent<RectTransform>();
        
        // Pivot to left edge so it scales from left to right
        fillBarRect.anchorMin = Vector2.zero;
        fillBarRect.anchorMax = Vector2.one;
        fillBarRect.pivot = new Vector2(0f, 0.5f);
        fillBarRect.anchoredPosition = Vector2.zero;
        fillBarRect.sizeDelta = Vector2.zero;

        Image fillBarImg = fillGo.AddComponent<Image>();
        fillBarImg.color = new Color(0.8f, 0.05f, 0.05f, 1f); // Crimson red
    }

    private static T FindFirstObjectByTypeAll<T>() where T : Component
    {
        T[] objs = Resources.FindObjectsOfTypeAll<T>();
        foreach (T obj in objs)
        {
            if (obj.gameObject.scene.isLoaded)
            {
                return obj;
            }
        }
        return null;
    }
}
