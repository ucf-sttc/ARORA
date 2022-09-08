using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils;

public class SplitScenesBasedOnTerrainObjects : MonoBehaviour
{
#if UNITY_EDITOR
    public int gridWidth;
    public List<GameObject> StaticObjectParents = new List<GameObject>();
    bool splitStaticObjectsByCenterPosition;
    [ContextMenu("Setup terrain scenes")]
    void GenerateScenes()
    {
        int i;

        //Set static objects as children of terrain tiles
        SplitStaticObjectParents();

        GameObject[] terrainObjects = new GameObject[transform.childCount];
        for (i = 0; i < transform.childCount; i++)
            terrainObjects[i] = transform.GetChild(i).gameObject;
        i = 0;
        int x, y;
        for (y = 0; y < terrainObjects.Length / gridWidth; y++)
            for (x = 0; x < gridWidth; x++)
            {
                Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

                newScene.name = "" + x + "-" + y;
                GameObject newTerrain = terrainObjects[i++];
                newTerrain.transform.parent = null;
                SceneManager.MoveGameObjectToScene(newTerrain, newScene);

                EditorSceneManager.SaveScene(newScene, "Assets/Scenes/" + newScene.name + ".unity");
                Debug.Log("Saved Assets / Scenes / " + newScene.name + ".unity");
            }
    }

    [ContextMenu("Split static objects")]
    public void SplitStaticObjectParents()
    {
        for (int i = 0; i < StaticObjectParents.Count; i++)
        {
            SplitStaticObjects(StaticObjectParents[i]);
        }
    }
    public void SplitStaticObjects(GameObject staticObjectParent)
    {
        List<GameObject> dirtyObjects = new List<GameObject>();
        dirtyObjects.Add(staticObjectParent);

        for (int i = staticObjectParent.transform.childCount - 1; i >= 0; i--)
        {
            Transform staticObject = staticObjectParent.transform.GetChild(i);
            Terrain nearestTerrain = TerrainUtils.getNearestTerrain(staticObject.position);
            Transform matchingParent = nearestTerrain.gameObject.transform.Find(staticObjectParent.name);
            if (matchingParent == null)
            {
                matchingParent = new GameObject(staticObjectParent.name).transform;
                matchingParent.parent = nearestTerrain.transform;
                matchingParent.localPosition = Vector3.zero;
            }
            staticObject.parent = matchingParent;
            if (!dirtyObjects.Contains(matchingParent.gameObject))
                dirtyObjects.Add(matchingParent.gameObject);

            Vector3 groundedPosition = staticObject.position;
            groundedPosition.y = TerrainUtils.getTerrainHeight(groundedPosition);
            staticObject.position = groundedPosition;
            
        }
        foreach(GameObject g in dirtyObjects)
            UnityEditor.EditorUtility.SetDirty(g);
    }
#endif
}
