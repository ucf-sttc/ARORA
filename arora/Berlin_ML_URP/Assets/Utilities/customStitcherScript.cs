using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class customStitcherScript : MonoBehaviour
{
    public int tIndices;
    public List<GameObject> terrainPrefabs = new List<GameObject>();
    public int terrainSize;
    public int x, y;
    // Start is called before the first frame update
    void Start()
    {
        int hr = terrainPrefabs[0].GetComponent<Terrain>().terrainData.heightmapResolution;
        float[,] gh = terrainPrefabs[0].GetComponent<Terrain>().terrainData.GetHeights(0, 0, hr, hr);
        //Debug.Log(gh[4096,4096]);
    }

    public void getheightarea()
    {
        Debug.Log(terrainPrefabs[3].GetComponent<Terrain>().terrainData.GetHeight(x, y));
        Debug.Log(terrainPrefabs[3].GetComponent<Terrain>().terrainData.GetHeight(x+1, y));
        Debug.Log(terrainPrefabs[3].GetComponent<Terrain>().terrainData.GetHeight(x + 1, y+1));
        Debug.Log(terrainPrefabs[3].GetComponent<Terrain>().terrainData.GetHeight(x, y+1));



    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            stitchTerrains();
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            getheightarea();
        }
    }

    public void stitchTerrains()
    {
        for (int i = 0; i < tIndices; i++)//for each row of the subdivision 
        {
            for (int j = 0; j < tIndices; j++)//for each column of the subdivision
            {
                Terrain top = null, left = null, right = null, bottom = null;
                foreach (GameObject go in terrainPrefabs)
                {
                    if (terrainPrefabs[(i * tIndices) + j] != go)//if the terrain we're comparing to isn't itself
                    {
                        if (Vector3.Distance(go.transform.position, terrainPrefabs[(i * tIndices) + j].transform.position) <= terrainSize * 1.05f)//if the distance between tiles is small enough
                        {
                            if (go.transform.position.z - terrainPrefabs[(i * tIndices) + j].transform.position.z > terrainSize * .25f)
                                top = go.GetComponent<Terrain>();
                            else if (go.transform.position.z - terrainPrefabs[(i * tIndices) + j].transform.position.z < -terrainSize * .25f)
                                bottom = go.GetComponent<Terrain>();
                            else if (go.transform.position.x - terrainPrefabs[(i * tIndices) + j].transform.position.x < -terrainSize * .25f)
                                left = go.GetComponent<Terrain>();
                            else if (go.transform.position.x - terrainPrefabs[(i * tIndices) + j].transform.position.x > terrainSize * .25f)
                                right = go.GetComponent<Terrain>();
                        }
                    }

                }
                terrainPrefabs[(i * tIndices) + j].GetComponent<Terrain>().SetNeighbors(left, top, right, bottom);
            }

        }

        Debug.Log("Top: " + terrainPrefabs[0].GetComponent<Terrain>().topNeighbor);
            Debug.Log("Right: " + terrainPrefabs[0].GetComponent<Terrain>().rightNeighbor);
            Debug.Log("Bottom: " + terrainPrefabs[0].GetComponent<Terrain>().bottomNeighbor);
            Debug.Log("Left: " + terrainPrefabs[0].GetComponent<Terrain>().leftNeighbor);

        int hr = terrainPrefabs[0].GetComponent<Terrain>().terrainData.heightmapResolution;//hr=heightmap resolution

        for (int i = 0; i < tIndices; i++)//for each row of the subdivision 
        {
            for (int j = 0; j < tIndices; j++)//for each column of the subdivision
            {
                Terrain ct = terrainPrefabs[(i * tIndices) + j].GetComponent<Terrain>();//ct=current terrain
                float ctHeight = ct.terrainData.size.y;
                float[,] ctDupe = ct.terrainData.GetHeights(0, 0, hr, hr);//making a copy of the height array for the original terrain data to work with

                if (ct.topNeighbor != null)
                {                   

                    float[,] topDupe = ct.topNeighbor.terrainData.GetHeights(0, 0, hr, hr);//duplicate the terrain data from above to work with
                    float topHeight = ct.topNeighbor.terrainData.size.y;
                    float average;
                    for (int k = 0; k < hr; k++)
                    {
                        average=(((topDupe[k, 0]*topHeight) + (ctDupe[k,hr-1]*ctHeight)) / 2.0f);//we must multiply by heights because actual height values are 0 to 1 multiplied by the terrain size
                        topDupe[k, 0] = average/topHeight;
                        ctDupe[k, hr-1] = average/ctHeight;
                    }
                    ct.topNeighbor.terrainData.SetHeights(0,0, topDupe);
                }
                if (ct.rightNeighbor != null)
                {
                    float[,] rightDupe = ct.rightNeighbor.terrainData.GetHeights(0, 0, hr, hr);//duplicate the terrain data from above to work with
                    float rightHeight = ct.rightNeighbor.terrainData.size.y;

                    float average;
                    for (int k = 0; k < hr; k++)
                    {
                        average = (((rightDupe[0, k]*rightHeight) + (ctDupe[hr-1, k]*ctHeight)) / 2.0f);
                        rightDupe[0, k] =  average/rightHeight;
                        ctDupe[hr-1, k] = average/ctHeight;
                    }
                    ct.rightNeighbor.terrainData.SetHeights(0, 0, rightDupe);
                }
                if (ct.leftNeighbor != null)
                {
                    float[,] leftDupe = ct.leftNeighbor.terrainData.GetHeights(0, 0, hr, hr);//duplicate the terrain data from above to work 
                    float leftHeight = ct.leftNeighbor.terrainData.size.y;
                    float average;
                    for (int k = 0; k < hr; k++)
                    {
                        average = (((leftDupe[hr-1, k]*leftHeight) + (ctDupe[0, k]*ctHeight)) / 2.0f);
                        Debug.Log("Average equals: "+ (leftDupe[hr - 1, k] * leftHeight)+" and "+ (ctDupe[0, k] * ctHeight));
                        leftDupe[hr - 1, k] =  average/leftHeight;
                        ctDupe[0, k] = average/ctHeight;
                    }
                    ct.leftNeighbor.terrainData.SetHeights(0, 0, leftDupe);
                }
                if (ct.bottomNeighbor != null)
                {
                    float[,] bottomDupe = ct.bottomNeighbor.terrainData.GetHeights(0, 0, hr, hr);//duplicate the terrain data from above to work with
                    float bottomHeight = ct.bottomNeighbor.terrainData.size.y;
                    float average;
                    for (int k = 0; k < hr; k++)
                    {
                        average = (((bottomDupe[k, hr-1]*bottomHeight)+ (ctDupe[k, 0]*ctHeight)) / 2.0f);
                        bottomDupe[k, hr-1] = average/bottomHeight;
                        ctDupe[k, 0] = average/ctHeight;
                    }
                    ct.bottomNeighbor.terrainData.SetHeights(0, 0, bottomDupe);
                }
                ct.terrainData.SetHeights(0, 0, ctDupe);


            }

        }

     }
}
