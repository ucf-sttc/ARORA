using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class TerrainGroupControls : MonoBehaviour
{
#if UNITY_EDITOR
    public Material replacementMaterial;
    [ContextMenu("Update Terrain material")]
    void UpdateMaterials()
    {
        Terrain[] terrains = FindObjectsOfType<Terrain>();
        for(int i=0;i<terrains.Length;i++)
        {
            if (replacementMaterial != null)
                terrains[i].materialTemplate = replacementMaterial;
            else
                terrains[i].materialTemplate = new Material(Shader.Find("Custom/Terrain/Lit_SegmentationEnabled"));
            EditorUtility.SetDirty(terrains[i]);
        }
    }

    [ContextMenu("Enable Draw Instanced Rendering")]
    void SetDrawInstancedRendering()
    {
        Terrain[] terrains = FindObjectsOfType<Terrain>();
        for (int i = 0; i < terrains.Length; i++)
        {
            terrains[i].drawInstanced = false;
            EditorUtility.SetDirty(terrains[i]);
        }
    }

    public int baseMapDistance;
    [ContextMenu("Set Base Map Distance")]
    void SetBaseMapDistance()
    {
        Terrain[] terrains = FindObjectsOfType<Terrain>();
        for (int i = 0; i < terrains.Length; i++)
        {
            terrains[i].basemapDistance = baseMapDistance;
            EditorUtility.SetDirty(terrains[i]);
        }
    }

    public int detailPatchResolution, detailResolution;
    [ContextMenu("Set Detail Patch Size and Resolution")]
    void SetDetailPatchSizeAndResolution()
    {
        Terrain[] terrains = FindObjectsOfType<Terrain>();
        for (int i = 0; i < terrains.Length; i++)
        {
            terrains[i].terrainData.SetDetailResolution(detailResolution, detailPatchResolution);
            EditorUtility.SetDirty(terrains[i]);
        }
    }

    public bool preserveTreePrefabLayers;
    [ContextMenu("Set Tree Prefab Layer Preservation on Terrains")]
    void preserveTreePrefabLayersOnTerrain()
    {
        foreach (Terrain t in Terrain.activeTerrains)
        {
            if (true)
            {
                t.preserveTreePrototypeLayers = preserveTreePrefabLayers;
                EditorUtility.SetDirty(t);
            }
        }
    }

    [ContextMenu("Clear tree instances")]
    void ClearTrees()
    {
        Terrain[] terrains = FindObjectsOfType<Terrain>();
        for (int i = 0; i < terrains.Length; i++)
        {
            terrains[i].terrainData.treeInstances = new TreeInstance[] { };
            terrains[i].terrainData.treePrototypes = new TreePrototype[] { };
            terrains[i].terrainData.RefreshPrototypes();
            EditorUtility.SetDirty(terrains[i]);
        }
    }

    [ContextMenu("Clear unused tree prefabs")]
    void ClearUnusedTreePrefabs()
    {
        Terrain[] terrains = FindObjectsOfType<Terrain>();
        int i, j, prototypeIndex;
        for (i = 0; i < terrains.Length; i++)
        {
            List<TreePrototype> usedPrototypes = new List<TreePrototype>();
            List<int> usedTreePrototypeIndexes = new List<int>();
            TreeInstance[] newTreeInstances = terrains[i].terrainData.treeInstances;

            for (j=0;j<terrains[i].terrainData.treeInstanceCount;j++)
            { 
                prototypeIndex = usedTreePrototypeIndexes.IndexOf(terrains[i].terrainData.treeInstances[j].prototypeIndex);
                if (prototypeIndex == -1)
                {
                    usedTreePrototypeIndexes.Add(terrains[i].terrainData.treeInstances[j].prototypeIndex);
                    usedPrototypes.Add(terrains[i].terrainData.treePrototypes[terrains[i].terrainData.treeInstances[j].prototypeIndex]);
                    prototypeIndex = usedPrototypes.Count - 1;
                }

                newTreeInstances[j].prototypeIndex = prototypeIndex;
            }

            terrains[i].terrainData.treeInstances = newTreeInstances;
               
            terrains[i].terrainData.treePrototypes = usedPrototypes.ToArray();
            
            terrains[i].terrainData.RefreshPrototypes();
            EditorUtility.SetDirty(terrains[i]);
        }
    }
#endif
}
