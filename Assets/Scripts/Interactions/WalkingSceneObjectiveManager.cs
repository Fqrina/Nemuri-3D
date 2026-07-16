using UnityEngine;
using System.Collections;
using Nemuri.Dialogue;
using Nemuri.UI;

namespace Nemuri.Interactions
{
    public class WalkingSceneObjectiveManager : MonoBehaviour
    {
        public static WalkingSceneObjectiveManager Instance { get; private set; }

        public enum WalkingObjective
        {
            None = 0,
            GoToKitchen = 1,
            TakeWater = 2,
            DrinkPills = 3,
            GoToBed = 4
        }

        [Header("Scene Transition")]
        [SerializeField] private string _nextSceneName = "chpt2";

        [Header("Intro Settings")]
        [SerializeField] private TextAsset _introDialogueJson;

        [Header("Current State")]
        [SerializeField] private WalkingObjective _currentObjective = WalkingObjective.None;

        [Header("Intro Sequence References")]
        [SerializeField] private Animator _kaelcharaAnimator;
        [SerializeField] private GameObject _kaelcharaInnerCamera;
        [SerializeField] private GameObject _mainVirtualCamera;
        [SerializeField] private GameObject _bedObject;
        [SerializeField] private float _animationWaitTime = 3f;

        [Header("Post-Animation Tweaks")]
        [SerializeField] private float _postAnimationRotationY = 90f;
        [SerializeField] private float _postAnimationOffsetZ = 1f;

        public WalkingObjective CurrentObjective => _currentObjective;

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
            if (SceneTransitionState.FadeOutInstantlyOnLoad)
            {
                ScreenFader.Instance.SetAlphaImmediate(0f);
                SceneTransitionState.FadeOutInstantlyOnLoad = false;
            }

            _currentObjective = WalkingObjective.None;
            StartCoroutine(IntroSequenceRoutine());
        }

        private IEnumerator IntroSequenceRoutine()
        {
            // 1. Setup camera & disable move & ignore collisions & disable gravity
            Rigidbody playerRb = null;
            if (Nemuri.Player.PlayerMovement.Instance != null)
            {
                Nemuri.Player.PlayerMovement.Instance.SetCanMove(false);
                playerRb = Nemuri.Player.PlayerMovement.Instance.GetComponent<Rigidbody>();
            }

            if (playerRb != null)
            {
                playerRb.useGravity = false;
                playerRb.linearVelocity = Vector3.zero;
                playerRb.angularVelocity = Vector3.zero;
            }

            SetCollisionIgnored(true);

            Camera mainCam = Camera.main;
            Camera innerCam = _kaelcharaInnerCamera != null ? _kaelcharaInnerCamera.GetComponent<Camera>() : null;

            if (innerCam != null)
            {
                // Case A: Inner camera is a standard Unity Camera
                innerCam.enabled = true;
                if (mainCam != null)
                {
                    mainCam.enabled = false;
                }
            }
            else
            {
                // Case B: Inner camera is a Cinemachine Virtual Camera (or we assume it is)
                SetVirtualCameraPriority(_kaelcharaInnerCamera, 100);
                SetVirtualCameraPriority(_mainVirtualCamera, 0);

                if (_kaelcharaInnerCamera != null) _kaelcharaInnerCamera.SetActive(true);
                if (_mainVirtualCamera != null) _mainVirtualCamera.SetActive(false);
            }

            // 2. Play animation
            if (_kaelcharaAnimator != null)
            {
                _kaelcharaAnimator.Play("OutBed");
            }

            // 3. Wait for animation
            yield return new WaitForSeconds(_animationWaitTime);

            // 4. Reset Animator state and apply final rotation/offset as requested
            if (_kaelcharaAnimator != null)
            {
                _kaelcharaAnimator.Play("Idle"); // Reset state to fix walking animation
            }

            Transform playerTransform = Nemuri.Player.PlayerMovement.Instance != null ? 
                Nemuri.Player.PlayerMovement.Instance.transform : 
                (_kaelcharaAnimator != null ? _kaelcharaAnimator.transform : null);

            if (playerTransform != null)
            {
                playerTransform.rotation = Quaternion.Euler(0f, _postAnimationRotationY, 0f);
                playerTransform.position += new Vector3(0f, 0f, _postAnimationOffsetZ);
            }

            // 5. Restore camera & re-enable collisions & gravity
            if (playerRb != null)
            {
                playerRb.useGravity = true;
            }

            SetCollisionIgnored(false);

            if (innerCam != null)
            {
                innerCam.enabled = false;
                if (mainCam != null)
                {
                    mainCam.enabled = true;
                }
            }
            else
            {
                SetVirtualCameraPriority(_kaelcharaInnerCamera, 0);
                SetVirtualCameraPriority(_mainVirtualCamera, 100);

                if (_kaelcharaInnerCamera != null) _kaelcharaInnerCamera.SetActive(false);
                if (_mainVirtualCamera != null) _mainVirtualCamera.SetActive(true);
            }

            // 6. Start objectives
            StartIntroDialogue();

            // Note: We don't enable movement here because dialogue is playing,
            // the DialogueManager handles enabling/disabling movement.
        }

        private void SetCollisionIgnored(bool ignore)
        {
            if (_bedObject == null) return;
            
            var playerMove = Nemuri.Player.PlayerMovement.Instance;
            if (playerMove == null) return;

            Collider[] playerColliders = playerMove.GetComponentsInChildren<Collider>();
            Collider[] bedColliders = _bedObject.GetComponentsInChildren<Collider>();

            foreach (var pCol in playerColliders)
            {
                foreach (var bCol in bedColliders)
                {
                    if (pCol != null && bCol != null)
                    {
                        Physics.IgnoreCollision(pCol, bCol, ignore);
                    }
                }
            }
        }

        private void SetVirtualCameraPriority(GameObject camObj, int priority)
        {
            if (camObj == null) return;
            
            // Try CinemachineVirtualCamera (Unity 2022-)
            var vcam = camObj.GetComponent("CinemachineVirtualCamera");
            if (vcam != null)
            {
                var prop = vcam.GetType().GetProperty("Priority");
                if (prop != null) prop.SetValue(vcam, priority, null);
                return;
            }

            // Try CinemachineCamera (Unity 6)
            var ccam = camObj.GetComponent("CinemachineCamera");
            if (ccam != null)
            {
                var prop = ccam.GetType().GetProperty("Priority");
                if (prop != null) prop.SetValue(ccam, priority, null);
            }
        }

        private void StartIntroDialogue()
        {
            if (DialogueManager.Instance == null)
            {
                DialogueManager existingManager = FindAnyObjectByType<DialogueManager>();
                if (existingManager == null)
                {
                    GameObject dmGo = new GameObject("DialogueManager");
                    dmGo.AddComponent<WalkingDialogueManager>();
                }
            }

            if (_introDialogueJson == null)
            {
                Debug.LogWarning("[WalkingSceneObjectiveManager] Intro dialogue JSON is not assigned.", this);
                return;
            }

            DialogueSequence sequence = JsonUtility.FromJson<DialogueSequence>(_introDialogueJson.text);
            if (sequence == null || sequence.nodes == null)
            {
                Debug.LogWarning("[WalkingSceneObjectiveManager] Intro dialogue JSON could not be parsed.", this);
                return;
            }

            if (DialogueManager.Instance == null)
            {
                Debug.LogWarning("[WalkingSceneObjectiveManager] DialogueManager is not available.", this);
                return;
            }

            DialogueManager.Instance.StartConversation(sequence.nodes);
        }

        public void SetActiveObjective(string text)
        {
            if (text.Contains("kitchen") || text.Contains("Kitchen"))
            {
                _currentObjective = WalkingObjective.GoToKitchen;
            }
            else if (text.Contains("water") || text.Contains("Water") || text.Contains("glass") || text.Contains("Glass"))
            {
                _currentObjective = WalkingObjective.TakeWater;
            }
            else if (text.Contains("pill") || text.Contains("Pill"))
            {
                _currentObjective = WalkingObjective.DrinkPills;
            }
            else if (text.Contains("bed") || text.Contains("Bed"))
            {
                _currentObjective = WalkingObjective.GoToBed;
            }
            Debug.Log($"[WalkingSceneObjectiveManager] Active objective changed to: {_currentObjective}");
        }

        public void CompleteKitchenObjective()
        {
            if (_currentObjective != WalkingObjective.GoToKitchen) return;
            _currentObjective = WalkingObjective.None;
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.ResumeConversation();
            }
        }

        public void CompleteWaterObjective()
        {
            if (_currentObjective != WalkingObjective.TakeWater) return;
            _currentObjective = WalkingObjective.None;
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.ResumeConversation();
            }
        }

        public void CompletePillsObjective()
        {
            if (_currentObjective != WalkingObjective.DrinkPills) return;
            _currentObjective = WalkingObjective.None;
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.ResumeConversation();
            }
        }

        public void CompleteBedObjective()
        {
            if (_currentObjective != WalkingObjective.GoToBed) return;
            _currentObjective = WalkingObjective.None;
            Debug.Log("[WalkingSceneObjectiveManager] All objectives completed!");
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.ForceEndConversation();
            }
            StartCoroutine(FinishIntroRoutine());
        }

        private IEnumerator FinishIntroRoutine()
        {
            if (Nemuri.Player.PlayerMovement.Instance != null)
            {
                Nemuri.Player.PlayerMovement.Instance.SetCanMove(false);
            }

            SceneTransitionState.FadeInOnLoad = true;
            SceneTransitionState.FadeInDuration = 2f;

            if (ScreenFader.Instance != null)
            {
                yield return ScreenFader.Instance.FadeToBlack(2f);
            }

            if (string.IsNullOrWhiteSpace(_nextSceneName))
            {
                Debug.LogError("[WalkingSceneObjectiveManager] Next scene name is not assigned.", this);
                yield break;
            }

            UnityEngine.SceneManagement.SceneManager.LoadScene(_nextSceneName);
        }
    }
}
