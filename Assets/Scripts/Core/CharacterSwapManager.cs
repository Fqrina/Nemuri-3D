using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Nemuri.Dialogue;
using Nemuri.Player;

namespace Nemuri.Core
{
    public class CharacterSwapManager : MonoBehaviour
    {
        [System.Serializable]
        public class CharacterBinding
        {
            public string characterName;
            public GameObject playerObject;
            public GameObject npcObject;
        }

        public static CharacterSwapManager Instance { get; private set; }

        [Header("Character Configurations")]
        [SerializeField] private List<CharacterBinding> _characters = new List<CharacterBinding>();
        [SerializeField] private int _activeCharacterIndex = 0;
        public int ActiveCharacterIndex => _activeCharacterIndex;

        // Audio Volume (Set manually between 0.0f and 1.0f)
        private float _crystalBlinkVolume = 10.0f;

        [Header("Character Swap UI Configuration")]
        [SerializeField] private Vector2 _uiImageSize = new Vector2(200f, 200f);
        [SerializeField] private Vector2 _uiImagePosition = new Vector2(-50f, 0f);

        private GameObject _uiCanvasObject;
        private Image _uiImageComponent;

        public GameObject GetActivePlayerObject()
        {
            if (_activeCharacterIndex >= 0 && _activeCharacterIndex < _characters.Count)
            {
                return _characters[_activeCharacterIndex].playerObject;
            }
            return null;
        }

        public string GetActiveCharacterName()
        {
            if (_activeCharacterIndex >= 0 && _activeCharacterIndex < _characters.Count)
            {
                string name = _characters[_activeCharacterIndex].characterName;
                return name.Replace("CHARA", "").Replace("chara", "").Trim();
            }
            return "PLAYER";
        }

        [Header("Camera Configurations")]
        [SerializeField] private FixedWorldOffsetCamera _followCamera;

        private int _characterIndexBeforeDialogue = 0;
        private float _guideLightCooldown = 0f;

        [Header("Unlock States")]
        [SerializeField] private bool[] _unlockedCharacters = new bool[] { true, true, true, false, false };

        public void SetCharacterUnlocked(int index, bool unlocked)
        {
            if (index >= 0 && index < _unlockedCharacters.Length)
            {
                _unlockedCharacters[index] = unlocked;
                EnsureUIInitialized();
                UpdateCharacterImageSprite();
            }
        }

        public bool IsCharacterUnlocked(int index)
        {
            if (index >= 0 && index < _unlockedCharacters.Length)
            {
                return _unlockedCharacters[index];
            }
            return false;
        }

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
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "chpt3")
            {
                for (int i = 0; i < _unlockedCharacters.Length; i++)
                {
                    _unlockedCharacters[i] = true;
                }
            }
            InitializeCharacters();
            EnsureUIInitialized();
        }

        private void OnEnable()
        {
            DialogueManager.OnConversationStart += HandleConversationStart;
            DialogueManager.OnConversationEnd += HandleConversationEnd;
            DialogueManager.OnConversationStart += RefreshCharacterImage;
            DialogueManager.OnConversationEnd += RefreshCharacterImage;
        }

        private void OnDisable()
        {
            DialogueManager.OnConversationStart -= HandleConversationStart;
            DialogueManager.OnConversationEnd -= HandleConversationEnd;
            DialogueManager.OnConversationStart -= RefreshCharacterImage;
            DialogueManager.OnConversationEnd -= RefreshCharacterImage;
        }

        private void RefreshCharacterImage()
        {
            EnsureUIInitialized();
            UpdateCharacterImageSprite();
        }

        private void Update()
        {
            EnsureUIInitialized();
            UpdateCharacterImageTransform();
            UpdateCharacterImageSprite();

            if (_guideLightCooldown > 0f)
            {
                _guideLightCooldown -= Time.deltaTime;
            }

            // Keiko's Guide Light keypress check (T key)
            if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
            {
                // Unlocked only when intro is completed (bunny dialog is done), active character is Keiko (3), and cooldown is ready
                if (Nemuri.Scenes.NocturneIntroController.IsIntroCompleted && 
                    _activeCharacterIndex == 3 && 
                    _guideLightCooldown <= 0f)
                {
                    GameObject target = FindNextPuzzleTarget();
                    if (target != null)
                    {
                        _guideLightCooldown = 7f;
                        SpawnGuideLight(_characters[3].playerObject.transform.position, target.transform.position);
                        Debug.Log("[CharacterSwapManager] Keiko cast Guide Light targeting: " + target.name);

                        AudioClip blinkClip = Resources.Load<AudioClip>("CrystalBlink");
                        if (blinkClip != null)
                        {
                            AudioSource.PlayClipAtPoint(blinkClip, _characters[3].playerObject.transform.position, _crystalBlinkVolume);
                        }
                    }
                }
            }
            // Determine if character swapping is currently allowed/unlocked
            bool isDialogueActive = (DialogueManager.Instance != null && DialogueManager.Instance.IsConversationActive);
            bool canSwap = false;
            if (!isDialogueActive)
            {
                if (Nemuri.Scenes.NocturneIntroController.Instance != null)
                {
                    canSwap = Nemuri.Scenes.NocturneIntroController.CanSwapTo(0);
                }
                else
                {
                    canSwap = true; // Fallback default
                }
            }

            // Hide NPCs when character swapping is unlocked (free roaming/play mode)
            if (canSwap)
            {
                for (int i = 0; i < _characters.Count; i++)
                {
                    if (_characters[i].npcObject == null || !_characters[i].npcObject.scene.IsValid())
                    {
                        _characters[i].npcObject = GetNpcObject(i);
                    }

                    if (_characters[i].npcObject != null && _characters[i].npcObject.activeSelf)
                    {
                        _characters[i].npcObject.SetActive(false);
                    }
                }

                HandleInput();
            }
            else
            {
                // Show NPCs (except the active player's own NPC representation) when swapping is locked (cutscenes/dialogue/story walks)
                for (int i = 0; i < _characters.Count; i++)
                {
                    if (_characters[i].npcObject == null || !_characters[i].npcObject.scene.IsValid())
                    {
                        _characters[i].npcObject = GetNpcObject(i);
                    }

                    if (_characters[i].npcObject != null)
                    {
                        bool shouldBeActive = (i != _activeCharacterIndex);
                        if (i == 2 && Nemuri.Scenes.NocturneIntroController.Instance != null && 
                            !Nemuri.Scenes.NocturneIntroController.Instance.HasMurialFallen && 
                            !Nemuri.Scenes.NocturneIntroController.Instance.IsMurialFalling)
                        {
                            shouldBeActive = false;
                        }

                        if (_characters[i].npcObject.activeSelf != shouldBeActive)
                        {
                            _characters[i].npcObject.SetActive(shouldBeActive);
                        }
                    }
                }
            }
        }

        private GameObject GetNpcObject(int i)
        {
            GameObject result = null;
            if (Nemuri.Scenes.NocturneIntroController.Instance != null)
            {
                if (i == 1) result = Nemuri.Scenes.NocturneIntroController.Instance.RonaNpc;
                else if (i == 2) result = Nemuri.Scenes.NocturneIntroController.Instance.MurialNpc;
                else if (i == 3) result = Nemuri.Scenes.NocturneIntroController.Instance.KeikoNpc;
                else if (i == 4) result = Nemuri.Scenes.NocturneIntroController.Instance.FeanorNpc;
            }

            if (result != null && result.scene.IsValid())
            {
                return result;
            }

            string searchName = i == 0 ? "KAELNPC" : _characters[i].characterName.Replace("CHARA", "").Trim() + "NPC";
            result = FindSceneObjectByName(searchName);
            if (result == null)
            {
                result = FindSceneObjectByName(searchName.Replace("NPC", " NPC"));
            }
            return result;
        }

        private GameObject GetPlayerObject(int i)
        {
            string searchName = _characters[i].characterName.Replace("CHARA", "").Trim();
            GameObject result = FindSceneObjectByName(searchName);
            if (result == null)
            {
                result = FindSceneObjectByName(searchName.ToLower());
            }
            return result;
        }

        private GameObject FindSceneObjectByName(string name)
        {
            string lower = name.ToLower();
            foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go.hideFlags == HideFlags.NotEditable || go.hideFlags == HideFlags.HideAndDontSave)
                    continue;

                #if UNITY_EDITOR
                if (UnityEditor.EditorUtility.IsPersistent(go.transform.root.gameObject))
                    continue;
                #endif

                if (go.scene.IsValid() && go.name.ToLower() == lower)
                {
                    return go;
                }
            }
            return null;
        }

        private void InitializeCharacters()
        {
            if (_characters == null || _characters.Count == 0)
            {
                return;
            }

            for (int i = 0; i < _characters.Count; i++)
            {
                bool isActive = (i == _activeCharacterIndex);
                
                // Automatically find npcObject if null or prefab in Inspector
                if (_characters[i].npcObject == null || !_characters[i].npcObject.scene.IsValid())
                {
                    _characters[i].npcObject = GetNpcObject(i);
                }

                // Automatically find playerObject if null or prefab in Inspector
                if (_characters[i].playerObject == null || !_characters[i].playerObject.scene.IsValid())
                {
                    _characters[i].playerObject = GetPlayerObject(i);
                }

                if (_characters[i].playerObject != null)
                {
                    _characters[i].playerObject.SetActive(isActive);
                }

                if (_characters[i].npcObject != null)
                {
                    _characters[i].npcObject.SetActive(!isActive);
                    if (!isActive)
                    {
                        bool isMurialWaitingToFall = (_characters[i].npcObject.name == "MURIALNPC" || _characters[i].npcObject.name == "MURIAL NPC") &&
                            (Nemuri.Scenes.NocturneIntroController.Instance != null && !Nemuri.Scenes.NocturneIntroController.Instance.HasMurialFallen);

                        if (!isMurialWaitingToFall)
                        {
                            SnapToGround(_characters[i].npcObject);
                        }
                    }
                }
            }

            if (_activeCharacterIndex >= 0 && _activeCharacterIndex < _characters.Count && _characters[_activeCharacterIndex].playerObject != null)
            {
                UpdateCameraTargets(_characters[_activeCharacterIndex].playerObject.transform);
            }
        }

        private void HandleInput()
        {
            if (Keyboard.current == null)
            {
                return;
            }

            bool bypassIntroLock = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "chpt3";

            if (Keyboard.current.digit1Key.wasPressedThisFrame && IsCharacterUnlocked(0) && (bypassIntroLock || Nemuri.Scenes.NocturneIntroController.CanSwapTo(0))) SwapToCharacter(0);
            else if (Keyboard.current.digit2Key.wasPressedThisFrame && IsCharacterUnlocked(1) && (bypassIntroLock || Nemuri.Scenes.NocturneIntroController.CanSwapTo(1))) SwapToCharacter(1);
            else if (Keyboard.current.digit3Key.wasPressedThisFrame && IsCharacterUnlocked(2) && (bypassIntroLock || Nemuri.Scenes.NocturneIntroController.CanSwapTo(2))) SwapToCharacter(2);
            else if (Keyboard.current.digit4Key.wasPressedThisFrame && IsCharacterUnlocked(3) && (bypassIntroLock || Nemuri.Scenes.NocturneIntroController.CanSwapTo(3))) SwapToCharacter(3);
            else if (Keyboard.current.digit5Key.wasPressedThisFrame && IsCharacterUnlocked(4) && (bypassIntroLock || Nemuri.Scenes.NocturneIntroController.CanSwapTo(4))) SwapToCharacter(4);
        }

        public void SwapToCharacter(int index, bool isDialogueSwap = false)
        {
            if (_characters == null || index < 0 || index >= _characters.Count)
            {
                return;
            }

            if (index == _activeCharacterIndex)
            {
                return;
            }

            // Ensure player objects are valid scene instances
            if (_characters[_activeCharacterIndex].playerObject == null || !_characters[_activeCharacterIndex].playerObject.scene.IsValid())
            {
                _characters[_activeCharacterIndex].playerObject = GetPlayerObject(_activeCharacterIndex);
            }
            if (_characters[index].playerObject == null || !_characters[index].playerObject.scene.IsValid())
            {
                _characters[index].playerObject = GetPlayerObject(index);
            }

            GameObject currentCharacterObj = _characters[_activeCharacterIndex].playerObject;
            GameObject targetCharacterObj = _characters[index].playerObject;

            if (currentCharacterObj == null || targetCharacterObj == null)
            {
                return;
            }

            // In-place mesh swap: target character takes current character's position and rotation
            targetCharacterObj.transform.position = currentCharacterObj.transform.position;
            targetCharacterObj.transform.rotation = currentCharacterObj.transform.rotation;
            currentCharacterObj.SetActive(false);
            targetCharacterObj.SetActive(true);

            // Ensure npcObjects are valid scene instances
            if (_characters[_activeCharacterIndex].npcObject == null || !_characters[_activeCharacterIndex].npcObject.scene.IsValid())
            {
                _characters[_activeCharacterIndex].npcObject = GetNpcObject(_activeCharacterIndex);
            }
            if (_characters[index].npcObject == null || !_characters[index].npcObject.scene.IsValid())
            {
                _characters[index].npcObject = GetNpcObject(index);
            }

            // Toggle companion NPC active states
            if (_characters[_activeCharacterIndex].npcObject != null)
            {
                _characters[_activeCharacterIndex].npcObject.SetActive(true);
                if (!isDialogueSwap)
                {
                    _characters[_activeCharacterIndex].npcObject.transform.position = currentCharacterObj.transform.position;
                    SnapToGround(_characters[_activeCharacterIndex].npcObject);
                }
            }
            if (_characters[index].npcObject != null)
            {
                _characters[index].npcObject.SetActive(false);
            }

            UpdateCameraTargets(targetCharacterObj.transform);

            _activeCharacterIndex = index;
            EnsureUIInitialized();
            UpdateCharacterImageSprite();
        }

        private void SetNpcPositionAndRotation(GameObject npc, Vector3 position, Quaternion rotation)
        {
            if (npc == null) return;
            var cc = npc.GetComponent<CharacterController>();
            if (cc == null) cc = npc.GetComponentInChildren<CharacterController>();

            bool ccWasEnabled = false;
            if (cc != null)
            {
                ccWasEnabled = cc.enabled;
                cc.enabled = false;
            }

            npc.transform.position = position;
            npc.transform.rotation = rotation;

            if (cc != null)
            {
                cc.enabled = ccWasEnabled;
            }
        }

        private void SnapToGround(GameObject npc)
        {
            if (npc == null) return;
            Vector3 pos = npc.transform.position;
            pos.y = Nemuri.Scenes.NocturneIntroController.GetGroundHeight(pos);
            npc.transform.position = pos;
        }

        private void UpdateCameraTargets(Transform targetTransform)
        {
            if (_followCamera != null)
            {
                _followCamera.target = targetTransform;
            }

            var virtualCameras = FindObjectsByType<Cinemachine.CinemachineVirtualCamera>();
            foreach (var vCam in virtualCameras)
            {
                if (vCam != null)
                {
                    vCam.Follow = targetTransform;
                    vCam.LookAt = targetTransform;
                }
            }
        }

        public void ResetSwapStateToKael()
        {
            _characterIndexBeforeDialogue = 0;
            if (_activeCharacterIndex != 0)
            {
                SwapToCharacter(0, isDialogueSwap: true);
            }
        }

        private void HandleConversationStart()
        {
            _characterIndexBeforeDialogue = _activeCharacterIndex;

            if (_activeCharacterIndex != 0)
            {
                SwapToCharacter(0, isDialogueSwap: true);
            }
        }

        private void HandleConversationEnd()
        {
            if (_activeCharacterIndex != _characterIndexBeforeDialogue)
            {
                SwapToCharacter(_characterIndexBeforeDialogue, isDialogueSwap: true);
            }
        }

        private void SpawnGuideLight(Vector3 startPosition, Vector3 targetPosition)
        {
            GameObject lightObj = new GameObject("GuideLight");
            lightObj.transform.position = startPosition;

            // Add point light component
            Light lightComponent = lightObj.AddComponent<Light>();
            lightComponent.type = LightType.Point;
            lightComponent.color = Color.cyan;
            lightComponent.intensity = 5f;
            lightComponent.range = 15f;

            // Add simple visual sphere representation
            GameObject sphereVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphereVisual.transform.SetParent(lightObj.transform);
            sphereVisual.transform.localPosition = Vector3.zero;
            sphereVisual.transform.localScale = Vector3.one * 0.4f;
            Destroy(sphereVisual.GetComponent<Collider>());

            var renderer = sphereVisual.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material glowMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                glowMat.SetColor("_BaseColor", Color.cyan);
                glowMat.EnableKeyword("_EMISSION");
                glowMat.SetColor("_EmissionColor", Color.cyan * 3f);
                renderer.sharedMaterial = glowMat;
            }

            StartCoroutine(MoveGuideLightRoutine(lightObj, startPosition, targetPosition, lightComponent, renderer));
        }

        private System.Collections.IEnumerator MoveGuideLightRoutine(GameObject lightObj, Vector3 start, Vector3 target, Light lightComp, Renderer visualRenderer)
        {
            float duration = 2.0f; // Fly time
            float elapsed = 0f;

            Vector3 travelDir = target - start;
            Vector3 perpendicular = Vector3.Cross(travelDir, Vector3.up).normalized;
            if (perpendicular == Vector3.zero) perpendicular = Vector3.right;
            
            float sideOffset = Random.Range(-4f, 4f);
            float heightOffset = Random.Range(3f, 6f);

            while (elapsed < duration)
            {
                if (lightObj == null) yield break;
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float tSmooth = Mathf.SmoothStep(0f, 1f, t);

                Vector3 currentPos = Vector3.Lerp(start, target, tSmooth);
                float sinValue = Mathf.Sin(t * Mathf.PI);
                currentPos += perpendicular * sideOffset * sinValue;
                currentPos += Vector3.up * heightOffset * sinValue;

                lightObj.transform.position = currentPos;
                yield return null;
            }

            if (lightObj == null) yield break;
            lightObj.transform.position = target;

            // Stay at the target for 3 seconds
            yield return new WaitForSeconds(3.0f);

            // Fade out the light and visual emission over 1 second
            float fadeDuration = 1.0f;
            float fadeElapsed = 0f;
            float startIntensity = lightComp != null ? lightComp.intensity : 5f;
            Material mat = visualRenderer != null ? visualRenderer.material : null;
            Color startColor = Color.cyan;

            while (fadeElapsed < fadeDuration)
            {
                if (lightObj == null) yield break;
                fadeElapsed += Time.deltaTime;
                float tFade = fadeElapsed / fadeDuration;

                if (lightComp != null)
                {
                    lightComp.intensity = Mathf.Lerp(startIntensity, 0f, tFade);
                }

                if (mat != null)
                {
                    Color fadedColor = Color.Lerp(startColor, new Color(startColor.r, startColor.g, startColor.b, 0f), tFade);
                    mat.SetColor("_BaseColor", fadedColor);
                    mat.SetColor("_EmissionColor", fadedColor * (1f - tFade));
                }

                yield return null;
            }

            if (lightObj != null)
            {
                Destroy(lightObj);
            }
        }

        private GameObject FindNextPuzzleTarget()
        {
            var intro = Nemuri.Scenes.NocturneIntroController.Instance;
            if (intro != null)
            {
                if (intro.HasPuzzle3Collected)
                {
                    if (!intro.HasBunnyDialogueEnded)
                    {
                        GameObject pg = GameObject.Find("PINEALGLAND");
                        if (pg != null)
                        {
                            Transform go1 = pg.transform.Find("GameObject (1)");
                            if (go1 != null)
                            {
                                Transform metarig = go1.Find("metarig");
                                if (metarig != null) return metarig.gameObject;
                            }
                        }
                    }
                    else if (!intro.HasPortalFixed)
                    {
                        GameObject pg = GameObject.Find("PINEALGLAND");
                        if (pg != null)
                        {
                            Transform cube015 = FindChildRecursiveTransform(pg.transform, "cube 015");
                            if (cube015 == null) cube015 = FindChildRecursiveTransform(pg.transform, "cube.015");
                            if (cube015 != null) return cube015.gameObject;
                        }
                        GameObject directCube = GameObject.Find("cube 015");
                        if (directCube == null) directCube = GameObject.Find("cube.015");
                        if (directCube != null) return directCube;
                    }
                }
            }

            GameObject target1 = FindCrystalByName("dobj.001");
            if (target1 != null && target1.activeSelf)
            {
                return target1;
            }

            GameObject target2 = FindCrystalByName("dobj");
            if (target2 != null && target2.activeSelf)
            {
                return target2;
            }

            GameObject target3 = FindCrystalByName("dobj.002");
            if (target3 != null && target3.activeSelf)
            {
                return target3;
            }

            return null;
        }

        private GameObject FindCrystalByName(string name)
        {
            GameObject pg = GameObject.Find("PINEALGLAND");
            if (pg != null)
            {
                Transform found = FindChildRecursiveTransform(pg.transform, name);
                if (found != null) return found.gameObject;
            }
            return GameObject.Find(name);
        }

        private Transform FindChildRecursiveTransform(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform result = FindChildRecursiveTransform(parent.GetChild(i), name);
                if (result != null) return result;
            }
            return null;
        }

        private void EnsureUIInitialized()
        {
            if (_uiImageComponent != null && _uiCanvasObject != null)
            {
                return;
            }

            _uiCanvasObject = new GameObject("CharacterSwapUI_Canvas");
            Canvas canvas = _uiCanvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 800; // Render behind dialogue (900) but on top of normal UI

            CanvasScaler scaler = _uiCanvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            _uiCanvasObject.AddComponent<GraphicRaycaster>();

            GameObject imageObj = new GameObject("ActiveCharacter_Image");
            imageObj.transform.SetParent(_uiCanvasObject.transform, false);

            _uiImageComponent = imageObj.AddComponent<Image>();
            
            // Configure RectTransform for right-alignment (Middle-Right anchor and pivot)
            RectTransform rectTransform = _uiImageComponent.rectTransform;
            rectTransform.anchorMin = new Vector2(1f, 0.5f);
            rectTransform.anchorMax = new Vector2(1f, 0.5f);
            rectTransform.pivot = new Vector2(1f, 0.5f);
            
            UpdateCharacterImageTransform();
            UpdateCharacterImageSprite();
        }

        private void UpdateCharacterImageTransform()
        {
            if (_uiImageComponent != null)
            {
                RectTransform rectTransform = _uiImageComponent.rectTransform;
                if (rectTransform != null)
                {
                    rectTransform.sizeDelta = _uiImageSize;
                    rectTransform.anchoredPosition = _uiImagePosition;
                }
            }
        }

        private Sprite LoadSprite(string path)
        {
            Sprite sprite = Resources.Load<Sprite>(path);
            if (sprite != null)
            {
                return sprite;
            }
            Texture2D tex = Resources.Load<Texture2D>(path);
            if (tex != null)
            {
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
            return null;
        }

        private void UpdateCharacterImageSprite()
        {
            if (_uiImageComponent == null) return;

            // 1. Calculate how many characters are unlocked
            int unlockedCount = 0;
            for (int i = 0; i < _unlockedCharacters.Length; i++)
            {
                if (_unlockedCharacters[i])
                {
                    unlockedCount++;
                }
            }

            // 2. Decide the prefix (3p or 5p) based on unlockedCount
            string prefix = "";
            if (unlockedCount == 3)
            {
                prefix = "3p";
            }
            else if (unlockedCount == 5)
            {
                prefix = "5p";
            }

            if (string.IsNullOrEmpty(prefix))
            {
                // If neither 3 nor 5, hide the image
                _uiImageComponent.gameObject.SetActive(false);
                return;
            }

            // 3. Check for dialogue, narrative, scene transition, or menu hiding conditions
            bool isDialogueActive = (DialogueManager.Instance != null && DialogueManager.Instance.IsConversationActive);
            bool isTransitioning = (Nemuri.UI.ScreenFader.Instance != null && Nemuri.UI.ScreenFader.Instance.CurrentAlpha > 0.01f);
            
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            bool isMenu = activeScene.name.ToLower().Contains("mainmenu") || activeScene.name.ToLower().Contains("menu");

            if (isDialogueActive || isTransitioning || isMenu)
            {
                _uiImageComponent.gameObject.SetActive(false);
                return;
            }

            // 4. Determine the active character image index (1-indexed)
            int imageIndex = _activeCharacterIndex + 1; // 1-indexed (1 to 5)

            if (unlockedCount == 3 && imageIndex > 3)
            {
                _uiImageComponent.gameObject.SetActive(false);
                return;
            }
            if (unlockedCount == 5 && imageIndex > 5)
            {
                _uiImageComponent.gameObject.SetActive(false);
                return;
            }

            string resourcePath = "PlayerInfo/" + prefix + "-" + imageIndex;

            // 5. Load and display sprite
            Sprite characterSprite = LoadSprite(resourcePath);

            if (characterSprite != null)
            {
                _uiImageComponent.sprite = characterSprite;
                _uiImageComponent.gameObject.SetActive(true);
            }
            else
            {
                _uiImageComponent.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (_uiCanvasObject != null)
            {
                Destroy(_uiCanvasObject);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying && _uiImageComponent != null)
            {
                UpdateCharacterImageTransform();
                UpdateCharacterImageSprite();
            }
        }
#endif
    }
}
