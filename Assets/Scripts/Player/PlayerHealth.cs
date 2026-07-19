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

    private void Die()
    {
        Debug.Log("[PlayerHealth] " + gameObject.name + " died!");
    }
}
