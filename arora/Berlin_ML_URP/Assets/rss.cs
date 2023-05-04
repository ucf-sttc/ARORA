using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class rss : MonoBehaviour
{

    //public static regionSelectionScriptEditor rsse;
    public Transform prefabToHold;
    public List<Texture2D> tileTextures = new List<Texture2D>();
    public List<TerrainLayer> terrainLayers = new List<TerrainLayer>();
    public List<TerrainData> terrainDatas = new List<TerrainData>();
    public bool newData;
    public bool[,] selectedData;
    public int pixelError=20;
    public List<GameObject> terrainPrefabs = new List<GameObject>();
    // Start is called before the first frame update
    void Start()
    {

        GameObject tp = GameObject.Find("exported");
        if(tp!=null)
        for(int i = 0; i < tp.transform.childCount; i++)
        {
                if (tp.transform.GetChild(i).gameObject.name == "rss")
                    continue;
                terrainPrefabs.Add(tp.transform.GetChild(i).gameObject);
        }
        tp = GameObject.Find("exported2");
        if(tp!=null)
        for (int i = 0; i < tp.transform.childCount; i++)
        {
                if (tp.transform.GetChild(i).gameObject.name == "rss")
                    continue;
                terrainPrefabs.Add(tp.transform.GetChild(i).gameObject);
        }
        tp = GameObject.Find("exported3");
        if (tp != null)
        for (int i = 0; i < tp.transform.childCount; i++)
        {
                if (tp.transform.GetChild(i).gameObject.name == "rss")
                    continue;
                terrainPrefabs.Add(tp.transform.GetChild(i).gameObject);
        }
        tp = GameObject.Find("exported4");
        if (tp != null)
        for (int i = 0; i < tp.transform.childCount; i++)
        {
                if (tp.transform.GetChild(i).gameObject.name == "rss")
                    continue;
                terrainPrefabs.Add(tp.transform.GetChild(i).gameObject);
        }

        if (GameObject.Find("pixelerror") != null)
        {
            GameObject.Find("pixelerror").GetComponent<Text>().text = "Press 'O' to adjust Pixel Error";
        }
    }



    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            pixelError = pixelError > 1 ? pixelError - 1 : pixelError;
            Debug.Log("Pixel Error: " + pixelError);
            for (int i = 0; i < terrainPrefabs.Count; i++)
            {
                terrainPrefabs[i].GetComponent<Terrain>().heightmapPixelError = pixelError;
            }
            if (GameObject.Find("pixelerror") != null)
            {
                GameObject.Find("pixelerror").GetComponent<Text>().text = "Pixel Error: " + pixelError;
            }
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            pixelError = pixelError < 201 ? pixelError + 1 : pixelError;
            Debug.Log("Pixel Error: " + pixelError);
            for (int i = 0; i < terrainPrefabs.Count; i++)
            {
                terrainPrefabs[i].GetComponent<Terrain>().heightmapPixelError = pixelError;
            }
            if (GameObject.Find("pixelerror") != null)
            {
                GameObject.Find("pixelerror").GetComponent<Text>().text = "Pixel Error: " + pixelError;
            }

        }
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
        for (int i = 0; i < root; i++)
        {
            for (int j = 0; j < root; j++)
            {
                transform.GetChild((i * root) + j).gameObject.SetActive(!selectedData[i, j]);
            }
        }

    }
}
