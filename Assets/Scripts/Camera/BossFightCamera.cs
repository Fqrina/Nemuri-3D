using UnityEngine;

public class BossFightCamera : MonoBehaviour
{
    [Header("Targets")]
    public Transform player;
    public Transform boss;
    private Transform bossTargetPoint; // Specific child like Plane.001

    [Header("Positioning Settings")]
    public float followDistance = 20f;   // Distance behind the player
    public float cameraHeight = 20f;    // Height above the player
    public float rightOffset = 1.0f;     // Slight over-the-shoulder offset (common in GoW)
    public float positionSmoothSpeed = 8.0f;
    public float rotationSmoothSpeed = 10.0f;
    public Vector3 shakeOffset = Vector3.zero;

    [Header("LookAt Settings")]
    public Vector3 lookAtOffset = new Vector3(0f, 1.0f, 0f);

    [Header("Dynamic LookAt Height (GoW Proximity)")]
    public bool enableDynamicHeightLookAt = true;
    public float closeLookAtHeight = 3.5f;       // Y offset when player is close to boss
    public float farLookAtHeight = 1.0f;         // Y offset when player is far from boss
    public float heightBlendMinDistance = 5.0f;  // Blends close height at or below this distance
    public float heightBlendMaxDistance = 20.0f; // Blends far height at or above this distance

    [Header("Collision Settings")]
    public bool enableCollision = true;
    public LayerMask collisionLayers = ~0;
    public float cameraSphereRadius = 0.4f;
    public float wallSafetyDistance = 0.3f;
    public float minDistanceToPlayer = 2.0f;
    public float playerCollisionHeightOffset = 1.5f;

    private Cinemachine.CinemachineVirtualCamera vCam;

    private void Start()
    {
        vCam = GetComponent<Cinemachine.CinemachineVirtualCamera>();
        
        // Sanitize lookAtOffset if the user set it as an angle (e.g. 90 degrees) instead of position offset
        if (Mathf.Abs(lookAtOffset.x) > 10f || Mathf.Abs(lookAtOffset.z) > 10f)
        {
            lookAtOffset = new Vector3(0f, 1.5f, 0f);
        }

        FindTargets();
        UpdateCamera(true);
    }

    private void LateUpdate()
    {
        FindTargets();
        UpdateCamera(false);
    }

    private void FindTargets()
    {
        // Auto-correct if player target is null or mistakenly set to boss parts (like Plane.001)
        if (player == null || IsBossOrChild(player))
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            else
            {
                playerObj = FindSceneObjectContaining("Player");
                if (playerObj == null) playerObj = FindSceneObjectContaining("kael");
                if (playerObj != null) player = playerObj.transform;
            }
        }

        if (boss == null)
        {
            // 1. Try finding HoverAnimationController in loaded scene (active or inactive)
            HoverAnimationController hover = FindFirstObjectByTypeAll<HoverAnimationController>();
            if (hover != null)
            {
                boss = hover.transform;
            }
            else
            {
                // 2. Try finding EVILRABBIT (active or inactive)
                GameObject bossObj = FindSceneObjectByName("EVILRABBIT");
                if (bossObj == null) bossObj = FindSceneObjectByName("EVILRABBIT(Clone)");
                if (bossObj == null) bossObj = FindSceneObjectContaining("EVILRABBIT");
                if (bossObj != null) boss = bossObj.transform;
            }

            // Dynamically attach BossFightTester to the boss so they can immediately test the health bar
            if (boss != null)
            {
                var tester = boss.GetComponent<BossFightTester>();
                if (tester == null)
                {
                    tester = boss.gameObject.AddComponent<BossFightTester>();
                    tester.bossName = "EVIL RABBIT";
                    tester.maxHealth = 100f;
                    tester.triggerDistance = 45f;
                }
            }
        }

        // Find specific target child (CamTarget) for precise locking
        if (boss != null && bossTargetPoint == null)
        {
            bossTargetPoint = FindChildRecursive(boss, "CamTarget");
            if (bossTargetPoint == null)
            {
                bossTargetPoint = FindChildRecursive(boss, "Plane.001");
            }
            if (bossTargetPoint == null)
            {
                bossTargetPoint = FindChildContaining(boss, "plane");
            }
            if (bossTargetPoint == null)
            {
                bossTargetPoint = boss; // Fallback to main transform
            }
        }
    }

    private bool IsBossOrChild(Transform t)
    {
        if (t == null) return false;
        string nameLower = t.name.ToLower();
        if (nameLower.Contains("plane.001") || nameLower.Contains("evilrabbit") || nameLower.Contains("cube"))
        {
            return true;
        }
        return false;
    }

    private GameObject FindSceneObjectByName(string name)
    {
        GameObject[] objs = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject obj in objs)
        {
            if (obj.name == name && obj.scene.isLoaded)
            {
                return obj;
            }
        }
        return null;
    }

    private GameObject FindSceneObjectContaining(string substring)
    {
        GameObject[] objs = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject obj in objs)
        {
            if (obj.name.ToLower().Contains(substring.ToLower()) && obj.scene.isLoaded)
            {
                return obj;
            }
        }
        return null;
    }

    private T FindFirstObjectByTypeAll<T>() where T : Component
    {
        T[] objs = Resources.FindObjectsOfTypeAll<T>();
        foreach (T obj in objs)
        {
            if (obj.gameObject.scene.isLoaded)
            {
                return obj;
            }
        }
        return null;
    }

    private Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent.name == childName) return parent;
        foreach (Transform child in parent)
        {
            Transform found = FindChildRecursive(child, childName);
            if (found != null) return found;
        }
        return null;
    }

    private Transform FindChildContaining(Transform parent, string substring)
    {
        if (parent.name.ToLower().Contains(substring.ToLower())) return parent;
        foreach (Transform child in parent)
        {
            Transform found = FindChildContaining(child, substring);
            if (found != null) return found;
        }
        return null;
    }

    private void UpdateCamera(bool immediate)
    {
        if (vCam != null)
        {
            vCam.Follow = null;
            vCam.LookAt = null;
        }

        if (player == null || boss == null) return;

        Transform lockTarget = bossTargetPoint != null ? bossTargetPoint : boss;

        // Calculate direction from player to the boss target
        Vector3 toBoss = lockTarget.position - player.position;
        Vector3 horizontalDir = new Vector3(toBoss.x, 0f, toBoss.z).normalized;

        if (horizontalDir.sqrMagnitude < 0.001f)
        {
            horizontalDir = player.forward; // Fallback if perfectly on top
        }

        // God of War Over-the-shoulder positioning:
        // Position is behind the player (opposite of boss direction)
        // Plus an optional right offset to frame the player slightly to the side (over-the-shoulder)
        Vector3 cameraRight = Vector3.Cross(Vector3.up, horizontalDir).normalized;
        Vector3 desiredPosition = player.position - (horizontalDir * followDistance) + (Vector3.up * cameraHeight) + (cameraRight * rightOffset);

        // Handle Camera Collision from player's back to desired position
        if (enableCollision)
        {
            Vector3 rayStart = player.position + Vector3.up * playerCollisionHeightOffset; // Cast from player's upper chest height
            Vector3 rayDir = desiredPosition - rayStart;
            float rayDistance = rayDir.magnitude;
            rayDir.Normalize();

            if (Physics.SphereCast(rayStart, cameraSphereRadius, rayDir, out RaycastHit hit, rayDistance, collisionLayers))
            {
                float adjustedDistance = Mathf.Max(minDistanceToPlayer, hit.distance - wallSafetyDistance);
                desiredPosition = rayStart + rayDir * adjustedDistance;
            }
        }

        // Smooth position movement
        if (immediate)
        {
            transform.position = desiredPosition + shakeOffset;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, desiredPosition, positionSmoothSpeed * Time.deltaTime) + shakeOffset;
        }

        // Look target on the boss (locked on Plane.001 / CamTarget)
        Vector3 finalLookAtOffset = lookAtOffset;
        if (enableDynamicHeightLookAt)
        {
            float distanceToBoss = Vector3.Distance(player.position, lockTarget.position);
            float t = Mathf.InverseLerp(heightBlendMinDistance, heightBlendMaxDistance, distanceToBoss);
            // Blends between closeLookAtHeight (closer to face) and farLookAtHeight (closer to body)
            finalLookAtOffset.y = Mathf.Lerp(closeLookAtHeight, farLookAtHeight, t);
        }

        Vector3 lookTarget = lockTarget.position + finalLookAtOffset;
        Vector3 lookDirection = lookTarget - transform.position;

        if (lookDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            if (immediate)
            {
                transform.rotation = targetRotation;
            }
            else
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothSpeed * Time.deltaTime);
            }
        }
    }
}
