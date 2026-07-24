using UnityEngine;
using UnityEngine.InputSystem;

public class BossFightTester : MonoBehaviour
{
    [Header("Settings")]
    public string bossName = "EVIL RABBIT";
    public float maxHealth = 100f;
    public float triggerDistance = 45f;
    public bool manualStartOnly = false;

    [Header("Boss Music Settings (0 to 5 = 0% to 500% Volume)")]
    [SerializeField] public AudioClip bossMusic;
    [SerializeField, Range(0f, 5f)] public float musicVolume = 1.0f;

    private Transform player;
    private float currentHealth;
    private bool fightStarted = false;
    private bool defeated = false;
    private AudioSource musicAudioSource;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (bossMusic == null)
        {
            bossMusic = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/BossMusic.wav");
            if (bossMusic == null)
                bossMusic = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/BossMusic.mp3");
            if (bossMusic == null)
                bossMusic = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/MindlitBg.mp3");
        }
    }
#endif

    private void Start()
    {
        currentHealth = maxHealth;
        FindPlayer();

        // If in chpt3 scene with Chapter3IntroController, set manualStartOnly = true
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "chpt3")
        {
            manualStartOnly = true;
        }

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

        if (!fightStarted && !manualStartOnly)
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

    public void StartFightFromCutscene()
    {
        manualStartOnly = false;
        StartFight();
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

    public bool IsFightStarted => fightStarted;

    private void StartFight()
    {
        fightStarted = true;
        Debug.Log("[BossFightTester] Boss fight started! Press O to deal damage.");

        if (BossFightManager.Instance != null)
        {
            BossFightManager.Instance.isBossFightActive = true;
        }

        if (PlayerHealthBarUI.Instance != null && player != null)
        {
            PlayerHealthBarUI.Instance.ShowPlayerHealthBar(player.gameObject.name, 100f);
        }

        BossHealthBarUI.Instance.ShowBossHealthBar(bossName, maxHealth);

        PlayBossMusic();

        // Dynamically attach and start BossAttackController
        var attackCtrl = GetComponent<BossAttackController>();
        if (attackCtrl == null)
        {
            attackCtrl = gameObject.AddComponent<BossAttackController>();
        }
        attackCtrl.StartBossAttackLoop();
    }

    private void PlayBossMusic()
    {
        if (musicAudioSource == null)
        {
            musicAudioSource = gameObject.AddComponent<AudioSource>();
            musicAudioSource.playOnAwake = false;
            musicAudioSource.loop = true;
            musicAudioSource.spatialBlend = 0f; // 2D BGM
        }

        if (bossMusic == null)
        {
#if UNITY_EDITOR
            bossMusic = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/BossMusic.wav");
            if (bossMusic == null) bossMusic = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/BossMusic.mp3");
            if (bossMusic == null) bossMusic = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/MindlitBg.mp3");
#endif
            if (bossMusic == null) bossMusic = Resources.Load<AudioClip>("BossMusic");
        }

        if (bossMusic != null)
        {
            musicAudioSource.clip = bossMusic;
            musicAudioSource.volume = musicVolume;
            musicAudioSource.Play();
            Debug.Log("[BossFightTester] Playing BossMusic BGM: " + bossMusic.name);
        }
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

    public void ResetBossFight()
    {
        defeated = false;
        fightStarted = false;
        currentHealth = maxHealth;
        Debug.Log("[BossFightTester] Boss fight reset! Boss HP restored to full (" + maxHealth + ").");
        
        if (musicAudioSource != null && musicAudioSource.isPlaying)
        {
            musicAudioSource.Stop();
        }

        var attackCtrl = GetComponent<BossAttackController>();
        if (attackCtrl != null)
        {
            attackCtrl.ResetAttackLoop();
        }

        if (BossHealthBarUI.Instance != null)
        {
            BossHealthBarUI.Instance.ShowBossHealthBar(bossName, maxHealth);
            BossHealthBarUI.Instance.UpdateHealth(currentHealth);
        }
    }

    private void DefeatBoss()
    {
        defeated = true;
        Debug.Log("[BossFightTester] Boss defeated!");

        if (musicAudioSource != null && musicAudioSource.isPlaying)
        {
            musicAudioSource.Stop();
        }

        BossHealthBarUI.Instance.HideBossHealthBar();
    }
}
