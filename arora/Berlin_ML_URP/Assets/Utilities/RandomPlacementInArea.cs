using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RandomPlacementInArea : MonoBehaviour
{
    int i = 0;
    private void FixedUpdate()
    {
        if(Time.timeScale != 0)
        {
            Vector3 randomPosition = new Vector3(Random.Range(1f, 32*102f), 40, Random.Range(1f, 26*102f));
            Debug.Log("New agent location: " + randomPosition.ToString());
            transform.position = randomPosition;

            Debug.Log("Time: "+ Time.realtimeSinceStartupAsDouble + ". Terrain load: "+ i++ + ". Tiles waiting to unload: " + DynamicSceneLoader.instance.tilesWaitingToUnload.Count);
            DynamicSceneLoader.CallInitialTileLoad();
        }
    }
}
