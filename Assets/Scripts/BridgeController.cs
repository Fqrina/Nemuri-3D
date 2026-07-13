using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class BridgeController : MonoBehaviour
{
    [Header("Detection Settings")]
    public float activationDistance = 3.0f;
    [SerializeField] private string[] targetCharacterNames = { "KEIKOCHARA" };

    private Animator[] childAnimators;
    private BoxCollider[] childColliders;
    private bool isTriggered = false;
    private Transform activePlayerTransform;
    private float searchTimer = 0f;

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
                TriggerBridge();
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