using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using Nemuri.Dialogue;
using Nemuri.UI;
using Nemuri.Player;
using Nemuri.Core;

namespace Nemuri.Scenes
{
    public class Chapter3IntroController : MonoBehaviour
    {
        public static Chapter3IntroController Instance { get; private set; }

        [Header("Dialogue File")]
        [SerializeField] private TextAsset _amygdalaDialogueJson;

        [Header("Camera Cutscene Transforms / Objects")]
        [SerializeField] private Transform _cutsceneCam;
        [SerializeField] private Transform _cutsceneTarget;
        [SerializeField] private Transform _cutscene2Cam;

        [Header("Cutscene Video Settings")]
        [SerializeField] private VideoClip _chpt3VideoClip;

        [Header("Boss & NPC References")]
        [SerializeField] private GameObject _evilRabbitBoss;
        [SerializeField] private List<GameObject> _npcsToDisable = new List<GameObject>();

        private bool _isCutsceneActive = false;
        private Coroutine _cameraPanCoroutine;
        private int _originalCullingMask;

        private Quaternion _initialCutsceneCamRotation;
        private BossFightCamera _bossFightCam;
        private FixedWorldOffsetCamera _fixedCam;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Auto-run when in chpt3 scene
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "chpt3")
            {
                StartCoroutine(Chapter3CutsceneSequence());
            }
        }

        private void Update()
        {
            if (_isCutsceneActive)
            {
                // Ensure player health bar remains hidden during cutscene
                if (PlayerHealthBarUI.Instance != null)
                {
                    PlayerHealthBarUI.Instance.HidePlayerHealthBar();
                }

                // Ensure all gameplay follow cameras remain disabled during cutscene
                BossFightCamera[] bossCams = FindObjectsByType<BossFightCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var c in bossCams)
                {
                    if (c != null && c.enabled) c.enabled = false;
                }

                FixedWorldOffsetCamera[] fixedCams = FindObjectsByType<FixedWorldOffsetCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var fc in fixedCams)
                {
                    if (fc != null && fc.enabled) fc.enabled = false;
                }
            }
        }

        public IEnumerator Chapter3CutsceneSequence()
        {
            _isCutsceneActive = true;

            // Ensure DialogueManager instance exists
            EnsureDialogueManager();

            // ----------------------------------------------------
            // Step 1: Initial Black Screen, Camera Locking & Setup
            // ----------------------------------------------------
            if (ScreenFader.Instance != null)
            {
                ScreenFader.Instance.SetAlphaImmediate(1f);
            }

            // Lock Player Movement & Swapping
            SetPlayerControlEnabled(false);

            // Disable gameplay follow cameras (BossFightCamera / FixedWorldOffsetCamera) during cutscene
            DisableGameplayCameras();

            // Find EVILRABBIT boss & set visibility = 0
            FindAndHideBoss();

            // Find or Auto-Create Cutscene Transforms if not assigned
            FindCutsceneTargets();

            // Explicitly set camera to CutsceneCam (disabling Cutscene2Cam)
            SetActiveCutsceneCamera(_cutsceneCam);

            if (_cutsceneCam != null)
            {
                _initialCutsceneCamRotation = _cutsceneCam.rotation;
            }
            else if (Camera.main != null)
            {
                _initialCutsceneCamRotation = Camera.main.transform.rotation;
            }

            // Prevent BossFightTester from auto-starting on proximity distance
            BossFightTester tester = FindAnyObjectByType<BossFightTester>();
            if (tester != null)
            {
                tester.enabled = true;
                tester.manualStartOnly = true;
            }

            // Fade in from black to clear
            if (ScreenFader.Instance != null)
            {
                yield return ScreenFader.Instance.FadeToClear(1.5f);
            }

            // ----------------------------------------------------
            // Step 2: Play Part 1 Dialogue (Nodes 0 - 12)
            // ----------------------------------------------------
            TextAsset dialogueAsset = _amygdalaDialogueJson;
            if (dialogueAsset == null)
            {
                dialogueAsset = Resources.Load<TextAsset>("Dialogue/amygdala");
            }

            if (dialogueAsset != null && DialogueManager.Instance != null)
            {
                DialogueSequence seq = JsonUtility.FromJson<DialogueSequence>(dialogueAsset.text);
                if (seq != null && seq.nodes != null && seq.nodes.Count >= 13)
                {
                    List<DialogueNode> part1Nodes = seq.nodes.GetRange(0, 13);

                    int currentNodeIndex = 0;

                    // Subscribe to camera rotation event
                    System.Action<DialogueNode> onNodeDisplayedHandler = (node) =>
                    {
                        if (node != null)
                        {
                            // Node 5: Keiko says "(Cry) Please don't fight..." -> rotate to look at CutsceneTarget
                            bool isPanToTargetNode = node.speaker == "Keiko" && node.text.Contains("fight");

                            // Node 8: Keiko says "I can feel the resonance..." -> revert rotation back to original
                            bool isRevertNode = node.speaker == "Keiko" && (node.text.Contains("resonance") || node.text.Contains("faint"));

                            if (isPanToTargetNode && _cutsceneTarget != null)
                            {
                                Debug.Log("[Chapter3IntroController] Keiko fight node displayed - rotating CutsceneCam to CutsceneTarget!");
                                if (_cameraPanCoroutine != null) StopCoroutine(_cameraPanCoroutine);

                                Transform camTrans = _cutsceneCam != null ? _cutsceneCam : (Camera.main != null ? Camera.main.transform : null);
                                if (camTrans != null)
                                {
                                    Vector3 dir = _cutsceneTarget.position - camTrans.position;
                                    if (dir.sqrMagnitude > 0.001f)
                                    {
                                        Quaternion targetRot = Quaternion.LookRotation(dir);
                                        _cameraPanCoroutine = StartCoroutine(SmoothRotateCameraToRotation(targetRot, 2.5f));
                                    }
                                }
                            }
                            else if (isRevertNode)
                            {
                                Debug.Log("[Chapter3IntroController] Keiko resonance node displayed - reverting CutsceneCam to original rotation!");
                                if (_cameraPanCoroutine != null) StopCoroutine(_cameraPanCoroutine);
                                _cameraPanCoroutine = StartCoroutine(SmoothRotateCameraToRotation(_initialCutsceneCamRotation, 2.5f));
                            }
                        }

                        currentNodeIndex++;
                    };

                    DialogueManager.OnNodeDisplayed += onNodeDisplayedHandler;
                    DialogueManager.Instance.StartConversation(part1Nodes);

                    while (DialogueManager.Instance.IsConversationActive)
                    {
                        yield return null;
                    }

                    DialogueManager.OnNodeDisplayed -= onNodeDisplayedHandler;
                }
            }

            // ----------------------------------------------------
            // Step 3: Fade to Black & Play CHPT3.mov Video
            // ----------------------------------------------------
            if (ScreenFader.Instance != null)
            {
                yield return ScreenFader.Instance.FadeToBlack(1.0f);
            }

            // Disable all NPCs
            DisableAllNpcs();

            // Set CullingMask = 0 to reduce rendering lag during video cutscene
            Camera videoCam = Camera.main;
            if (videoCam == null && _cutsceneCam != null) videoCam = _cutsceneCam.GetComponent<Camera>();

            if (videoCam != null)
            {
                _originalCullingMask = videoCam.cullingMask;
                videoCam.cullingMask = 0;
            }

            // Setup VideoPlayer
            GameObject videoGo = new GameObject("CHPT3_VideoPlayer");
            VideoPlayer vp = videoGo.AddComponent<VideoPlayer>();
            VideoScenePlayer vsp = videoGo.AddComponent<VideoScenePlayer>();

            VideoClip clipToPlay = _chpt3VideoClip;
            if (clipToPlay == null)
            {
#if UNITY_EDITOR
                clipToPlay = UnityEditor.AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/Videos/CHPT3.mov");
#endif
            }

            if (clipToPlay != null)
            {
                vsp.Initialize(clipToPlay, false);
                yield return vsp.PrepareRoutine();

                vsp.Play();

                // Buffer wait up to 2.0s for video player to start playing asynchronously
                float startTimeout = 2.0f;
                while (!vsp.IsPlaying && startTimeout > 0f)
                {
                    startTimeout -= Time.deltaTime;
                    yield return null;
                }

                if (ScreenFader.Instance != null)
                {
                    yield return ScreenFader.Instance.FadeToClear(0.5f);
                }

                // Wait until video finishes playing
                while (vsp.IsPlaying)
                {
                    yield return null;
                }
            }

            if (ScreenFader.Instance != null)
            {
                yield return ScreenFader.Instance.FadeToBlack(1.0f);
            }

            Destroy(videoGo);

            // Restore Camera CullingMask
            if (videoCam != null)
            {
                videoCam.cullingMask = _originalCullingMask;
            }

            // ----------------------------------------------------
            // Step 4: Make EVIL RABBIT visible & Switch Camera to Cutscene2cam for Part 2 Dialogue
            // ----------------------------------------------------
            ShowBoss();
            SetActiveCutsceneCamera(_cutscene2Cam);

            if (ScreenFader.Instance != null)
            {
                yield return ScreenFader.Instance.FadeToClear(1.0f);
            }

            if (dialogueAsset != null && DialogueManager.Instance != null)
            {
                DialogueSequence seq = JsonUtility.FromJson<DialogueSequence>(dialogueAsset.text);
                if (seq != null && seq.nodes != null && seq.nodes.Count > 13)
                {
                    List<DialogueNode> part2Nodes = seq.nodes.GetRange(13, seq.nodes.Count - 13);
                    DialogueManager.Instance.StartConversation(part2Nodes);

                    while (DialogueManager.Instance.IsConversationActive)
                    {
                        yield return null;
                    }
                }
            }

            // ----------------------------------------------------
            // Step 5: Restore Gameplay Camera & Activate Boss Fight State
            // ----------------------------------------------------
            // Enable Player Control & Restore BossFightCamera
            SetPlayerControlEnabled(true);
            RestoreGameplayCamera();

            // Start Boss Fight directly
            if (tester != null)
            {
                tester.StartFightFromCutscene();
            }
        }

        private void SetActiveCutsceneCamera(Transform activeCamTarget)
        {
            if (_cutsceneCam != null)
            {
                Camera c1 = _cutsceneCam.GetComponent<Camera>();
                if (c1 != null) c1.enabled = (_cutsceneCam == activeCamTarget);
            }

            if (_cutscene2Cam != null)
            {
                Camera c2 = _cutscene2Cam.GetComponent<Camera>();
                if (c2 != null) c2.enabled = (_cutscene2Cam == activeCamTarget);
            }

            if (Camera.main != null && activeCamTarget != null)
            {
                Camera.main.transform.position = activeCamTarget.position;
                Camera.main.transform.rotation = activeCamTarget.rotation;
            }
        }

        private void RestoreGameplayCamera()
        {
            _isCutsceneActive = false;

            if (_cutsceneCam != null)
            {
                Camera c1 = _cutsceneCam.GetComponent<Camera>();
                if (c1 != null) c1.enabled = false;
                _cutsceneCam.gameObject.SetActive(false);
            }

            if (_cutscene2Cam != null)
            {
                Camera c2 = _cutscene2Cam.GetComponent<Camera>();
                if (c2 != null) c2.enabled = false;
                _cutscene2Cam.gameObject.SetActive(false);
            }

            if (Camera.main != null)
            {
                Camera.main.enabled = true;

                // Disable CinemachineBrain so BossFightCamera has 100% direct control of MainCamera
                var brain = Camera.main.GetComponent<Cinemachine.CinemachineBrain>();
                if (brain != null) brain.enabled = false;

                BossFightCamera bCam = Camera.main.GetComponent<BossFightCamera>();
                if (bCam == null)
                {
                    bCam = Camera.main.gameObject.AddComponent<BossFightCamera>();
                }

                if (bCam != null)
                {
                    GameObject pObj = GameObject.FindWithTag("Player");
                    if (pObj != null) bCam.player = pObj.transform;

                    GameObject bObj = _evilRabbitBoss != null ? _evilRabbitBoss : GameObject.Find("EVILRABBIT");
                    if (bObj == null) bObj = GameObject.Find("EVIL RABBIT");
                    if (bObj != null) bCam.boss = bObj.transform;

                    bCam.enabled = true;
                }
            }
        }

        private void EnsureDialogueManager()
        {
            if (DialogueManager.Instance != null) return;
            DialogueManager existing = FindAnyObjectByType<DialogueManager>();
            if (existing == null)
            {
                new GameObject("DialogueManager").AddComponent<DialogueManager>();
            }
        }

        private void DisableGameplayCameras()
        {
            if (Camera.main != null)
            {
                _bossFightCam = Camera.main.GetComponent<BossFightCamera>();
                if (_bossFightCam != null) _bossFightCam.enabled = false;

                _fixedCam = Camera.main.GetComponent<FixedWorldOffsetCamera>();
                if (_fixedCam != null) _fixedCam.enabled = false;

                var brain = Camera.main.GetComponent<Cinemachine.CinemachineBrain>();
                if (brain != null) brain.enabled = false;
            }
        }

        private void EnableGameplayCameras()
        {
            RestoreGameplayCamera();
        }

        private IEnumerator SmoothRotateCameraToRotation(Quaternion targetRot, float duration)
        {
            Transform camTrans = _cutsceneCam != null ? _cutsceneCam : (Camera.main != null ? Camera.main.transform : null);
            if (camTrans == null) yield break;

            Quaternion startRotCam = camTrans.rotation;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                Quaternion currentRot = Quaternion.Slerp(startRotCam, targetRot, t);

                if (_cutsceneCam != null)
                {
                    _cutsceneCam.rotation = currentRot;
                }

                if (Camera.main != null)
                {
                    Camera.main.transform.rotation = currentRot;
                }

                yield return null;
            }

            if (_cutsceneCam != null) _cutsceneCam.rotation = targetRot;
            if (Camera.main != null) Camera.main.transform.rotation = targetRot;
        }

        private void FindAndHideBoss()
        {
            if (_evilRabbitBoss == null)
            {
                GameObject boss = GameObject.Find("EVILRABBIT");
                if (boss == null) boss = GameObject.Find("EVIL RABBIT");
                if (boss == null) boss = GameObject.Find("EVILRABBIT.fbx");
                if (boss != null) _evilRabbitBoss = boss;
            }

            if (_evilRabbitBoss != null)
            {
                SetBossVisibility(_evilRabbitBoss, false);
            }
        }

        private void ShowBoss()
        {
            if (_evilRabbitBoss == null)
            {
                GameObject boss = GameObject.Find("EVILRABBIT");
                if (boss == null) boss = GameObject.Find("EVIL RABBIT");
                if (boss == null) boss = GameObject.Find("EVILRABBIT.fbx");
                if (boss != null) _evilRabbitBoss = boss;
            }

            if (_evilRabbitBoss != null)
            {
                SetBossVisibility(_evilRabbitBoss, true);
            }
        }

        private void SetBossVisibility(GameObject boss, bool visible)
        {
            Renderer[] renderers = boss.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in renderers)
            {
                r.enabled = visible;
            }
        }

        private void DisableAllNpcs()
        {
            string[] npcNames = new string[] { "Keiko", "Rona", "Murial", "Feanor", "Ferry", "FerryNPC", "KeikoNPC", "RonaNPC", "MurialNPC", "FeanorNPC" };
            foreach (string name in npcNames)
            {
                GameObject npc = GameObject.Find(name);
                if (npc != null)
                {
                    npc.SetActive(false);
                    if (!_npcsToDisable.Contains(npc)) _npcsToDisable.Add(npc);
                }
            }

            foreach (GameObject npc in _npcsToDisable)
            {
                if (npc != null) npc.SetActive(false);
            }
        }

        private void FindCutsceneTargets()
        {
            if (_cutsceneCam == null)
            {
                GameObject go = GameObject.Find("CutsceneCam");
                if (go == null) go = GameObject.Find("Cutscene Cam");
                if (go == null) go = GameObject.Find("cutsceneCam");
                if (go != null) _cutsceneCam = go.transform;
                else
                {
                    go = new GameObject("CutsceneCam");
                    go.transform.position = new Vector3(0f, 5f, -10f);
                    _cutsceneCam = go.transform;
                }
            }

            if (_cutsceneTarget == null)
            {
                GameObject go = GameObject.Find("CutsceneTarget");
                if (go == null) go = GameObject.Find("Cutscene Target");
                if (go == null) go = GameObject.Find("cutsceneTarget");
                if (go != null) _cutsceneTarget = go.transform;
                else
                {
                    go = new GameObject("CutsceneTarget");
                    go.transform.position = _cutsceneCam.position + _cutsceneCam.forward * 5f;
                    _cutsceneTarget = go.transform;
                }
            }

            if (_cutscene2Cam == null)
            {
                GameObject go = GameObject.Find("Cutscene2Cam");
                if (go == null) go = GameObject.Find("Cutscene2cam");
                if (go == null) go = GameObject.Find("Cutscene 2 Cam");
                if (go != null) _cutscene2Cam = go.transform;
                else
                {
                    go = new GameObject("Cutscene2cam");
                    go.transform.position = new Vector3(0f, 6f, -8f);
                    _cutscene2Cam = go.transform;
                }
            }
        }

        private void SetPlayerControlEnabled(bool enabled)
        {
            CharacterSwapManager swapMgr = FindAnyObjectByType<CharacterSwapManager>();
            if (swapMgr != null)
            {
                swapMgr.enabled = enabled;
            }

            PlayerMovementChapt1 playerMov = FindAnyObjectByType<PlayerMovementChapt1>();
            if (playerMov != null)
            {
                playerMov.enabled = enabled;
            }
        }
    }
}
