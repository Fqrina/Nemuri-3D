using UnityEngine;
using UnityEditor;

public class GrassScript : EditorWindow
{
    private GameObject grassPrefab;
    private GameObject targetPlane;
    private float brushRadius = 3f;
    private int grassDensity = 2;
    private bool isPainting = false;

    [MenuItem("Window/Custom Tools/Grass Painter")]
    public static void ShowWindow() => GetWindow<GrassScript>("Grass Painter");

    private void OnGUI()
    {
        grassPrefab = (GameObject)EditorGUILayout.ObjectField("grass prefab", grassPrefab, typeof(GameObject), false);
        targetPlane = (GameObject)EditorGUILayout.ObjectField("target plane obj", targetPlane, typeof(GameObject), true);
        brushRadius = EditorGUILayout.Slider("brush radius", brushRadius, 1f, 50f);
        grassDensity = EditorGUILayout.IntSlider("density per click", grassDensity, 1, 20);

        GUILayout.Space(10);
        GUI.backgroundColor = isPainting ? Color.green : Color.red;
        if (GUILayout.Button(isPainting ? "on" : "off", GUILayout.Height(40)))
        {
            isPainting = !isPainting;
            if (isPainting) SceneView.duringSceneGui += OnSceneGUI;
            else SceneView.duringSceneGui -= OnSceneGUI;
        }
    }

    private void OnDestroy() => SceneView.duringSceneGui -= OnSceneGUI;

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!isPainting || targetPlane == null) return;

        Event e = Event.current;
        bool isHoldingShift = e.shift;

        if ((e.type == EventType.MouseDown && e.button == 0) || (e.type == EventType.MouseDrag && e.button == 0))
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            
            Collider targetCollider = targetPlane.GetComponent<Collider>();
            if (targetCollider == null) return;

            if (targetCollider.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
            {
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

                if (isHoldingShift)
                {
                    // menghapus objek anak berdasarkan jarak matematika (bebas lag)
                    for (int i = targetPlane.transform.childCount - 1; i >= 0; i--)
                    {
                        Transform child = targetPlane.transform.GetChild(i);
                        if (Vector3.Distance(hit.point, child.position) <= brushRadius)
                        {
                            Undo.DestroyObjectImmediate(child.gameObject);
                        }
                    }
                }
                else
                {
                    if (grassPrefab == null) return;

                    for (int i = 0; i < grassDensity; i++)
                    {
                        Vector2 randomCircle = Random.insideUnitCircle * brushRadius;
                        Vector3 spawnPos = hit.point + new Vector3(randomCircle.x, 0, randomCircle.y);
                        
                        Ray downRay = new Ray(spawnPos + Vector3.up * 50f, Vector3.down);

                        if (targetCollider.Raycast(downRay, out RaycastHit groundHit, Mathf.Infinity))
                        {
                            GameObject grass = (GameObject)PrefabUtility.InstantiatePrefab(grassPrefab);
                            grass.transform.position = groundHit.point;
                            grass.transform.rotation = Quaternion.FromToRotation(Vector3.up, groundHit.normal) * Quaternion.Euler(0, Random.Range(0, 360), 0);
                            grass.transform.parent = targetPlane.transform;
                            
                            // paksa hilangkan collider bawaan jika terlanjur ada di dalam prefab rumput
                            Collider grassCollider = grass.GetComponent<Collider>();
                            if (grassCollider != null)
                            {
                                DestroyImmediate(grassCollider);
                            }
                            
                            Undo.RegisterCreatedObjectUndo(grass, "paint grass");
                        }
                    }
                }
                e.Use();
            }
        }
    }
}
