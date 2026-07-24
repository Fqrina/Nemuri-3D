using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using Nemuri.Dialogue;
using Nemuri.Interactions;
using Nemuri.Player;
using Nemuri.Scenes;
using Nemuri.UI;

namespace Nemuri.Core
{
    public class TeleportPortal : MonoBehaviour
    {
        private const string PlayerTag = "Player";

        public enum TeleportMode { SameScene, DifferentScene }

        [Header("Teleport Settings")]
        [SerializeField] private TeleportMode _teleportMode = TeleportMode.DifferentScene;
        [SerializeField] private Vector3 _targetPosition = new Vector3(100f, 0f, 0f);
        [SerializeField] private string _targetSceneName = "chpt2";

        [Header("Cutscene & Audio")]
        [SerializeField] private VideoClip _transitionVideo;
        [SerializeField] private AudioClip _transitionAudio;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(PlayerTag) && 
                !other.TryGetComponent(out PlayerMovement _) && 
                !other.TryGetComponent(out PlayerMovementChapt1 _))
                return;

            if (PuzzleManager.Instance == null || !PuzzleManager.Instance.AreAllCrystalsCollected())
            {
                EnsureDialogueManager();
                if (DialogueManager.Instance != null)
                {
                    DialogueManager.Instance.ShowDialogue("Kael", "I need to collect all 3 crystals first.");
                }
                return;
            }

            Teleport(other.transform);
        }

        private void Teleport(Transform playerTransform)
        {
            if (_teleportMode == TeleportMode.DifferentScene)
            {
                StartCoroutine(TransitionSceneRoutine());
                return;
            }

            playerTransform.position = _targetPosition;

            if (playerTransform.TryGetComponent(out Rigidbody rb))
            {
                rb.position = _targetPosition;
                rb.linearVelocity = Vector3.zero;
            }
        }

        private IEnumerator TransitionSceneRoutine()
        {
            if (PlayerMovement.Instance != null) PlayerMovement.Instance.SetCanMove(false);
            if (PlayerMovementChapt1.Instance != null) PlayerMovementChapt1.Instance.SetCanMove(false);

            if (ScreenFader.Instance != null)
            {
                yield return ScreenFader.Instance.FadeToBlack(2f);
            }

            // Optimize 3D camera rendering during video playback
            Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            Dictionary<Camera, int> originalCullingMasks = new Dictionary<Camera, int>();
            foreach (Camera cam in allCameras)
            {
                if (cam != null && cam.enabled)
                {
                    originalCullingMasks[cam] = cam.cullingMask;
                    cam.cullingMask = 0; // 0 = Nothing (0 3D draw calls)
                }
            }

            // Play cutscene video and transition sound if assigned
            if (_transitionVideo != null)
            {
                VideoScenePlayer videoPlayer = GetComponent<VideoScenePlayer>();
                if (videoPlayer == null)
                {
                    videoPlayer = gameObject.AddComponent<VideoScenePlayer>();
                }

                bool isVideoFinished = false;
                videoPlayer.Initialize(_transitionVideo, loop: false);

                // Buffer video into GPU memory while black
                yield return videoPlayer.PrepareRoutine();

                if (videoPlayer.Player != null)
                {
                    videoPlayer.Player.isLooping = false;
                    videoPlayer.Player.loopPointReached += (source) => isVideoFinished = true;
                }

                // Play transition sound effect
                if (_transitionAudio != null)
                {
                    AudioSource audioSource = GetComponent<AudioSource>();
                    if (audioSource == null)
                    {
                        audioSource = gameObject.AddComponent<AudioSource>();
                    }
                    audioSource.clip = _transitionAudio;
                    audioSource.playOnAwake = false;
                    audioSource.volume = 1.0f;
                    audioSource.Play();
                }

                videoPlayer.Play();

                // Fade in from black to display video once prepared
                if (ScreenFader.Instance != null)
                {
                    yield return ScreenFader.Instance.FadeToClear(1f);
                }

                // Wait for video playback completion
                while (!isVideoFinished)
                {
                    yield return null;
                }

                // Fade out to black after video finishes
                if (ScreenFader.Instance != null)
                {
                    yield return ScreenFader.Instance.FadeToBlack(1.5f);
                }

                videoPlayer.Stop();
            }

            SceneTransitionState.FadeInOnLoad = true;
            SceneTransitionState.FadeInDuration = 2f;

            if (string.IsNullOrWhiteSpace(_targetSceneName))
            {
                Debug.LogError("[TeleportPortal] Target scene name is not assigned.", this);
                yield break;
            }

            UnityEngine.SceneManagement.SceneManager.LoadScene(_targetSceneName);
        }

        private void EnsureDialogueManager()
        {
            if (DialogueManager.Instance != null) return;
            DialogueManager existingManager = FindAnyObjectByType<DialogueManager>();
            if (existingManager == null)
            {
                new GameObject("DialogueManager").AddComponent<DialogueManager>();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_transitionVideo == null)
            {
                _transitionVideo = UnityEditor.AssetDatabase.LoadAssetAtPath<VideoClip>(
                    "Assets/Videos/CHPT1to2.mov");
            }

            if (_transitionAudio == null)
            {
                _transitionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(
                    "Assets/Sounds/Transition.wav");
            }
        }
#endif
    }
}
