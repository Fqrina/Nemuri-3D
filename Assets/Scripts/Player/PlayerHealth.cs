using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Settings")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    private void OnEnable()
    {
        // Update/Show in UI when this character becomes active
        if (PlayerHealthBarUI.Instance != null)
        {
            PlayerHealthBarUI.Instance.ShowPlayerHealthBar(gameObject.name, maxHealth);
            PlayerHealthBarUI.Instance.UpdateHealth(currentHealth);
        }
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(0f, currentHealth);
        Debug.Log("[PlayerHealth] " + gameObject.name + " took " + damage + " damage. Current HP: " + currentHealth);

        if (PlayerHealthBarUI.Instance != null)
        {
            PlayerHealthBarUI.Instance.UpdateHealth(currentHealth);
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(maxHealth, currentHealth);
        Debug.Log("[PlayerHealth] " + gameObject.name + " healed for " + amount + ". Current HP: " + currentHealth);

        if (PlayerHealthBarUI.Instance != null)
        {
            PlayerHealthBarUI.Instance.UpdateHealth(currentHealth);
        }
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        if (PlayerHealthBarUI.Instance != null)
        {
            PlayerHealthBarUI.Instance.ShowPlayerHealthBar(gameObject.name, maxHealth);
            PlayerHealthBarUI.Instance.UpdateHealth(currentHealth);
        }
    }

    private static CanvasGroup deathFadeGroup;

    private void CreateDeathFadeCanvas()
    {
        if (deathFadeGroup != null) return;

        GameObject canvasObj = new GameObject("DeathFadeCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        deathFadeGroup = canvasObj.AddComponent<CanvasGroup>();
        deathFadeGroup.alpha = 0f;
        deathFadeGroup.blocksRaycasts = false;
        deathFadeGroup.interactable = false;

        UnityEngine.UI.Image blackScreen = canvasObj.AddComponent<UnityEngine.UI.Image>();
        blackScreen.color = Color.black;
        Object.DontDestroyOnLoad(canvasObj);
    }

    private void Die()
    {
        Debug.Log("[PlayerHealth] " + gameObject.name + " died! Triggering death sequence (Fade -> Teleport -> Reset)...");
        CreateDeathFadeCanvas();
        StartCoroutine(DeathSequenceRoutine());
    }

    private System.Collections.IEnumerator DeathSequenceRoutine()
    {
        float fadeSpeed = 3.5f;

        // 1. Fade OUT to black
        while (deathFadeGroup.alpha < 1f)
        {
            deathFadeGroup.alpha += Time.deltaTime * fadeSpeed;
            yield return null;
        }
        deathFadeGroup.alpha = 1f;

        // 2. Teleport player to "Spawn" object position
        Transform spawnPoint = GetSpawnTransform();
        if (spawnPoint != null)
        {
            TeleportPlayerTo(spawnPoint.position);
        }

        // 3. Reset all player characters' health
        PlayerHealth[] allHealths = Resources.FindObjectsOfTypeAll<PlayerHealth>();
        foreach (var ph in allHealths)
        {
            if (ph != null) ph.ResetHealth();
        }

        // 4. Reset Boss HP & Attack Loop
        BossFightTester tester = Object.FindFirstObjectByType<BossFightTester>();
        if (tester == null)
        {
            BossFightTester[] testers = Resources.FindObjectsOfTypeAll<BossFightTester>();
            foreach (var t in testers)
            {
                if (t.gameObject.scene.isLoaded)
                {
                    tester = t;
                    break;
                }
            }
        }

        if (tester != null)
        {
            tester.ResetBossFight();
        }

        yield return new WaitForSeconds(0.2f);

        // 5. Fade IN back from black
        while (deathFadeGroup.alpha > 0f)
        {
            deathFadeGroup.alpha -= Time.deltaTime * fadeSpeed;
            yield return null;
        }
        deathFadeGroup.alpha = 0f;
    }

    private Transform GetSpawnTransform()
    {
        GameObject spawnObj = GameObject.Find("Spawn");
        if (spawnObj == null) spawnObj = GameObject.Find("spawn");
        if (spawnObj == null)
        {
            foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go.name.ToLower().Equals("spawn") && go.scene.isLoaded)
                {
                    spawnObj = go;
                    break;
                }
            }
        }
        return spawnObj != null ? spawnObj.transform : null;
    }

    private void TeleportPlayerTo(Vector3 targetPos)
    {
        // Teleport root parent ("Walking Player") if present
        if (transform.parent != null)
        {
            transform.parent.position = targetPos;
            Rigidbody parentRb = transform.parent.GetComponent<Rigidbody>();
            if (parentRb != null)
            {
                parentRb.linearVelocity = Vector3.zero;
                parentRb.angularVelocity = Vector3.zero;
            }
        }

        transform.position = targetPos;
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}
