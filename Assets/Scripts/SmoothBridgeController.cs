using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmoothBridgeController : MonoBehaviour
{
    [Header("Bridge GameObjects")]
    [SerializeField] private GameObject bridge1;
    [SerializeField] private GameObject bridge2;
    [SerializeField] private GameObject bridge3;

    [Header("Target Positions")]
    [SerializeField] private Vector3 bridge1Target = new Vector3(64.5999985f, -102.82f, 68f);
    [SerializeField] private Vector3 bridge2Target = new Vector3(76.7099991f, -100.629997f, 100.910004f);
    [SerializeField] private Vector3 bridge3Target = new Vector3(90.1399994f, -100.629997f, 78.2600021f);

    [Header("Transition Settings")]
    [SerializeField] private float duration = 3.0f;
    [SerializeField] private bool hideOnStart = true;

    private bool _isBridge1Active;
    private bool _isBridge2Active;
    private bool _isBridge3Active;

    private class RendererMatInfo
    {
        public Renderer renderer;
        public Material material;
        public Color originalColor;
    }

    private void Start()
    {
        if (hideOnStart)
        {
            InitializeBridgeState(bridge1);
            InitializeBridgeState(bridge2);
            InitializeBridgeState(bridge3);
        }
    }

    private void InitializeBridgeState(GameObject bridge)
    {
        if (bridge == null) return;

        bridge.SetActive(false);

        Collider[] colliders = bridge.GetComponentsInChildren<Collider>(true);
        foreach (var col in colliders)
        {
            if (col != null)
            {
                col.enabled = false;
            }
        }
    }

    public void TriggerBridge1()
    {
        if (_isBridge1Active) return;
        Debug.Log("[SmoothBridgeController] Triggering Bridge 1 transition...");
        if (bridge1 == null)
        {
            Debug.LogError("[SmoothBridgeController] Cannot transition Bridge 1: 'bridge1' GameObject is NULL / unassigned in the Inspector!");
            return;
        }
        _isBridge1Active = true;
        StartCoroutine(TransitionBridgeRoutine(bridge1, bridge1Target));
    }

    public void TriggerBridge2()
    {
        if (_isBridge2Active) return;
        Debug.Log("[SmoothBridgeController] Triggering Bridge 2 transition...");
        if (bridge2 == null)
        {
            Debug.LogError("[SmoothBridgeController] Cannot transition Bridge 2: 'bridge2' GameObject is NULL / unassigned in the Inspector!");
            return;
        }
        _isBridge2Active = true;
        StartCoroutine(TransitionBridgeRoutine(bridge2, bridge2Target));
    }

    public void TriggerBridge3()
    {
        if (_isBridge3Active) return;
        Debug.Log("[SmoothBridgeController] Triggering Bridge 3 transition...");
        if (bridge3 == null)
        {
            Debug.LogError("[SmoothBridgeController] Cannot transition Bridge 3: 'bridge3' GameObject is NULL / unassigned in the Inspector!");
            return;
        }
        _isBridge3Active = true;
        StartCoroutine(TransitionBridgeRoutine(bridge3, bridge3Target));
    }

    public void TriggerBridge(int index)
    {
        if (index == 1) TriggerBridge1();
        else if (index == 2) TriggerBridge2();
        else if (index == 3) TriggerBridge3();
    }

    private string GetColorPropertyName(Material mat)
    {
        if (mat.HasProperty("_BaseColor")) return "_BaseColor";
        if (mat.HasProperty("_Color")) return "_Color";
        return null;
    }

    private IEnumerator TransitionBridgeRoutine(GameObject bridge, Vector3 targetPos)
    {
        if (bridge == null) yield break;

        bridge.SetActive(true);

        Collider[] colliders = bridge.GetComponentsInChildren<Collider>(true);
        foreach (var col in colliders)
        {
            if (col != null)
            {
                col.enabled = false;
            }
        }

        Vector3 startPos = bridge.transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float tSmooth = Mathf.SmoothStep(0f, 1f, t);

            bridge.transform.position = Vector3.Lerp(startPos, targetPos, tSmooth);
            yield return null;
        }

        bridge.transform.position = targetPos;

        foreach (var col in colliders)
        {
            if (col != null)
            {
                col.enabled = true;
            }
        }
    }
}
