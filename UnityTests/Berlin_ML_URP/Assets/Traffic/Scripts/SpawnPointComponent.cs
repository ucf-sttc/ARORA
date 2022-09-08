using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class SpawnPointComponent : MonoBehaviour
{
    public int wp, seg;
    

    public void Awake()
    {
        //TrafficCarSpawner.loadEvent?.Invoke(this);
    }

    public void OnDestroy()
    {
        //TrafficCarSpawner.unloadEvent?.Invoke(this);
    }
}
