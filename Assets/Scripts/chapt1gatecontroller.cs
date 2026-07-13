using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Nemuri.Interactions;

public class Chapt1gatecontroller : MonoBehaviour
{
    [Header("Movement Settings")]
    public float dropDistance = 1.34f;
    public float duration = 1.0f; 
    public float activationDistance = 3.0f; 

    [Header("Target Character Settings")]
    [SerializeField] private string[] targetCharacterNames = { "KEIKOCHARA", "Player2" };

    [Header("Hold Interaction")]
    public float holdDuration = 3f;

    [SerializeField] private string[] allCharacterNames = { "KEIKOCHARA", "Player2" };

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private bool isTriggered = false; 
    private Transform activePlayerTransform;
    private float searchTimer = 0f;
    private float _holdTimer;
    private Interactable _interactable;
    private bool _hasShownWrongPlayerError;

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
            bool isAllowed = System.Array.Exists(
                targetCharacterNames, n => activePlayerTransform.name == n);

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
                if (!_hasShownWrongPlayerError)
                {
                    _interactable?.DisplayInteraction("Hold E to open gate", 0f);

                    if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                    {
                        string names = string.Join(" or ", targetCharacterNames);
                        _interactable?.DisplayInteraction($"You must use {names} as player to interact", 0f);
                        _hasShownWrongPlayerError = true;
                        isTriggered = true;
                        _interactable?.DismissInteraction();
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

    void TriggerMovement()
    {
        isTriggered = true; 
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
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activationDistance);
    }
}