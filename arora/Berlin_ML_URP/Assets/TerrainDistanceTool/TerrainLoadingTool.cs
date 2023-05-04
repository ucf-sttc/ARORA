using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;

public class TerrainLoadingTool : MonoBehaviour
{
    
    public Terrain[,] terrainGrid;
    public int gridWidth;
    public string terrainTileNamePrefix;
    public GameObject agent;
    Terrain lastTerrainTile;
    public int range;
    public float pixelErrorModificationMultiplier;

    public List<GameObject> StaticObjectParents = new List<GameObject>();

    Terrain[] terrains;
    int gridHeight;

    // Start is called before the first frame update
    void Start()
    {
        terrains = FindObjectsOfType<Terrain>().OrderBy(x=>int.Parse(x.name.Substring(terrainTileNamePrefix.Length))).ToArray();
        gridHeight = terrains.Length / gridWidth;
        terrainGrid = new Terrain[gridWidth, gridHeight];

        for (int i = 0; i < terrains.Length; i++)
        {
            int x = i % gridWidth;
            int y = i / gridWidth;

            terrainGrid[x, y] = terrains[i];
            terrains[i].gameObject.AddComponent<gridPosition>().setPosition(x,y);
        }
        Debug.Log("Terrain grid size" + gridWidth + ", " + gridHeight + ". Terrain count: " + terrains.Length);

        //Set static objects as children of terrain tiles
        for(int i = 0; i < StaticObjectParents.Count;i++)
        {
            SplitStaticObjects(StaticObjectParents[i], false);    
        }
    }

    private void FixedUpdate()
    {
        if(agent != null)
        {
            Terrain currentTerrainTile = TerrainUtils.getNearestTerrain(agent.transform.position);
            if (currentTerrainTile != null && (lastTerrainTile == null || lastTerrainTile != currentTerrainTile))
                UpdateActiveTerrainTiles(currentTerrainTile);
        }
    }

    void UpdateActiveTerrainTiles(Terrain currentTerrainTile)
    {
        int x, y;
        int[] position = currentTerrainTile.GetComponent<gridPosition>().position;
        List<Vector2> newActiveTiles = new List<Vector2>();

        lastTerrainTile = currentTerrainTile;

        for (x = position[0] - range; x <= position[0] + range; x++)
            for (y = position[1] - range; y <= position[1] + range; y++)
                    newActiveTiles.Add(new Vector2(x, y));

        for (x = 0; x < gridWidth; x++)
            for (y = 0; y < gridHeight; y++)
            {
                bool activate = newActiveTiles.Contains(new Vector2(x, y));
                terrainGrid[x, y].gameObject.SetActive(activate);
                if (activate)
                    terrainGrid[x, y].heightmapPixelError = Mathf.Max(1, Mathf.Pow(Mathf.Abs(position[0] - x) + Mathf.Abs(position[1] - y)-1, pixelErrorModificationMultiplier));
            }
    }

    public void SplitStaticObjects(GameObject staticObjectParent, bool addToStaticObjectParents)
    {
        if(terrains != null)
        {
            for (int i = staticObjectParent.transform.childCount - 1; i >= 0; i--)
            {
                Transform staticObject = staticObjectParent.transform.GetChild(i);
                staticObject.parent = TerrainUtils.getNearestTerrain(staticObject.position, terrains).transform;
            }
        }
        if(addToStaticObjectParents)
            StaticObjectParents.Add(staticObjectParent);
    }

    Terrain[] GetRow(int rowNumber)
    {
        return Enumerable.Range(0, terrainGrid.GetLength(0))
                .Select(x => terrainGrid[x, rowNumber])
                .ToArray();
    }

    Terrain[] GetColumn(int columnNumber)
    {
        return Enumerable.Range(0, terrainGrid.GetLength(1))
                .Select(x => terrainGrid[columnNumber, x])
                .ToArray();
    }
}