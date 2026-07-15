using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Nemuri.Interactions;

public class BridgeController : MonoBehaviour
{
    [Header("Detection Settings")]
    public float activationDistance = 3.0f;
    [SerializeField] private string[] targetCharacterNames = { "KEIKOCHARA" };

    [Header("Hold Interaction")]
    public float holdDuration = 3f;

    [SerializeField] private string[] allCharacterNames = { "KEIKOCHARA", "Player2" };

    private Animator[] childAnimators;
    private BoxCollider[] childColliders;
    private bool isTriggered = false;
    private Transform activePlayerTransform;
    private float searchTimer = 0f;
    private float _holdTimer;
    private Interactable _interactable;
    private bool _hasShownWrongPlayerError;

    void Start()
    {
        FindActivePlayer();

        childAnimators = GetComponentsInChildren<Animator>(true);
        childColliders = GetComponentsInChildren<BoxCollider>(true);

        foreach (var anim in childAnimators)
        {
            anim.enabled = false;
        }

        foreach (var col in childColliders)
        {
            if (col.gameObject.name.Contains("008") || 
                col.gameObject.name.Contains("009") || 
                col.gameObject.name.Contains("010"))
            {
                col.enabled = false;
            }
            
            if (col.gameObject.name.Contains("012"))
            {
                col.enabled = true;
            }
        }

        _interactable = GetComponent<Interactable>();
        if (_interactable != null)
        {
            _interactable.enabled = false;
        }
    }

    void Update()
    {
        if (isTriggered) return;

        // Check if Crescent Tear is collected
        if (Nemuri.Scenes.NocturneIntroController.Instance != null && !Nemuri.Scenes.NocturneIntroController.Instance.HasCrescentTearCollected)
        {
            if (_interactable != null) _interactable.enabled = false;
            HideInteraction();
            return;
        }

        if (activePlayerTransform == null || !activePlayerTransform.gameObject.activeInHierarchy)
        {
            searchTimer += Time.deltaTime;
            if (searchTimer >= 0.5f)
            {
                FindActivePlayer();
                searchTimer = 0f;
            }
            HideInteraction();
            return;
        }

        float distance = Vector3.Distance(transform.position, activePlayerTransform.position);

        if (distance <= activationDistance)
        {
            var intro = Nemuri.Scenes.NocturneIntroController.Instance;
            if (intro != null)
            {
                if (_interactable != null) _interactable.enabled = true;

                if (!intro.HasBridgeIntroStarted)
                {
                    _interactable?.DisplayInteraction("Press E to Interact", 0f);
                    if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                    {
                        intro.OnBridgeInteracted();
                        _interactable?.DismissInteraction();
                    }
                }
                else if (intro.HasBridgeIntroEnded && !intro.HasBridgeFixed)
                {
                    bool isRona = (Nemuri.Core.CharacterSwapManager.Instance != null && Nemuri.Core.CharacterSwapManager.Instance.ActiveCharacterIndex == 1);

                    if (isRona)
                    {
                        if (Keyboard.current != null && Keyboard.current.eKey.isPressed)
                        {
                            _holdTimer += Time.deltaTime;
                            _interactable?.DisplayInteraction("Press E to fix bridge", _holdTimer / holdDuration);

                            if (_holdTimer >= holdDuration)
                            {
                                _holdTimer = 0f;
                                _interactable?.DismissInteraction();
                                isTriggered = true;
                                intro.OnBridgeFixedByRona(this);
                            }
                        }
                        else
                        {
                            if (_holdTimer > 0f)
                            {
                                HideInteraction();
                            }
                            else
                            {
                                _interactable?.DisplayInteraction("Press E to fix bridge", 0f);
                            }
                        }
                    }
                    else
                    {
                        _interactable?.DisplayInteraction("Press E to fix bridge", 0f);
                        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                        {
                            _interactable?.DisplayInteraction("You must use Rona as player to interact", 0f);
                        }
                    }
                }
            }
        }
        else
        {
            HideInteraction();
        }
    }

    private void HideInteraction()
    {
        _interactable?.DismissInteraction();
        _holdTimer = 0f;
    }

    void FindActivePlayer()
    {
        Transform closest = null;
        float closestDist = float.MaxValue;

        foreach (string charName in allCharacterNames)
        {
            GameObject obj = GameObject.Find(charName);
            if (obj != null && obj.activeInHierarchy)
            {
                float dist = Vector3.Distance(transform.position, obj.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = obj.transform;
                }
            }
        }

        if (closest == null)
        {
            foreach (string charName in targetCharacterNames)
            {
                GameObject obj = GameObject.Find(charName);
                if (obj != null && obj.activeInHierarchy)
                {
                    activePlayerTransform = obj.transform;
                    return;
                }
            }
        }

        activePlayerTransform = closest;
    }

    public void TriggerBridgePublic()
    {
        TriggerBridge();
    }

    void TriggerBridge()
    {
        isTriggered = true;

        foreach (var anim in childAnimators)
        {
            anim.enabled = true;
        }

        StartCoroutine(EnableCollidersDelayed());
    }

    IEnumerator EnableCollidersDelayed()
    {
        yield return new WaitForSeconds(5.792f); 
        
        foreach (var col in childColliders)
        {
            col.enabled = true;
        }
        foreach (var anim in childAnimators)
        {
            anim.enabled = false;
        }
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, activationDistance);
    }
}