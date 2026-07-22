using UnityEngine;
using UnityEngine.InputSystem;

public class BossFightTester : MonoBehaviour
{
    [Header("Settings")]
    public string bossName = "EVIL RABBIT";
    public float maxHealth = 100f;
    public float triggerDistance = 45f;

    private Transform player;
    private float currentHealth;
    private bool fightStarted = false;
    private bool defeated = false;

    private void Start()
    {
        currentHealth = maxHealth;
        FindPlayer();

        // Dynamically instantiate BossFightManager if not already present
        if (BossFightManager.Instance == null)
        {
            GameObject bfm = new GameObject("BossFightManager");
            bfm.AddComponent<BossFightManager>();
        }
    }

    private void Update()
    {
        if (defeated) return;

        if (!fightStarted)
        {
            FindPlayer();
            if (player != null)
            {
                float distance = Vector3.Distance(transform.position, player.position);
                if (distance <= triggerDistance)
                {
                    StartFight();
                }
            }
        }
    }

    private void FindPlayer()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                
                // Dynamically attach PlayerHealth component to the player
                var ph = playerObj.GetComponent<PlayerHealth>();
                if (ph == null)
                {
                    ph = playerObj.AddComponent<PlayerHealth>();
                    ph.maxHealth = 100f;
                }
            }
        }
    }

    private void StartFight()
    {
        fightStarted = true;
        Debug.Log("[BossFightTester] Boss fight started! Press O to deal damage.");
        BossHealthBarUI.Instance.ShowBossHealthBar(bossName, maxHealth);
    }

    public void TakeDamage(float amount)
    {
        if (defeated) return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(0f, currentHealth);
        Debug.Log("[BossFightTester] Boss took " + amount + " damage. Current HP: " + currentHealth);

        BossHealthBarUI.Instance.UpdateHealth(currentHealth);

        if (currentHealth <= 0f)
        {
            DefeatBoss();
        }
    }

    private void DefeatBoss()
    {
        defeated = true;
        Debug.Log("[BossFightTester] Boss defeated!");
        BossHealthBarUI.Instance.HideBossHealthBar();
    }
}
