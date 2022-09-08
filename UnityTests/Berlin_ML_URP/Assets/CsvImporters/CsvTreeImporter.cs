using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using Utils;

public class CsvTreeImporter : CsvImporter
{
#if UNITY_EDITOR
    public GameObject treeColliderPrefab;
    public List<GameObject> TreeModels;
    public int xTerrainTiling, yTerrainTiling, terrainTileNameOffset;
    public struct TreeToSpawn
    {
        public int index;
        public GameObject colliderObject;
        public float x, y, maxW, minW, maxH, minH;

        public TreeToSpawn(int i, float x1, float y1, float mW, float mnW, float mH, float mnH, GameObject colliderObject)
        {
            index = i;
            x = x1;
            y = y1;
            maxW = mW;
            minW = mnW;
            maxH = mH;
            minH = mnH;
            this.colliderObject = colliderObject;
        }
    }

    public List<TreeToSpawn> treesToSpawn = new List<TreeToSpawn>();

    override protected IEnumerator CreateObject(AttributeClass.Attribute[] attributes)
    {
        string[] s = AttributeClass.GetValueForKeyFromAttributeArray("MODEL_NAME", attributes)?.Split(new char[] { '/', '.' });
        if (s == null)
        {
            Debug.LogError("MODEL_NAME value not found");
            yield break;
        }
        string assetPath = AttributeClass.GetValueForKeyFromAttributeArray("MODEL_PATH", attributes) + AttributeClass.GetValueForKeyFromAttributeArray("MODEL_NAME", attributes);
        GameObject prefab = AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject)) as GameObject;
        if (prefab == null)
        {
            Debug.LogError("Prefab at " + assetPath + " not found");
            yield break;
        }
        if (!TreeModels.Contains(prefab))
            TreeModels.Add(prefab);
        GameObject colliderObject = (GameObject)PrefabUtility.InstantiatePrefab(treeColliderPrefab, objectParent.transform);
        if (objectParent.isStatic)
            colliderObject.isStatic = true;
        AttributeClass attributeClass = colliderObject.AddComponent<AttributeClass>();
        attributeClass.attributes = attributes;

        treesToSpawn.Add(new TreeToSpawn(TreeModels.IndexOf(prefab), (float)(double.Parse(attributeClass.GetValueForKey("X")) - X_offset), (float)(double.Parse(attributeClass.GetValueForKey("Y")) - Y_offset), float.Parse(attributeClass.GetValueForKey("MAX_WID_FR")), float.Parse(attributeClass.GetValueForKey("MIN_WID_FR")), float.Parse(attributeClass.GetValueForKey("MAX_HGT_FR")), float.Parse(attributeClass.GetValueForKey("MIN_HGT_FR")), colliderObject));
        //WE ARE DONE ESTABLISHING THE TRACKING OF THE OBJECTS

        //copy.name = "Model: " + attributeClass.getValueForKey("MODEL + "___" + attributeClass.getValueForKey("Y.ToString();
        Vector3 position = new Vector3(((float)(double.Parse(attributeClass.GetValueForKey("X")) - X_offset)), 100, ((float)(double.Parse(attributeClass.GetValueForKey("Y")) - Y_offset)));
        position.y = TerrainUtils.getTerrainHeight(position);
        colliderObject.transform.position = position;
        colliderObject.transform.localScale = new Vector3(float.Parse(attributeClass.GetValueForKey("MAX_WID_FR")), float.Parse(attributeClass.GetValueForKey("MAX_HGT_FR")), float.Parse(attributeClass.GetValueForKey("MAX_WID_FR")));
        yield return null;
    }

    override
    protected void OnFileEnd()
    {
        List<TreePrototype> tpl = new List<TreePrototype>();
        foreach (GameObject go in TreeModels)
            if (go != null)
                tpl.Add(new TreePrototype());

        TreePrototype[] tp = tpl.ToArray();


        for (int i = 0; i < tp.Length; i++)
        {
            tp[i] = new TreePrototype();
        }

        for (int i = 0; i < tp.Length; i++)
        {
            tp[i].prefab = TreeModels[i];

        }

        foreach (Terrain td in GameObject.FindObjectsOfType<Terrain>())
        {
            td.terrainData.treePrototypes = tp;
        }



        //NOW HERE'S WHERE WE NEED TO SPAWN THE TREES ON THE TERRAIN THEMSELVES
        PlaceFinalTrees();
        base.OnFileEnd();
    }
    void PlaceFinalTrees()
    {
        for (int i = 0; i < treesToSpawn.Count; i++)
        {
            TreeInstance treeInstance = new TreeInstance();
            Vector3 treeInstanceWorldPosition = new Vector3(treesToSpawn[i].x, 0f, treesToSpawn[i].y);

            treeInstance.prototypeIndex = treesToSpawn[i].index;
            treeInstance.widthScale = Random.Range(treesToSpawn[i].minW, treesToSpawn[i].maxW);
            treeInstance.heightScale = Random.Range(treesToSpawn[i].maxH, treesToSpawn[i].minH);

            Terrain nearestTerrain = TerrainUtils.getNearestTerrain(treeInstanceWorldPosition);
            treeInstance.position = new Vector3(
                (treesToSpawn[i].x-nearestTerrain.transform.position.x) / nearestTerrain.terrainData.size.x, 
                0, 
                (treesToSpawn[i].y-nearestTerrain.transform.position.z) / nearestTerrain.terrainData.size.z);

            if (treeInstance.position.x >= 0 && treeInstance.position.x < 1 && treeInstance.position.z >= 0 && treeInstance.position.z < 1)
                nearestTerrain.AddTreeInstance(treeInstance);
            else
            {
                //treesToSpawn[i].colliderObject.name += "_FAILED Point: " + terrainPoint.x +"," + terrainPoint.y;
                DestroyImmediate(treesToSpawn[i].colliderObject);
                Debug.LogError("Could not find terrain for tree: " + i);
            }

        }
        Debug.Log("done creating trees");

    }
    /*
    void PlaceFinalTrees()
    {
        Terrain[,] allTerrains = new Terrain[xTerrainTiling, yTerrainTiling];
        for (int i = 0; i < xTerrainTiling; i++)
        {
            for (int j = 0; j < yTerrainTiling; j++)
            {
                allTerrains[i, j] = GameObject.Find("Terrain" + (terrainTileNameOffset + i + (j * xTerrainTiling)).ToString())?.GetComponent<Terrain>();
                if (allTerrains[i, j] == null)
                    Debug.Log("couldn't find Terrain" + i + (j * xTerrainTiling) + " aka i=" + i + " j=" + j);
            }

        }
        float TerrainSize = allTerrains[0, 0].terrainData.size.x;

        for (int i = 0; i < treesToSpawn.Count; i++)
        {
            TreeInstance treeInstance = new TreeInstance();
            // random placement modifier for a more natural look
            float xpos = (treesToSpawn[i].x % TerrainSize) / TerrainSize;
            float ypos = (treesToSpawn[i].y % TerrainSize) / TerrainSize;
            //ypos = Mathf.Clamp01(1 - ypos);
            //treeInstance.color = 

            treeInstance.position = new Vector3(xpos, 0f, ypos);
            //treeInstance.color = Color.white;
            //treeInstance.lightmapColor = Color.white;
            treeInstance.prototypeIndex = treesToSpawn[i].index;
            treeInstance.widthScale = Random.Range(treesToSpawn[i].minW, treesToSpawn[i].maxW);
            treeInstance.heightScale = Random.Range(treesToSpawn[i].maxH, treesToSpawn[i].minH);
            //Debug.Log((int)(treesToSpawn[i].x / TerrainSize) + "," + (int)(treesToSpawn[i].y / TerrainSize));
            if ((int)(treesToSpawn[i].x / TerrainSize) < xTerrainTiling && (int)(treesToSpawn[i].x / TerrainSize) >= 0 && (int)(treesToSpawn[i].y / TerrainSize) >= 0 && (int)(treesToSpawn[i].y / TerrainSize) < yTerrainTiling)
                allTerrains[(int)(treesToSpawn[i].x / TerrainSize), yTerrainTiling - 1 - (int)(treesToSpawn[i].y / TerrainSize)]?.AddTreeInstance(treeInstance);
            else
            {
                DestroyImmediate(treesToSpawn[i].colliderObject);
                Debug.LogError("Could not find terrain for tree: " + i);
            }
                
        }
        Debug.Log("done creating trees");
    }
    */
#endif
}
