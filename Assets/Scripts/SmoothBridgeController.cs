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
        _isBridge1Active = true;
        StartCoroutine(TransitionBridgeRoutine(bridge1, bridge1Target));
    }

    public void TriggerBridge2()
    {
        if (_isBridge2Active) return;
        _isBridge2Active = true;
        StartCoroutine(TransitionBridgeRoutine(bridge2, bridge2Target));
    }

    public void TriggerBridge3()
    {
        if (_isBridge3Active) return;
        _isBridge3Active = true;
        StartCoroutine(TransitionBridgeRoutine(bridge3, bridge3Target));
    }

    public void TriggerBridge(int index)
    {
        if (index == 1) TriggerBridge1();
        else if (index == 2) TriggerBridge2();
        else if (index == 3) TriggerBridge3();
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
        List<RendererMatInfo> matInfos = new List<RendererMatInfo>();
        Renderer[] renderers = bridge.GetComponentsInChildren<Renderer>(true);

        foreach (var r in renderers)
        {
            if (r == null) continue;
            foreach (var mat in r.materials)
            {
                if (mat == null) continue;

                Color origColor = mat.HasProperty("_Color") ? mat.color : Color.white;

                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", 0f);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

                if (mat.HasProperty("_Color"))
                {
                    Color c = origColor;
                    c.a = 0f;
                    mat.color = c;
                }

                matInfos.Add(new RendererMatInfo
                {
                    renderer = r,
                    material = mat,
                    originalColor = origColor
                });
            }
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float tSmooth = Mathf.SmoothStep(0f, 1f, t);

            bridge.transform.position = Vector3.Lerp(startPos, targetPos, tSmooth);

            foreach (var info in matInfos)
            {
                if (info.material != null && info.material.HasProperty("_Color"))
                {
                    Color c = info.originalColor;
                    c.a = Mathf.Lerp(0f, 1f, tSmooth);
                    info.material.color = c;
                }
            }

            yield return null;
        }

        bridge.transform.position = targetPos;

        foreach (var info in matInfos)
        {
            if (info.material != null)
            {
                if (info.material.HasProperty("_Color"))
                {
                    Color c = info.originalColor;
                    c.a = 1f;
                    info.material.color = c;
                }

                info.material.SetFloat("_Surface", 0f);
                info.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                info.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                info.material.SetInt("_ZWrite", 1);
                info.material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                info.material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
            }
        }

        foreach (var col in colliders)
        {
            if (col != null)
            {
                col.enabled = true;
            }
        }
    }
}
