using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Nemuri.Player;

namespace Nemuri.Interactions
{
    public class VinesMinigame : MonoBehaviour
    {
        private struct NoteSchedule
        {
            public float spawnTime;
            public int lane;
            public bool spawned;

            public NoteSchedule(float spawnTime, int lane)
            {
                this.spawnTime = spawnTime;
                this.lane = lane;
                this.spawned = false;
            }
        }

        private class NoteInstance
        {
            public int lane;
            public GameObject gameObject;
            public RectTransform rectTransform;
            public bool isHit;
        }

        private bool _isPlaying;
        private float _timer;
        private int _hits;
        private int _misses;
        private bool _isGameOver;
        private bool _hasWon;

        private float _speed = 350f;
        private const float TargetY = -250f;
        private const float SpawnY = 300f;
        private const float HitWindow = 50f;

        private float _panelWidth = 400f;
        private float _panelHeight = 700f;

        private GameObject _uiCanvasRoot;
        private RectTransform _notesContainer;
        private Text _scoreText;
        private Text _feedbackText;
        private GameObject _gameOverPanel;
        private Text _gameOverText;
        private Text _gameOverSubtext;

        private Image[] _targetZoneImages = new Image[4];
        private RectTransform[] _targetZoneRects = new RectTransform[4];

        private Sprite[] _noteSprites = new Sprite[4];
        private Sprite _targetSprite;

        private List<NoteSchedule> _notesSchedule = new List<NoteSchedule>();
        private List<NoteInstance> _activeNotes = new List<NoteInstance>();
        private Coroutine _feedbackCoroutine;
        private Interactable _interactable;

        private void Awake()
        {
            _interactable = GetComponent<Interactable>();
        }

        private void OnDisable()
        {
            if (_isPlaying)
            {
                EndMinigame(false);
            }
        }

        public void StartMinigame()
        {
            if (_isPlaying) return;

            _isPlaying = true;
            _isGameOver = false;
            _hasWon = false;
            _timer = 0f;
            _hits = 0;
            _misses = 0;

            _activeNotes.Clear();
            InitializeNotesSchedule();

            if (PlayerMovement.Instance != null) PlayerMovement.Instance.SetCanMove(false);
            if (PlayerMovementChapt1.Instance != null) PlayerMovementChapt1.Instance.SetCanMove(false);

            if (_interactable != null)
            {
                _interactable.DismissInteraction();
                _interactable.enabled = false;
            }

            InitializeSprites();
            CreateUI();
            UpdateScoreText();
        }

        private void InitializeSprites()
        {
            _targetSprite = CreateCircleSprite(30, Color.white, true, 3);
            for (int i = 0; i < 4; i++)
            {
                _noteSprites[i] = CreateCircleSprite(25, GetLaneColor(i), false, 0);
            }
        }

        private void CleanupSprites()
        {
            if (_targetSprite != null)
            {
                if (_targetSprite.texture != null) Destroy(_targetSprite.texture);
                Destroy(_targetSprite);
                _targetSprite = null;
            }
            for (int i = 0; i < 4; i++)
            {
                if (_noteSprites[i] != null)
                {
                    if (_noteSprites[i].texture != null) Destroy(_noteSprites[i].texture);
                    Destroy(_noteSprites[i]);
                    _noteSprites[i] = null;
                }
            }
        }

        private void InitializeNotesSchedule()
        {
            _notesSchedule.Clear();
            float[] times = {
                0.5f, 1.0f, 1.5f, 2.0f,
                2.8f, 2.8f,
                3.5f, 4.0f, 4.5f, 5.0f,
                5.8f, 5.8f,
                6.5f, 7.0f, 7.5f, 8.0f,
                8.8f, 8.8f,
                9.5f, 10.0f, 10.5f, 11.0f,
                11.8f, 11.8f,
                12.5f, 13.0f, 13.5f, 14.0f,
                14.8f, 14.8f
            };
            int[] lanes = {
                0, 1, 2, 3,
                1, 2,
                0, 3, 1, 2,
                0, 3,
                1, 2, 0, 3,
                1, 2,
                0, 3, 1, 2,
                0, 3,
                1, 2, 0, 3,
                1, 2
            };

            for (int i = 0; i < times.Length; i++)
            {
                _notesSchedule.Add(new NoteSchedule(times[i], lanes[i]));
            }
        }

        private void Update()
        {
            if (!_isPlaying) return;

            // handle abort input
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                EndMinigame(false);
                return;
            }

            if (_isGameOver)
            {
                // check retry or close
                if (!_hasWon && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    RestartMinigame();
                }
                return;
            }

            _timer += Time.deltaTime;

            // spawn scheduled notes
            for (int i = 0; i < _notesSchedule.Count; i++)
            {
                var schedule = _notesSchedule[i];
                if (!schedule.spawned && _timer >= schedule.spawnTime)
                {
                    SpawnNote(schedule.lane);
                    schedule.spawned = true;
                    _notesSchedule[i] = schedule;
                }
            }

            // move notes downwards
            for (int i = _activeNotes.Count - 1; i >= 0; i--)
            {
                var note = _activeNotes[i];
                Vector2 pos = note.rectTransform.anchoredPosition;
                pos.y -= _speed * Time.deltaTime;
                note.rectTransform.anchoredPosition = pos;

                // check miss threshold
                if (pos.y < TargetY - HitWindow)
                {
                    _misses++;
                    ShowFeedback("MISS");
                    Destroy(note.gameObject);
                    _activeNotes.RemoveAt(i);
                    UpdateScoreText();
                }
            }

            // handle lanes key inputs
            bool zPressed = Keyboard.current != null && Keyboard.current.zKey.isPressed;
            bool xPressed = Keyboard.current != null && Keyboard.current.xKey.isPressed;
            bool cPressed = Keyboard.current != null && Keyboard.current.cKey.isPressed;
            bool vPressed = Keyboard.current != null && Keyboard.current.vKey.isPressed;

            UpdateTargetZoneFeedback(0, zPressed);
            UpdateTargetZoneFeedback(1, xPressed);
            UpdateTargetZoneFeedback(2, cPressed);
            UpdateTargetZoneFeedback(3, vPressed);

            if (Keyboard.current != null)
            {
                if (Keyboard.current.zKey.wasPressedThisFrame) HandleKeyPress(0);
                if (Keyboard.current.xKey.wasPressedThisFrame) HandleKeyPress(1);
                if (Keyboard.current.cKey.wasPressedThisFrame) HandleKeyPress(2);
                if (Keyboard.current.vKey.wasPressedThisFrame) HandleKeyPress(3);
            }

            // evaluate if all notes have been spawned and processed
            bool allSpawned = true;
            for (int i = 0; i < _notesSchedule.Count; i++)
            {
                if (!_notesSchedule[i].spawned)
                {
                    allSpawned = false;
                    break;
                }
            }

            if (allSpawned && _activeNotes.Count == 0 && !_isGameOver)
            {
                EvaluateGameEnd();
            }
        }

        private void RestartMinigame()
        {
            if (_uiCanvasRoot != null)
            {
                Destroy(_uiCanvasRoot);
            }
            CleanupSprites();
            _isPlaying = false;
            StartMinigame();
        }

        private void HandleKeyPress(int lane)
        {
            NoteInstance closestNote = null;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < _activeNotes.Count; i++)
            {
                var note = _activeNotes[i];
                if (note.lane == lane && !note.isHit)
                {
                    float distance = Mathf.Abs(note.rectTransform.anchoredPosition.y - TargetY);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestNote = note;
                    }
                }
            }

            if (closestNote != null && closestDistance <= HitWindow)
            {
                closestNote.isHit = true;
                _hits++;
                ShowFeedback(closestDistance <= 25f ? "PERFECT!" : "GOOD!");
                Destroy(closestNote.gameObject);
                _activeNotes.Remove(closestNote);
                UpdateScoreText();
            }
        }

        private void SpawnNote(int lane)
        {
            float[] laneXs = { -120f, -40f, 40f, 120f };
            GameObject noteGo = new GameObject("Note_" + lane);
            noteGo.transform.SetParent(_notesContainer, false);
            Image noteImage = noteGo.AddComponent<Image>();
            noteImage.sprite = _noteSprites[lane];

            RectTransform rect = noteGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(laneXs[lane], SpawnY);
            rect.sizeDelta = new Vector2(50f, 50f);

            NoteInstance instance = new NoteInstance();
            instance.lane = lane;
            instance.gameObject = noteGo;
            instance.rectTransform = rect;
            instance.isHit = false;

            _activeNotes.Add(instance);
        }

        private void ShowFeedback(string text)
        {
            if (_feedbackText == null) return;

            _feedbackText.text = text;
            if (text == "PERFECT!")
            {
                _feedbackText.color = new Color(0.95f, 0.84f, 0.38f, 1f);
            }
            else if (text == "GOOD!")
            {
                _feedbackText.color = new Color(0.2f, 0.8f, 0.2f, 1f);
            }
            else
            {
                _feedbackText.color = new Color(0.9f, 0.2f, 0.2f, 1f);
            }

            if (_feedbackCoroutine != null)
            {
                StopCoroutine(_feedbackCoroutine);
            }
            _feedbackCoroutine = StartCoroutine(FadeFeedbackRoutine());
        }

        private IEnumerator FadeFeedbackRoutine()
        {
            float elapsed = 0f;
            float duration = 0.5f;
            _feedbackText.gameObject.SetActive(true);
            _feedbackText.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                _feedbackText.transform.localScale = Vector3.Lerp(new Vector3(1.2f, 1.2f, 1.2f), Vector3.one, t);
                Color col = _feedbackText.color;
                col.a = Mathf.Lerp(1f, 0f, t);
                _feedbackText.color = col;
                yield return null;
            }

            _feedbackText.gameObject.SetActive(false);
        }

        private void UpdateTargetZoneFeedback(int lane, bool isPressed)
        {
            if (_targetZoneImages[lane] != null)
            {
                if (isPressed)
                {
                    _targetZoneImages[lane].color = GetLaneColor(lane);
                    _targetZoneRects[lane].localScale = new Vector3(1.1f, 1.1f, 1.1f);
                }
                else
                {
                    _targetZoneImages[lane].color = new Color(0.8f, 0.8f, 0.8f, 0.6f);
                    _targetZoneRects[lane].localScale = Vector3.one;
                }
            }
        }

        private Color GetLaneColor(int lane)
        {
            Color pink = new Color(1.0f, 0.08f, 0.58f, 1f);
            Color purple = new Color(0.6f, 0.1f, 0.9f, 1f);
            float t = lane / 3f;
            return Color.Lerp(pink, purple, t);
        }

        private void UpdateScoreText()
        {
            if (_scoreText != null)
            {
                _scoreText.text = $"Hits: {_hits} / {_notesSchedule.Count}";
            }
        }

        private void EvaluateGameEnd()
        {
            _isGameOver = true;
            float targetPercentage = 0.70f;
            int requiredHits = Mathf.CeilToInt(_notesSchedule.Count * targetPercentage);

            if (_hits >= requiredHits)
            {
                _hasWon = true;
                _gameOverText.text = "SUCCESS";
                _gameOverText.color = new Color(0.2f, 0.8f, 0.2f, 1f);
                _gameOverSubtext.text = $"Completed with {_hits} hits!";
                _gameOverPanel.SetActive(true);
                StartCoroutine(SuccessDelayRoutine());
            }
            else
            {
                _hasWon = false;
                _gameOverText.text = "FAILED";
                _gameOverText.color = new Color(0.9f, 0.2f, 0.2f, 1f);
                _gameOverSubtext.text = $"Got {_hits} hits. Needed {requiredHits}.\nPress [Space] to Retry\nPress [E] to Abort";
                _gameOverPanel.SetActive(true);
            }
        }

        private IEnumerator SuccessDelayRoutine()
        {
            yield return new WaitForSeconds(1.5f);
            EndMinigame(true);
        }

        private void EndMinigame(bool completed)
        {
            _isPlaying = false;

            if (_uiCanvasRoot != null)
            {
                Destroy(_uiCanvasRoot);
            }
            CleanupSprites();

            if (PlayerMovement.Instance != null) PlayerMovement.Instance.SetCanMove(true);
            if (PlayerMovementChapt1.Instance != null) PlayerMovementChapt1.Instance.SetCanMove(true);

            if (completed)
            {
                if (Nemuri.Scenes.NocturneIntroController.Instance != null)
                {
                    // inform controller about puzzle success
                    Nemuri.Scenes.NocturneIntroController.Instance.OnPuzzle2VinesSuccess();
                }
            }
            else
            {
                if (_interactable != null)
                {
                    _interactable.enabled = true;
                }
            }
        }

        private void CreateUI()
        {
            _uiCanvasRoot = new GameObject("Vines Minigame UI");
            Canvas canvas = _uiCanvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 950;

            CanvasScaler scaler = _uiCanvasRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            _uiCanvasRoot.AddComponent<GraphicRaycaster>();
            DontDestroyOnLoad(_uiCanvasRoot);

            GameObject borderPanel = new GameObject("Border Panel");
            borderPanel.transform.SetParent(_uiCanvasRoot.transform, false);
            Image borderImage = borderPanel.AddComponent<Image>();
            borderImage.color = new Color(0.5f, 0.2f, 0.8f, 0.4f);

            RectTransform borderRect = borderPanel.GetComponent<RectTransform>();
            borderRect.anchorMin = new Vector2(0.5f, 0.5f);
            borderRect.anchorMax = new Vector2(0.5f, 0.5f);
            borderRect.pivot = new Vector2(0.5f, 0.5f);
            borderRect.sizeDelta = new Vector2(_panelWidth + 8f, _panelHeight + 8f);

            GameObject panel = new GameObject("Main Panel");
            panel.transform.SetParent(borderPanel.transform, false);
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.04f, 0.04f, 0.04f, 0.95f);

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(_panelWidth, _panelHeight);

            GameObject titleGo = new GameObject("Title Text");
            titleGo.transform.SetParent(panel.transform, false);
            Text titleText = titleGo.AddComponent<Text>();
            titleText.text = "UNTANGLE VINES";
            titleText.fontSize = 24;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = new Color(0.95f, 0.84f, 0.38f, 1f);
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.font = ResolveUiFont();

            RectTransform titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -20f);
            titleRect.sizeDelta = new Vector2(0f, 40f);

            GameObject subtitleGo = new GameObject("Subtitle Text");
            subtitleGo.transform.SetParent(panel.transform, false);
            Text subtitleText = subtitleGo.AddComponent<Text>();
            subtitleText.text = "Press Z, X, C, V as circles reach the targets";
            subtitleText.fontSize = 13;
            subtitleText.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            subtitleText.alignment = TextAnchor.MiddleCenter;
            subtitleText.font = ResolveUiFont();

            RectTransform subtitleRect = subtitleGo.GetComponent<RectTransform>();
            subtitleRect.anchorMin = new Vector2(0f, 1f);
            subtitleRect.anchorMax = new Vector2(1f, 1f);
            subtitleRect.pivot = new Vector2(0.5f, 1f);
            subtitleRect.anchoredPosition = new Vector2(0f, -55f);
            subtitleRect.sizeDelta = new Vector2(0f, 20f);

            float[] separatorXs = { -80f, 0f, 80f };
            foreach (float x in separatorXs)
            {
                GameObject sep = new GameObject("Separator");
                sep.transform.SetParent(panel.transform, false);
                Image sepImage = sep.AddComponent<Image>();
                sepImage.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);

                RectTransform sepRect = sep.GetComponent<RectTransform>();
                sepRect.anchorMin = new Vector2(0.5f, 0.5f);
                sepRect.anchorMax = new Vector2(0.5f, 0.5f);
                sepRect.pivot = new Vector2(0.5f, 0.5f);
                sepRect.anchoredPosition = new Vector2(x, -20f);
                sepRect.sizeDelta = new Vector2(2f, 520f);
            }

            float[] laneXs = { -120f, -40f, 40f, 120f };
            string[] keys = { "Z", "X", "C", "V" };

            for (int i = 0; i < 4; i++)
            {
                GameObject targetZone = new GameObject("Target Zone " + i);
                targetZone.transform.SetParent(panel.transform, false);
                _targetZoneImages[i] = targetZone.AddComponent<Image>();
                _targetZoneImages[i].sprite = _targetSprite;
                _targetZoneImages[i].color = new Color(0.8f, 0.8f, 0.8f, 0.6f);

                _targetZoneRects[i] = targetZone.GetComponent<RectTransform>();
                _targetZoneRects[i].anchorMin = new Vector2(0.5f, 0.5f);
                _targetZoneRects[i].anchorMax = new Vector2(0.5f, 0.5f);
                _targetZoneRects[i].pivot = new Vector2(0.5f, 0.5f);
                _targetZoneRects[i].anchoredPosition = new Vector2(laneXs[i], TargetY);
                _targetZoneRects[i].sizeDelta = new Vector2(60f, 60f);

                GameObject labelGo = new GameObject("Key Label " + i);
                labelGo.transform.SetParent(panel.transform, false);
                Text labelText = labelGo.AddComponent<Text>();
                labelText.text = keys[i];
                labelText.fontSize = 14;
                labelText.fontStyle = FontStyle.Bold;
                labelText.color = new Color(0.6f, 0.6f, 0.6f, 1f);
                labelText.alignment = TextAnchor.MiddleCenter;
                labelText.font = ResolveUiFont();

                RectTransform labelRect = labelGo.GetComponent<RectTransform>();
                labelRect.anchorMin = new Vector2(0.5f, 0.5f);
                labelRect.anchorMax = new Vector2(0.5f, 0.5f);
                labelRect.pivot = new Vector2(0.5f, 0.5f);
                labelRect.anchoredPosition = new Vector2(laneXs[i], TargetY - 45f);
                labelRect.sizeDelta = new Vector2(40f, 20f);
            }

            GameObject containerGo = new GameObject("Notes Container");
            containerGo.transform.SetParent(panel.transform, false);
            _notesContainer = containerGo.AddComponent<RectTransform>();
            _notesContainer.anchorMin = Vector2.zero;
            _notesContainer.anchorMax = Vector2.one;
            _notesContainer.pivot = new Vector2(0.5f, 0.5f);
            _notesContainer.offsetMin = Vector2.zero;
            _notesContainer.offsetMax = Vector2.zero;

            GameObject scoreGo = new GameObject("Score Text");
            scoreGo.transform.SetParent(panel.transform, false);
            _scoreText = scoreGo.AddComponent<Text>();
            _scoreText.text = "Hits: 0 / 30";
            _scoreText.fontSize = 16;
            _scoreText.fontStyle = FontStyle.Bold;
            _scoreText.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            _scoreText.alignment = TextAnchor.MiddleCenter;
            _scoreText.font = ResolveUiFont();

            RectTransform scoreRect = scoreGo.GetComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(0f, 1f);
            scoreRect.anchorMax = new Vector2(1f, 1f);
            scoreRect.pivot = new Vector2(0.5f, 1f);
            scoreRect.anchoredPosition = new Vector2(0f, -80f);
            scoreRect.sizeDelta = new Vector2(0f, 24f);

            GameObject feedbackGo = new GameObject("Feedback Text");
            feedbackGo.transform.SetParent(panel.transform, false);
            _feedbackText = feedbackGo.AddComponent<Text>();
            _feedbackText.text = "";
            _feedbackText.fontSize = 28;
            _feedbackText.fontStyle = FontStyle.Bold;
            _feedbackText.alignment = TextAnchor.MiddleCenter;
            _feedbackText.font = ResolveUiFont();
            _feedbackText.gameObject.SetActive(false);

            RectTransform feedbackRect = feedbackGo.GetComponent<RectTransform>();
            feedbackRect.anchorMin = new Vector2(0.5f, 0.5f);
            feedbackRect.anchorMax = new Vector2(0.5f, 0.5f);
            feedbackRect.pivot = new Vector2(0.5f, 0.5f);
            feedbackRect.anchoredPosition = new Vector2(0f, 0f);
            feedbackRect.sizeDelta = new Vector2(300f, 50f);

            GameObject helpGo = new GameObject("Help Text");
            helpGo.transform.SetParent(panel.transform, false);
            Text helpText = helpGo.AddComponent<Text>();
            helpText.text = "[E] Abort Minigame";
            helpText.fontSize = 13;
            helpText.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            helpText.alignment = TextAnchor.MiddleCenter;
            helpText.font = ResolveUiFont();

            RectTransform helpRect = helpGo.GetComponent<RectTransform>();
            helpRect.anchorMin = new Vector2(0f, 0f);
            helpRect.anchorMax = new Vector2(1f, 0f);
            helpRect.pivot = new Vector2(0.5f, 0f);
            helpRect.anchoredPosition = new Vector2(0f, 15f);
            helpRect.sizeDelta = new Vector2(0f, 20f);

            _gameOverPanel = new GameObject("Game Over Panel");
            _gameOverPanel.transform.SetParent(panel.transform, false);
            Image goImage = _gameOverPanel.AddComponent<Image>();
            goImage.color = new Color(0f, 0f, 0f, 0.9f);

            RectTransform goRect = _gameOverPanel.GetComponent<RectTransform>();
            goRect.anchorMin = Vector2.zero;
            goRect.anchorMax = Vector2.one;
            goRect.pivot = new Vector2(0.5f, 0.5f);
            goRect.offsetMin = Vector2.zero;
            goRect.offsetMax = Vector2.zero;

            GameObject goTextGo = new GameObject("Game Over Text");
            goTextGo.transform.SetParent(_gameOverPanel.transform, false);
            _gameOverText = goTextGo.AddComponent<Text>();
            _gameOverText.text = "FAILED";
            _gameOverText.fontSize = 36;
            _gameOverText.fontStyle = FontStyle.Bold;
            _gameOverText.color = Color.red;
            _gameOverText.alignment = TextAnchor.MiddleCenter;
            _gameOverText.font = ResolveUiFont();

            RectTransform goTextRect = goTextGo.GetComponent<RectTransform>();
            goTextRect.anchorMin = new Vector2(0.5f, 0.5f);
            goTextRect.anchorMax = new Vector2(0.5f, 0.5f);
            goTextRect.pivot = new Vector2(0.5f, 0.5f);
            goTextRect.anchoredPosition = new Vector2(0f, 40f);
            goTextRect.sizeDelta = new Vector2(300f, 50f);

            GameObject goSubtextGo = new GameObject("Game Over Subtext");
            goSubtextGo.transform.SetParent(_gameOverPanel.transform, false);
            _gameOverSubtext = goSubtextGo.AddComponent<Text>();
            _gameOverSubtext.text = "Press [Space] to Retry\nPress [E] to Abort";
            _gameOverSubtext.fontSize = 16;
            _gameOverSubtext.color = Color.white;
            _gameOverSubtext.alignment = TextAnchor.MiddleCenter;
            _gameOverSubtext.font = ResolveUiFont();
            _gameOverSubtext.lineSpacing = 1.2f;

            RectTransform goSubtextRect = goSubtextGo.GetComponent<RectTransform>();
            goSubtextRect.anchorMin = new Vector2(0.5f, 0.5f);
            goSubtextRect.anchorMax = new Vector2(0.5f, 0.5f);
            goSubtextRect.pivot = new Vector2(0.5f, 0.5f);
            goSubtextRect.anchoredPosition = new Vector2(0f, -40f);
            goSubtextRect.sizeDelta = new Vector2(300f, 80f);

            _gameOverPanel.SetActive(false);
        }

        private Font ResolveUiFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            }
            return font;
        }

        private Sprite CreateCircleSprite(int radius, Color color, bool outlineOnly, int outlineWidth)
        {
            int size = radius * 2;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius));
                    if (dist < radius)
                    {
                        if (outlineOnly)
                        {
                            if (dist >= radius - outlineWidth)
                            {
                                float alpha = 1f;
                                if (dist < radius - outlineWidth + 1f)
                                {
                                    alpha = dist - (radius - outlineWidth);
                                }
                                else if (dist > radius - 1f)
                                {
                                    alpha = radius - dist;
                                }
                                Color col = color;
                                col.a *= alpha;
                                texture.SetPixel(x, y, col);
                            }
                            else
                            {
                                texture.SetPixel(x, y, Color.clear);
                            }
                        }
                        else
                        {
                            float alpha = 1f;
                            if (dist > radius - 1.5f)
                            {
                                alpha = radius - dist;
                            }
                            Color col = color;
                            col.a *= alpha;
                            texture.SetPixel(x, y, col);
                        }
                    }
                    else
                    {
                        texture.SetPixel(x, y, Color.clear);
                    }
                }
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}
