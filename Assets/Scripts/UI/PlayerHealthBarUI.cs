using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthBarUI : MonoBehaviour
{
    private static PlayerHealthBarUI instance;
    public static PlayerHealthBarUI Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByTypeAll<PlayerHealthBarUI>();
                if (instance == null)
                {
                    GameObject go = new GameObject("PlayerHealthBarUI");
                    instance = go.AddComponent<PlayerHealthBarUI>();
                }
            }
            return instance;
        }
    }

    [Header("Settings")]
    public string playerName = "PLAYER";
    public float smoothTrailSpeed = 1.0f;
    public float fadeDuration = 0.5f;

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
        // Default to shown but with default name
        ShowPlayerHealthBar(playerName, maxHealth);
    }

    private void Update()
    {
        if (!isVisible && (canvasGroup == null || canvasGroup.alpha <= 0.001f)) return;

        // Dynamically track character name swaps from CharacterSwapManager
        if (Nemuri.Core.CharacterSwapManager.Instance != null)
        {
            string currentName = Nemuri.Core.CharacterSwapManager.Instance.GetActiveCharacterName().ToUpper();
            if (playerName != currentName)
            {
                playerName = currentName;
                if (nameText != null) nameText.text = playerName;
            }
        }

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

    public void ShowPlayerHealthBar(string name, float maxHp)
    {
        playerName = name.Replace("(Clone)", "").ToUpper(); // Clean name for display
        maxHealth = maxHp;
        currentHealth = maxHp;
        targetFillAmount = 1f;
        trailFillAmount = 1f;

        if (nameText != null) nameText.text = playerName;
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

    public void HidePlayerHealthBar()
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
        GameObject canvasGo = new GameObject("PlayerHealthBarCanvas");
        canvasGo.transform.SetParent(transform);
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        canvasGroup = canvasGo.AddComponent<CanvasGroup>();

        // 2. Create Main Container Panel in the Top-Left of the screen
        GameObject panelGo = new GameObject("MainPanel");
        panelGo.transform.SetParent(canvasGo.transform);
        mainPanel = panelGo.AddComponent<RectTransform>();
        
        // Anchor to Top-Left
        mainPanel.anchorMin = new Vector2(0f, 1f);
        mainPanel.anchorMax = new Vector2(0f, 1f);
        mainPanel.pivot = new Vector2(0f, 1f);
        mainPanel.anchoredPosition = new Vector2(40f, -40f); // 40px right, 40px down
        mainPanel.sizeDelta = new Vector2(300f, 50f);        // Width: 300px, Height: 50px

        // 3. Create Name Text
        GameObject nameGo = new GameObject("PlayerNameText");
        nameGo.transform.SetParent(panelGo.transform);
        RectTransform nameRect = nameGo.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 1f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.pivot = new Vector2(0f, 1f);
        nameRect.anchoredPosition = new Vector2(5f, 0f); // Slight horizontal margin
        nameRect.sizeDelta = new Vector2(-10f, 20f);

        nameText = nameGo.AddComponent<Text>();
        Font font = Resources.Load<Font>("Spinnenkop DEMO");
        if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 14);
        nameText.font = font;
        nameText.text = playerName;
        nameText.fontSize = 14;
        nameText.fontStyle = FontStyle.Bold;
        nameText.alignment = TextAnchor.MiddleLeft;
        nameText.color = Color.white;

        // Shadow effect for text
        Shadow textShadow = nameGo.AddComponent<Shadow>();
        textShadow.effectColor = Color.black;
        textShadow.effectDistance = new Vector2(1f, -1f);

        // 4. Create Background Bar (dark grey)
        GameObject bgGo = new GameObject("BackgroundBar");
        bgGo.transform.SetParent(panelGo.transform);
        RectTransform bgRect = bgGo.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0f, 0f);
        bgRect.anchorMax = new Vector2(1f, 0.4f); // Takes bottom 40% of container
        bgRect.pivot = new Vector2(0f, 0f);
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

        // 6. Create Main Fill Bar (green)
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
        fillBarImg.color = new Color(0.2f, 0.75f, 0.3f, 1f); // Emerald green
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
