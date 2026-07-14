using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Nemuri.Interactions;
using Nemuri.Player;

public class Chapt1gatecontroller : MonoBehaviour
{
    [Header("Movement Settings")]
    public float dropDistance = 1.34f;
    public float duration = 1.0f; 
    public float activationDistance = 3.0f; 

    [Header("Target Character Settings")]
    [SerializeField] private string[] targetCharacterNames = { "MURIALCHARA" };

    [Header("Hold Interaction")]
    public float holdDuration = 3f;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    public bool isTriggered = false; 
    private Transform activePlayerTransform;
    private float _holdTimer;
    private Interactable _interactable;
    private float _wrongPlayerTimer = 0f;

    void Start()
    {
        startPosition = transform.position;
        targetPosition = startPosition + (Vector3.down * dropDistance);
        FindActivePlayer();

        _interactable = GetComponent<Interactable>();
        if (_interactable != null)
        {
            _interactable.enabled = false;
        }
    }

    void Update()
    {
        if (isTriggered) return;

        // Wrong player warning feedback timer
        if (_wrongPlayerTimer > 0f)
        {
            _wrongPlayerTimer -= Time.deltaTime;
            if (_wrongPlayerTimer <= 0f)
            {
                _interactable?.DismissInteraction();
            }
            return;
        }

        // Find the active player every frame to ensure we are always referencing the correct character
        FindActivePlayer();

        if (activePlayerTransform == null)
        {
            HideInteraction();
            return; 
        }

        float distance = Vector3.Distance(transform.position, activePlayerTransform.position);
        
        if (distance <= activationDistance)
        {
            bool isAllowed = false;
            foreach (var n in targetCharacterNames)
            {
                if (activePlayerTransform.name.Contains(n))
                {
                    isAllowed = true;
                    break;
                }
            }

            if (isAllowed)
            {
                if (Keyboard.current != null && Keyboard.current.eKey.isPressed)
                {
                    _holdTimer += Time.deltaTime;
                    _interactable?.DisplayInteraction("Hold E to open gate", _holdTimer / holdDuration);

                    if (_holdTimer >= holdDuration)
                    {
                        _holdTimer = 0f;
                        _interactable?.DismissInteraction();
                        TriggerMovement();
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
                        _interactable?.DisplayInteraction("Hold E to open gate", 0f);
                    }
                }
            }
            else
            {
                _interactable?.DisplayInteraction("Hold E to open gate", 0f);

                if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                {
                    _interactable?.DisplayInteraction("Use Murial", 0f);
                    _wrongPlayerTimer = 2.0f; // Show error message for 2 seconds
                    _holdTimer = 0f;
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
        if (PlayerMovementChapt1.Instance != null && PlayerMovementChapt1.Instance.gameObject.activeInHierarchy)
        {
            activePlayerTransform = PlayerMovementChapt1.Instance.transform;
            Debug.Log($"[Chapt1GateController] Found active PlayerMovementChapt1: {activePlayerTransform.name}");
            return;
        }
        if (PlayerMovement.Instance != null && PlayerMovement.Instance.gameObject.activeInHierarchy)
        {
            activePlayerTransform = PlayerMovement.Instance.transform;
            Debug.Log($"[Chapt1GateController] Found active PlayerMovement: {activePlayerTransform.name}");
            return;
        }

        GameObject defaultPlayer = GameObject.FindWithTag("Player");
        if (defaultPlayer != null && defaultPlayer.activeInHierarchy)
        {
            activePlayerTransform = defaultPlayer.transform;
            Debug.Log($"[Chapt1GateController] Found active tag Player: {activePlayerTransform.name}");
        }
        else
        {
            activePlayerTransform = null;
            Debug.Log("[Chapt1GateController] No active player found!");
        }
    }

    void TriggerMovement()
    {
        StartCoroutine(MoveSmoothly());
    }

    IEnumerator MoveSmoothly()
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = Mathf.SmoothStep(0f, 1f, t); 
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }
        transform.position = targetPosition;
        
        // Mark as triggered ONLY after the movement is fully complete!
        isTriggered = true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activationDistance);
    }
}