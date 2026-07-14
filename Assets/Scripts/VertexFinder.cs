using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class VertexFinder : EditorWindow
{
    [MenuItem("Tools/Find Highest Vertex Objects")]
    public static void ShowWindow()
    {
        GetWindow<VertexFinder>("Vertex Finder");
    }

    void OnGUI()
    {
        if (GUILayout.Button("Scan Scene Vertices"))
        {
            ScanScene();
        }
    }

    private void ScanScene()
    {
        int totalStaticVertices = 0;
        int totalSkinnedVertices = 0;
        int totalTerrainVertices = 0;

        // Group by GameObject name to see accumulated totals (e.g. grass prefabs)
        Dictionary<string, (int singleMax, int accumulated, int count)> objectStats = new Dictionary<string, (int, int, int)>();

        // 1. Scan MeshFilters (Static/Dynamic meshes)
        MeshFilter[] meshFilters = FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var filter in meshFilters)
        {
            if (filter.sharedMesh != null)
            {
                string objName = filter.gameObject.name;
                int verts = filter.sharedMesh.vertexCount;
                totalStaticVertices += verts;

                if (objectStats.ContainsKey(objName))
                {
                    var stat = objectStats[objName];
                    objectStats[objName] = (Mathf.Max(stat.singleMax, verts), stat.accumulated + verts, stat.count + 1);
                }
                else
                {
                    objectStats[objName] = (verts, verts, 1);
                }
            }
        }

        // 2. Scan SkinnedMeshRenderers (Animated meshes)
        SkinnedMeshRenderer[] skinnedRenderers = FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var smr in skinnedRenderers)
        {
            if (smr.sharedMesh != null)
            {
                string objName = smr.gameObject.name;
                int verts = smr.sharedMesh.vertexCount;
                totalSkinnedVertices += verts;

                if (objectStats.ContainsKey(objName))
                {
                    var stat = objectStats[objName];
                    objectStats[objName] = (Mathf.Max(stat.singleMax, verts), stat.accumulated + verts, stat.count + 1);
                }
                else
                {
                    objectStats[objName] = (verts, verts, 1);
                }
            }
        }

        // 3. Scan Terrains
        Terrain[] terrains = FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var terrain in terrains)
        {
            if (terrain.terrainData != null)
            {
                string objName = terrain.gameObject.name;
                // Calculate rough vertex count for terrain based on heightmap resolution
                int width = terrain.terrainData.heightmapResolution;
                int height = terrain.terrainData.heightmapResolution;
                int verts = width * height;
                totalTerrainVertices += verts;

                if (objectStats.ContainsKey(objName))
                {
                    var stat = objectStats[objName];
                    objectStats[objName] = (Mathf.Max(stat.singleMax, verts), stat.accumulated + verts, stat.count + 1);
                }
                else
                {
                    objectStats[objName] = (verts, verts, 1);
                }
            }
        }

        int grandTotal = totalStaticVertices + totalSkinnedVertices + totalTerrainVertices;

        Debug.Log("==================================================");
        Debug.Log($"--- SCENE VERTEX OVERVIEW ---");
        Debug.Log($"Total Scene Vertices (approx): {grandTotal:N0}");
        Debug.Log($"- MeshFilter Vertices: {totalStaticVertices:N0}");
        Debug.Log($"- SkinnedMeshRenderer Vertices: {totalSkinnedVertices:N0}");
        Debug.Log($"- Terrain Vertices: {totalTerrainVertices:N0}");
        Debug.Log("==================================================");

        // Sort by accumulated vertex count in descending order
        var sortedByAccumulated = objectStats.OrderByDescending(pair => pair.Value.accumulated).ToList();

        Debug.Log("--- Top 15 Heaviest GameObjects by ACCUMULATED Vertex Count ---");
        for (int i = 0; i < Mathf.Min(15, sortedByAccumulated.Count); i++)
        {
            var pair = sortedByAccumulated[i];
            Debug.Log($"{i + 1}. {pair.Key}: {pair.Value.accumulated:N0} total vertices ({pair.Value.count} instances, max single: {pair.Value.singleMax:N0} verts)");
        }
        Debug.Log("==================================================");
    }
}
