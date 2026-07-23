using UnityEngine;
using System.Collections;

public class BossAttackSleepParalysis : MonoBehaviour
{
    [Header("Sleep Paralysis Settings")]
    public float eventInterval = 30.0f; // Happens every 30 seconds
    public float requiredStillTime = 3.0f; // Player must be completely still for 3 seconds to end it
    public float flashInterval = 1.5f;  // Flashes to SleepCam every 1.5 seconds
    public float speedMultiplier = 0.5f;

    [Header("References")]
    public AudioClip heartbeatSound;
    [SerializeField, Range(0f, 5f)] public float heartbeatVolume = 1.0f;
    public Camera sleepCam;
    public CanvasGroup fadeCanvasGroup;

    public static bool IsDebuffActive { get; private set; } = false;
    public static float ActiveSpeedMultiplier { get; private set; } = 1.0f;

    private float timer = 0f;
    private bool fightStarted = false;
    private AudioSource audioSource;
    private Camera mainCam;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (heartbeatSound == null)
        {
            heartbeatSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Heartbeat.wav");
        }
    }
#endif

    private void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound so it's loud and clear everywhere

        EnsureAudioClipsLoaded();

        // Create a fade canvas for the camera switches
        CreateFadeCanvas();
    }

    private void EnsureAudioClipsLoaded()
    {
#if UNITY_EDITOR
        if (heartbeatSound == null)
        {
            heartbeatSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Heartbeat.wav");
        }
#endif
        if (heartbeatSound == null) heartbeatSound = Resources.Load<AudioClip>("Heartbeat");
    }

    private void CreateFadeCanvas()
    {
        GameObject canvasObj = new GameObject("SleepParalysis_FadeCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // Ensure it's on top of everything

        fadeCanvasGroup = canvasObj.AddComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;
        fadeCanvasGroup.interactable = false;

        UnityEngine.UI.Image blackScreen = canvasObj.AddComponent<UnityEngine.UI.Image>();
        blackScreen.color = Color.black;
    }

    private void Start()
    {
        // Try to find SleepCam if not assigned
        if (sleepCam == null)
        {
            GameObject scObj = GameObject.Find("SleepCam");
            if (scObj != null) sleepCam = scObj.GetComponent<Camera>();
        }

        if (sleepCam != null)
        {
            sleepCam.gameObject.SetActive(false); // Ensure it's off initially
        }
    }

    public void ResetSleepParalysis()
    {
        fightStarted = false;
        timer = 0f;
        StopSleepParalysisEvent();
    }

    public void StopSleepParalysisEvent()
    {
        StopAllCoroutines();
        IsDebuffActive = false;
        ActiveSpeedMultiplier = 1.0f;

        if (SleepParalysisVignetteUI.Instance != null)
        {
            SleepParalysisVignetteUI.Instance.SetVignetteActive(false);
        }

        if (mainCam != null) mainCam.gameObject.SetActive(true);
        if (sleepCam != null) sleepCam.gameObject.SetActive(false);
        if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = 0f;
        Debug.Log("[SleepParalysis] Sleep paralysis event stopped & cleaned up!");
    }

    private void Update()
    {
        if (!fightStarted)
        {
            var tester = GetComponent<BossFightTester>();
            if (tester != null && tester.IsFightStarted)
            {
                fightStarted = true;
                timer = 0f; // Reset timer when fight starts
            }
            return;
        }

        if (IsDebuffActive) return; // Do not increment timer while event is active

        timer += Time.deltaTime;
        if (timer >= eventInterval)
        {
            timer = 0f;
            StartCoroutine(ExecuteSleepParalysisEvent());
        }
    }

    private Rigidbody GetActivePlayerRigidbody()
    {
        if (Nemuri.Core.CharacterSwapManager.Instance != null)
        {
            GameObject activePlayer = Nemuri.Core.CharacterSwapManager.Instance.GetActivePlayerObject();
            if (activePlayer != null)
            {
                if (activePlayer.transform.parent != null)
                {
                    var rb = activePlayer.transform.parent.GetComponent<Rigidbody>();
                    if (rb != null) return rb;
                }
                return activePlayer.GetComponent<Rigidbody>();
            }
        }
        return null;
    }

    private IEnumerator ExecuteSleepParalysisEvent()
    {
        Debug.Log("[SleepParalysis] 30-second interval hit! Triggering Unavoidable Sleep Paralysis! Stay still for 3 seconds to survive.");

        // Apply Debuff
        IsDebuffActive = true;
        ActiveSpeedMultiplier = speedMultiplier;

        if (SleepParalysisVignetteUI.Instance != null)
        {
            SleepParalysisVignetteUI.Instance.SetVignetteActive(true);
        }

        // Cache main camera
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null)
        {
            BossFightCamera bfc = FindAnyObjectByType<BossFightCamera>();
            if (bfc != null) mainCam = bfc.GetComponent<Camera>();
        }

        float elapsed = 0f;
        float nextFlashTime = 0f;
        float stillTimer = 0f;

        while (stillTimer < requiredStillTime)
        {
            elapsed += Time.deltaTime;

            Rigidbody playerRb = GetActivePlayerRigidbody();
            if (playerRb != null)
            {
                // Check horizontal velocity (ignoring Y for jumping/falling)
                Vector3 horizontalVel = new Vector3(playerRb.linearVelocity.x, 0f, playerRb.linearVelocity.z);
                if (horizontalVel.magnitude > 0.2f)
                {
                    stillTimer = 0f; // Player moved! Reset the still timer!
                }
                else
                {
                    stillTimer += Time.deltaTime; // Player is staying still!
                }
            }

            if (elapsed >= nextFlashTime)
            {
                nextFlashTime += flashInterval;
                StartCoroutine(CameraFlashRoutine());
            }

            yield return null;
        }

        Debug.Log("[SleepParalysis] Player stayed completely still for 3 seconds! Event ended.");

        // End Debuff
        IsDebuffActive = false;
        ActiveSpeedMultiplier = 1.0f;

        if (SleepParalysisVignetteUI.Instance != null)
        {
            SleepParalysisVignetteUI.Instance.SetVignetteActive(false);
        }

        // Ensure we end on main camera
        if (mainCam != null) mainCam.gameObject.SetActive(true);
        if (sleepCam != null) sleepCam.gameObject.SetActive(false);
        fadeCanvasGroup.alpha = 0f;
    }

    private void PlayCleanHeartbeat(AudioClip clip, float volume)
    {
        if (clip == null || volume <= 0f) return;

        // Dedicated 2D AudioSource GameObject so no other attack/script can interrupt or cut off the heartbeat
        GameObject soundObj = new GameObject("HeartbeatSFX_Player");
        AudioSource source = soundObj.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = Mathf.Clamp01(volume);
        source.pitch = 1.0f;
        source.spatialBlend = 0f; // Pure 2D stereo sound
        source.playOnAwake = false;
        source.Play();

        // Self-destruct after clip finishes naturally
        Destroy(soundObj, clip.length + 0.2f);
    }

    private IEnumerator CameraFlashRoutine()
    {
        EnsureAudioClipsLoaded();

        // 1. Play Heartbeat SFX once per camera flash on dedicated 2D AudioSource
        if (heartbeatSound != null)
        {
            PlayCleanHeartbeat(heartbeatSound, heartbeatVolume);
        }

        // 2. Locate cameras dynamically if not yet set
        if (sleepCam == null)
        {
            GameObject scObj = GameObject.Find("SleepCam");
            if (scObj != null) sleepCam = scObj.GetComponent<Camera>();
        }
        if (mainCam == null)
        {
            mainCam = Camera.main;
            if (mainCam == null)
            {
                BossFightCamera bfc = FindAnyObjectByType<BossFightCamera>();
                if (bfc != null) mainCam = bfc.GetComponent<Camera>();
            }
        }

        if (sleepCam == null || mainCam == null) yield break;

        float fadeSpeed = 8.0f; // Fast fade (0.125s)

        // Fade to black
        while (fadeCanvasGroup.alpha < 1f)
        {
            fadeCanvasGroup.alpha += Time.deltaTime * fadeSpeed;
            yield return null;
        }
        fadeCanvasGroup.alpha = 1f;

        // Switch to SleepCam
        mainCam.gameObject.SetActive(false);
        sleepCam.gameObject.SetActive(true);

        // Fade in from black to SleepCam
        while (fadeCanvasGroup.alpha > 0f)
        {
            fadeCanvasGroup.alpha -= Time.deltaTime * fadeSpeed;
            yield return null;
        }
        fadeCanvasGroup.alpha = 0f;

        // Hold on SleepCam for 0.4 seconds
        yield return new WaitForSeconds(0.4f);

        // Fade to black again
        while (fadeCanvasGroup.alpha < 1f)
        {
            fadeCanvasGroup.alpha += Time.deltaTime * fadeSpeed;
            yield return null;
        }
        fadeCanvasGroup.alpha = 1f;

        // Switch back to Main Cam
        sleepCam.gameObject.SetActive(false);
        mainCam.gameObject.SetActive(true);

        // Fade in from black to Main Cam
        while (fadeCanvasGroup.alpha > 0f)
        {
            fadeCanvasGroup.alpha -= Time.deltaTime * fadeSpeed;
            yield return null;
        }
        fadeCanvasGroup.alpha = 0f;
    }
}
