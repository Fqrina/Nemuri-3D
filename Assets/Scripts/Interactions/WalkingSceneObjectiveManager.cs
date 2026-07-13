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
            StartIntroDialogue();
        }

        private void StartIntroDialogue()
        {
            if (DialogueManager.Instance == null)
            {
                DialogueManager existingManager = FindAnyObjectByType<DialogueManager>();
                if (existingManager == null)
                {
                    GameObject dmGo = new GameObject("DialogueManager");
                    dmGo.AddComponent<DialogueManager>();
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
            StartCoroutine(FinishIntroRoutine());
        }

        private IEnumerator FinishIntroRoutine()
        {
            if (Nemuri.Player.PlayerMovement.Instance != null)
            {
                Nemuri.Player.PlayerMovement.Instance.SetCanMove(false);
            }

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
