using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class terrainManipulationScript : MonoBehaviour
{
    public GameObject terrainParent;
    public float terrainTileSize;
    public float basemapDist;
    public int pixelError;
    public int groupingID;
    public Material terrainMaterial;
    public UnityEngine.Rendering.ReflectionProbeUsage reflectionProbe;
    public bool lightProbeRinging, preserveTreePrototypeLayers;
    public int detailDistance;
    public float detailDensity;
    public int treeDistance, billboardStart, fadeLength, maxMeshTrees;
    public float windSpeed, windSize, windBending;
    public Color grassTint;
    public int detailResolutionPerPatch, detailResolution, baseTextureResolution, controlTextureResolution;
    public bool lightmapStatic;
    public List<Texture2D> tt;
    public List<TerrainData> td;
    [HideInInspector]
    public bool texturesOn = true;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
