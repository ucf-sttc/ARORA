using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


public class regionSelectionScript : MonoBehaviour
{
    //public static regionSelectionScriptEditor rsse;
    public List<Texture2D> tileTextures = new List<Texture2D>();
    public List<TerrainLayer> terrainLayers = new List<TerrainLayer>();
    public List<TerrainData> terrainDatas = new List<TerrainData>();
    public bool newData;
    public bool[,] selectedData;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// Makes the selected region visible, with (0,0) being the lower left. xLength and yLength are how many tiles out to the right and up the region will be made visible
    /// </summary>
    public void selectRegion(int xStart, int yStart, int xLength, int yLength)
    {

    }

    public void updateTerrainSelection()
    {
        int root = (int)Mathf.Sqrt(tileTextures.Count);
        for (int i = 0; i <root ; i++)
        {
            for(int j = 0; j < root; j++)
            {
                transform.GetChild((i * root) + j).gameObject.SetActive(!selectedData[i, j]);
            }
    }

    }
}
