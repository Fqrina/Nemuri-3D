using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Chapt1gatecontroller : MonoBehaviour
{
    [Header("Movement Settings")]
    public float dropDistance = 1.34f;
    public float duration = 1.0f; 
    public float activationDistance = 3.0f; 

    [Header("Target Character Settings")]
    [SerializeField] private string[] targetCharacterNames = { "KEIKOCHARA", "Player2" };

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private bool isTriggered = false; 
    private Transform activePlayerTransform;
    private float searchTimer = 0f;

    void Start()
    {
        startPosition = transform.position;
        targetPosition = startPosition + (Vector3.down * dropDistance);
        FindActivePlayer();
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
            return; 
        }

        float distance = Vector3.Distance(transform.position, activePlayerTransform.position);
        
        if (distance <= activationDistance)
        {
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                TriggerMovement();
            }
        }
    }

    void FindActivePlayer()
    {
        foreach (string charName in targetCharacterNames)
        {
            GameObject obj = GameObject.Find(charName);
            if (obj != null && obj.activeInHierarchy)
            {
                activePlayerTransform = obj.transform;
                break;
            }
        }
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