using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(FreelookCamera))]
public class CinematicPathManager : MonoBehaviour
{
    [System.Serializable]
    public struct CameraKeyframe
    {
        public Vector3 position;
        public Quaternion rotation;

        public CameraKeyframe(Vector3 pos, Quaternion rot)
        {
            position = pos;
            rotation = rot;
        }
    }

    [Header("Cinematic Path Settings")]
    [Tooltip("List of recorded keyframes.")]
    public List<CameraKeyframe> keyframes = new List<CameraKeyframe>();

    [Tooltip("Total duration of the playback in seconds.")]
    public float playbackDuration = 10f;

    [Tooltip("If true, eases in at the start and eases out at the end of the path.")]
    public bool easeInEaseOut = true;

    [Header("Controls")]
    [Tooltip("Key to add a keyframe at current position/rotation.")]
    public Key addKeyframeKey = Key.V;

    [Tooltip("Key to start/stop path playback.")]
    public Key playPlaybackKey = Key.P;

    private FreelookCamera freeLookCam;
    private bool isPlaying = false;
    private float playbackTime = 0f;

    void Awake()
    {
        freeLookCam = GetComponent<FreelookCamera>();
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard[addKeyframeKey].wasPressedThisFrame && !isPlaying)
        {
            AddKeyframe();
        }

        if (keyboard[playPlaybackKey].wasPressedThisFrame)
        {
            if (isPlaying)
            {
                StopPlayback();
            }
            else if (keyframes.Count >= 2)
            {
                StartPlayback();
            }
            else
            {
                Debug.LogWarning("CinematicPathManager: Need at least 2 keyframes to play path.");
            }
        }

        if (isPlaying)
        {
            UpdatePlayback();
        }
    }

    public void AddKeyframe()
    {
        CameraKeyframe frame = new CameraKeyframe(transform.position, transform.rotation);
        keyframes.Add(frame);
        Debug.Log($"CinematicPathManager: Added keyframe {keyframes.Count} at pos: {frame.position}, rot: {frame.rotation.eulerAngles}");
    }

    public void StartPlayback()
    {
        if (keyframes.Count < 2) return;
        isPlaying = true;
        playbackTime = 0f;
        if (freeLookCam != null)
        {
            freeLookCam.isPlaying = true;
        }
        Debug.Log("CinematicPathManager: Starting cinematic playback.");
    }

    public void StopPlayback()
    {
        isPlaying = false;
        if (freeLookCam != null)
        {
            freeLookCam.isPlaying = false;
        }
        Debug.Log("CinematicPathManager: Stopped cinematic playback.");
    }

    private void UpdatePlayback()
    {
        playbackTime += Time.deltaTime;
        float rawNormalizedTime = Mathf.Clamp01(playbackTime / playbackDuration);

        // Apply ease-in, ease-out (smooth acceleration and deceleration) using smoothstep formula
        float t = easeInEaseOut ? SmoothStep(rawNormalizedTime) : rawNormalizedTime;

        EvaluatePath(t, out Vector3 pos, out Quaternion rot);

        transform.position = pos;
        transform.rotation = rot;

        if (rawNormalizedTime >= 1f)
        {
            StopPlayback();
        }
    }

    private float SmoothStep(float x)
    {
        return x * x * (3f - 2f * x);
    }

    private void EvaluatePath(float t, out Vector3 position, out Quaternion rotation)
    {
        int numSections = keyframes.Count - 1;
        float scaledT = t * numSections;
        int index = Mathf.Min(Mathf.FloorToInt(scaledT), numSections - 1);
        float localT = scaledT - index;

        // Position evaluation: Catmull-Rom spline interpolation for smooth curves
        CameraKeyframe p0 = keyframes[Mathf.Max(index - 1, 0)];
        CameraKeyframe p1 = keyframes[index];
        CameraKeyframe p2 = keyframes[Mathf.Min(index + 1, numSections)];
        CameraKeyframe p3 = keyframes[Mathf.Min(index + 2, numSections)];

        position = GetCatmullRomPosition(localT, p0.position, p1.position, p2.position, p3.position);

        // Rotation evaluation: Spherical linear interpolation (Slerp) with smooth Hermite interpolation
        // to avoid jerky speed changes at keyframes
        float smoothT = localT * localT * (3f - 2f * localT);
        rotation = Quaternion.Slerp(p1.rotation, p2.rotation, smoothT);
    }

    private Vector3 GetCatmullRomPosition(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        Vector3 res = 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );

        return res;
    }
}
