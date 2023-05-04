// Traffic Simulation
// https://github.com/mchrbn/unity-traffic-simulation

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
//using Unity.Entities;
using UnityEngine;

public class TrafficSystem : MonoBehaviour {
    public bool showPathGuizmos = true;
    public bool showLabelGuizmos = true;
    public float segDetectThresh = 1.5f;
    public ArrowDraw arrowDrawType = ArrowDraw.ByLength;
    public int arrowCount = 1;
    public float arrowDistance = 5;
    public float arrowSizeWaypoint = 1;
    public float arrowSizeIntersection = 0.5f;
    public float waypointSize = 0.5f;
        
    public List<Segment> segments = new List<Segment>();
    public List<Intersection> intersections = new List<Intersection>();
        
    public Segment curSegment = null;
    public GameObject agent;
    [HideInInspector]
    public TrafficCarSpawner tcs;
    //public EndSimulationEntityCommandBufferSystem entityCommandBuffer;

    [HideInInspector]
    public int numCars = 0;

    public void Awake()
    {
        //int maxTrafficCars = (int)Academy.Instance.EnvironmentParameters.GetWithDefault("numberOfTrafficVehicles", 0);
        int maxTrafficCars = (int)CommandLineArgs.Instance.Parameters.GetWithDefault("numberOfTrafficVehicles", 0);
        gameObject.SetActive(!(maxTrafficCars == 0));
        tcs.maxCars = maxTrafficCars;
    }

    public List<Waypoint> GetAllWaypoints() {
        List<Waypoint> points = new List<Waypoint>();

        foreach (Segment segment in segments) {
            points.AddRange(segment.waypoints);
        }

        return points;
    }
}

public enum ArrowDraw {
    FixedCount, ByLength, Off
}
