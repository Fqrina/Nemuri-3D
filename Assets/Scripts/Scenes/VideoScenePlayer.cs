using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Nemuri.Scenes
{
    [RequireComponent(typeof(VideoPlayer))]
    public class VideoScenePlayer : MonoBehaviour
    {
        [SerializeField] private VideoClip _videoClip;
        [SerializeField] private bool _loop = false;
        [SerializeField] private Vector2Int _renderResolution = new Vector2Int(1920, 1080);

        private VideoPlayer _videoPlayer;
        private RenderTexture _renderTexture;
        private RawImage _videoDisplay;

        public VideoPlayer Player => _videoPlayer;
        public bool IsPlaying => _videoPlayer != null && _videoPlayer.isPlaying;

        private void Awake()
        {
            SetupUi();
        }

        public void Initialize(VideoClip clip, bool loop)
        {
            _videoClip = clip;
            _loop = loop;
            SetupVideoPlayer();
        }

        private void OnDestroy()
        {
            if (_renderTexture != null)
            {
                _renderTexture.Release();
            }
        }

        public void Play()
        {
            if (_videoPlayer == null)
            {
                return;
            }

            _videoPlayer.Play();
        }

        public void Stop()
        {
            if (_videoPlayer == null)
            {
                return;
            }

            _videoPlayer.Stop();
        }

        private void SetupUi()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGo = new GameObject("Video Canvas");
                canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 0;

                CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                canvasGo.AddComponent<GraphicRaycaster>();
            }

            Transform existingDisplay = canvas.transform.Find("Video Display");
            if (existingDisplay != null)
            {
                _videoDisplay = existingDisplay.GetComponent<RawImage>();
            }
            else
            {
                GameObject displayGo = new GameObject("Video Display");
                displayGo.transform.SetParent(canvas.transform, false);
                _videoDisplay = displayGo.AddComponent<RawImage>();
                _videoDisplay.color = Color.white;

                RectTransform rect = _videoDisplay.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
        }

        private void SetupVideoPlayer()
        {
            if (_videoPlayer != null)
            {
                return;
            }

            _videoPlayer = GetComponent<VideoPlayer>();
            _videoPlayer.playOnAwake = false;
            _videoPlayer.isLooping = _loop;
            _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            _videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;

            if (_videoClip != null)
            {
                _videoPlayer.clip = _videoClip;
            }

            _renderTexture = new RenderTexture(_renderResolution.x, _renderResolution.y, 0);
            _videoPlayer.targetTexture = _renderTexture;

            if (_videoDisplay != null)
            {
                _videoDisplay.texture = _renderTexture;
            }
        }
    }
}
