// Traffic Simulation
// https://github.com/mchrbn/unity-traffic-simulation

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Waypoint : MonoBehaviour {
    [HideInInspector]
    public int id;
    [HideInInspector]
    public Segment segment;

    public void Init(int newId, Segment newSegment) {
        id = newId;
        segment = newSegment;
        name = "Waypoint-" + newId;
        tag = "Waypoint";
            
        //Set the layer to Default
        gameObject.layer = 0;
            
        AddCollider();
    }

    public void AddCollider()
    {
        gameObject.AddComponent<SphereCollider>();
    }

    public void RemoveCollider() {
        if (GetComponent<SphereCollider>()) {
            DestroyImmediate(gameObject.GetComponent<SphereCollider>());
        }
    }

    public Vector3 GetVisualPos() {
        return transform.position + new Vector3(0, 0.5f, 0);
    }
}