using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utils;

public class SplitByCenterPosition : MonoBehaviour
{
#if UNITY_EDITOR
    [ContextMenu("Split")]
    public void SplitStaticObjects()
    {
        GameObject staticObjectParent = gameObject;
        List<GameObject> dirtyObjects = new List<GameObject>();
        dirtyObjects.Add(staticObjectParent);

        for (int i = staticObjectParent.transform.childCount - 1; i >= 0; i--)
        {
            Transform staticObject = staticObjectParent.transform.GetChild(i);
            Terrain nearestTerrain = TerrainUtils.getNearestTerrain(staticObject.GetComponent<Renderer>().bounds.center);
            
            staticObject.parent = nearestTerrain.gameObject.transform;
            if (!dirtyObjects.Contains(nearestTerrain.gameObject))
                dirtyObjects.Add(nearestTerrain.gameObject);

            //Vector3 groundedPosition = staticObject.position;
            //groundedPosition.y = TerrainUtils.getTerrainHeight(groundedPosition);
            //staticObject.position = groundedPosition;

        }
        foreach (GameObject g in dirtyObjects)
            UnityEditor.EditorUtility.SetDirty(g);
    }
#endif
}
