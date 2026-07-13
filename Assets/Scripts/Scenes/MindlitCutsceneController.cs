using System.Collections;
using Nemuri.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

namespace Nemuri.Scenes
{
    public class MindlitCutsceneController : MonoBehaviour
    {
        [Header("Video")]
        [SerializeField] private VideoClip _cutsceneVideo;

        [Header("Scene Transition")]
        [SerializeField] private string _nextSceneName = "Walking";
        [SerializeField, Min(0f)] private float _fadeDuration = 1.5f;

        private VideoScenePlayer _videoPlayer;
        private VideoPlayer _rawPlayer;
        private bool _hasFinished;

        private void Awake()
        {
            _videoPlayer = GetComponent<VideoScenePlayer>();
            if (_videoPlayer == null)
            {
                _videoPlayer = gameObject.AddComponent<VideoScenePlayer>();
            }

            _videoPlayer.Initialize(_cutsceneVideo, loop: false);
            _rawPlayer = _videoPlayer.Player;
            if (_rawPlayer != null)
            {
                _rawPlayer.isLooping = false;
                _rawPlayer.loopPointReached += OnVideoFinished;
            }
        }

        private void OnDestroy()
        {
            if (_rawPlayer != null)
            {
                _rawPlayer.loopPointReached -= OnVideoFinished;
            }
        }

        private void Start()
        {
            if (SceneTransitionState.FadeInOnLoad)
            {
                StartCoroutine(FadeInThenPlayRoutine());
                SceneTransitionState.FadeInOnLoad = false;
                return;
            }

            _videoPlayer.Play();
        }

        private IEnumerator FadeInThenPlayRoutine()
        {
            yield return ScreenFader.Instance.FadeToClear(SceneTransitionState.FadeInDuration);
            _videoPlayer.Play();
        }

        private void OnVideoFinished(VideoPlayer source)
        {
            if (_hasFinished)
            {
                return;
            }

            _hasFinished = true;
            StartCoroutine(TransitionToWalkingRoutine());
        }

        private IEnumerator TransitionToWalkingRoutine()
        {
            _videoPlayer.Stop();

            SceneTransitionState.FadeOutInstantlyOnLoad = true;

            yield return ScreenFader.Instance.FadeToBlack(_fadeDuration);

            if (string.IsNullOrWhiteSpace(_nextSceneName))
            {
                Debug.LogError("[MindlitCutsceneController] Next scene name is not assigned.", this);
                yield break;
            }

            SceneManager.LoadScene(_nextSceneName);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_cutsceneVideo == null)
            {
                _cutsceneVideo = UnityEditor.AssetDatabase.LoadAssetAtPath<VideoClip>(
                    "Assets/Videos/Mindlit Cutscene (Beginning).mov");
            }
        }
#endif
    }
}
