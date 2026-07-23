using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BossAttackController : MonoBehaviour
{
    public enum AttackOrderMode
    {
        Sequential,
        Random
    }

    public static BossAttackController Instance { get; private set; }

    [Header("Attack Cycle Settings")]
    public AttackOrderMode attackOrder = AttackOrderMode.Sequential;
    public float initialDelay = 3.0f;
    public float minIntervalBetweenAttacks = 4.0f;
    public float maxIntervalBetweenAttacks = 7.0f;

    [Header("Attack Modules (Auto-Populated if Empty)")]
    public List<BossAttackModule> attackModules = new List<BossAttackModule>();

    [Header("Targets")]
    public Transform bossTransform;
    public Transform playerTargetTransform;

    private bool isExecutingAttack = false;
    private bool fightStarted = false;
    private int currentSequentialIndex = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        EnsureAttackModulesAttached();
    }

    private void Start()
    {
        bossTransform = transform;
        FindPlayerTarget();
    }

    private void EnsureAttackModulesAttached()
    {
        attackModules.Clear();

        // 1. Intrusive Thoughts
        var m1 = GetComponent<BossAttackIntrusiveThoughts>();
        if (m1 == null) m1 = gameObject.AddComponent<BossAttackIntrusiveThoughts>();
        attackModules.Add(m1);

        // 2. Laser Beam
        var m2 = GetComponent<BossAttackLaserBeam>();
        if (m2 == null) m2 = gameObject.AddComponent<BossAttackLaserBeam>();
        attackModules.Add(m2);

        // 3. Brain Wave
        var m4 = GetComponent<BossAttackBrainWave>();
        if (m4 == null) m4 = gameObject.AddComponent<BossAttackBrainWave>();
        attackModules.Add(m4);
    }

    private void Update()
    {
        FindPlayerTarget();

        // Check if boss fight has started (either via BossFightTester or proximity)
        if (!fightStarted)
        {
            var tester = GetComponent<BossFightTester>();
            if (tester != null && tester.IsFightStarted)
            {
                StartBossAttackLoop();
            }
            else if (playerTargetTransform != null)
            {
                float dist = Vector3.Distance(transform.position, playerTargetTransform.position);
                if (dist <= 45.0f)
                {
                    StartBossAttackLoop();
                }
            }
        }
    }

    public void StartBossAttackLoop()
    {
        if (fightStarted) return;
        fightStarted = true;
        Debug.Log("[BossAttackController] Boss attack loop started!");
        StartCoroutine(BossAttackLoopRoutine());
    }

    public void ResetAttackLoop()
    {
        StopAllCoroutines();
        isExecutingAttack = false;
        fightStarted = false;
        currentSequentialIndex = 0;

        // Reset Sleep Paralysis independent event
        var sleepPar = GetComponent<BossAttackSleepParalysis>();
        if (sleepPar != null)
        {
            sleepPar.ResetSleepParalysis();
        }

        // Hide eye objects if laser was charging/firing
        var laserBeam = GetComponent<BossAttackLaserBeam>();
        if (laserBeam != null)
        {
            Transform eyeL = bossTransform.Find("LaserEyeL");
            Transform eyeR = bossTransform.Find("LaserEyeR");
            if (eyeL != null) eyeL.gameObject.SetActive(false);
            if (eyeR != null) eyeR.gameObject.SetActive(false);
        }

        // Destroy any active attack visual objects in the scene instantly
        CleanupActiveAttackObjects();

        // Stop all audio on attack modules
        foreach (var audio in GetComponentsInChildren<AudioSource>())
        {
            if (audio != null) audio.Stop();
        }

        Debug.Log("[BossAttackController] Attack loop reset & all ongoing attacks destroyed.");
    }

    private void CleanupActiveAttackObjects()
    {
        string[] attackObjectNames = new string[]
        {
            "IntrusiveThoughts_GroundIndicator",
            "IntrusiveThoughts_Sphere",
            "ImpactWave",
            "LaserBeam_GroundPathIndicator",
            "LaserBeam_EnergyVisual",
            "BrainWave_4TierWifiArc",
            "SleepParalysis_WarningDisc",
            "SleepParalysis_DarkPulseWave"
        };

        foreach (var objName in attackObjectNames)
        {
            GameObject[] objs = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var go in objs)
            {
                if (go != null && go.name.Contains(objName))
                {
                    Destroy(go);
                }
            }
        }

        if (LaserBeamRedScreenUI.Instance != null)
        {
            LaserBeamRedScreenUI.Instance.SetRedScreenActive(false);
        }
    }

    private void FindPlayerTarget()
    {
        if (playerTargetTransform == null)
        {
            if (Nemuri.Core.CharacterSwapManager.Instance != null)
            {
                GameObject activePlayer = Nemuri.Core.CharacterSwapManager.Instance.GetActivePlayerObject();
                if (activePlayer != null)
                {
                    playerTargetTransform = activePlayer.transform;
                    return;
                }
            }

            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) playerTargetTransform = playerObj.transform;
        }
        else if (Nemuri.Core.CharacterSwapManager.Instance != null)
        {
            // Dynamically update target to active player character when swapped
            GameObject activePlayer = Nemuri.Core.CharacterSwapManager.Instance.GetActivePlayerObject();
            if (activePlayer != null && playerTargetTransform != activePlayer.transform)
            {
                playerTargetTransform = activePlayer.transform;
            }
        }
    }

    private IEnumerator BossAttackLoopRoutine()
    {
        yield return new WaitForSeconds(initialDelay);

        while (fightStarted)
        {
            if (!isExecutingAttack)
            {
                BossAttackModule nextAttack = SelectNextAttack();
                if (nextAttack != null)
                {
                    isExecutingAttack = true;
                    Debug.Log("[BossAttackController] Triggering Attack: " + nextAttack.attackName);
                    
                    yield return StartCoroutine(nextAttack.ExecuteAttackRoutine(bossTransform, playerTargetTransform, () =>
                    {
                        isExecutingAttack = false;
                    }));

                    float waitTime = Random.Range(minIntervalBetweenAttacks, maxIntervalBetweenAttacks);
                    yield return new WaitForSeconds(waitTime);
                }
                else
                {
                    yield return new WaitForSeconds(1.0f);
                }
            }
            else
            {
                yield return new WaitForSeconds(0.5f);
            }
        }
    }

    private BossAttackModule SelectNextAttack()
    {
        if (attackModules == null || attackModules.Count == 0) return null;

        List<BossAttackModule> readyAttacks = new List<BossAttackModule>();
        foreach (var m in attackModules)
        {
            if (m != null && m.CanExecute())
            {
                readyAttacks.Add(m);
            }
        }

        if (readyAttacks.Count == 0) return null;

        if (attackOrder == AttackOrderMode.Sequential)
        {
            for (int i = 0; i < attackModules.Count; i++)
            {
                int index = (currentSequentialIndex + i) % attackModules.Count;
                var mod = attackModules[index];
                if (mod != null && mod.CanExecute())
                {
                    currentSequentialIndex = (index + 1) % attackModules.Count;
                    return mod;
                }
            }
            return readyAttacks[0];
        }
        else
        {
            int r = Random.Range(0, readyAttacks.Count);
            return readyAttacks[r];
        }
    }
}
