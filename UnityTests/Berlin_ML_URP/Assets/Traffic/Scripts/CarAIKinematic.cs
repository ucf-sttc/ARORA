// Traffic Simulation
// https://github.com/mchrbn/unity-traffic-simulation

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Profiling;

public class CarAIKinematic : MonoBehaviour
{
    [Header("Traffic System")]
    public TrafficSystem trafficSystem;
    public int waypointThresh = 6;

    [Header("Vehicle")]
    public float speed = 20f;

    int curWp = 0;
    [HideInInspector]
    public int curSeg = 0;
    private Rigidbody body;


    void Start()
    {
        body = GetComponent<Rigidbody>();

        if(trafficSystem == null)
            return;

        FindSegment();
    }

    void FixedUpdate(){
        if(trafficSystem == null)
            return;

        Profiler.BeginSample("WpChecker");
        WaypointChecker();
        Profiler.EndSample();
        Profiler.BeginSample("Drive");
        Vector3 direction = trafficSystem.segments[curSeg].waypoints[curWp].transform.position - transform.position;
        body.MovePosition(transform.position + direction.normalized * speed * Time.deltaTime);
        Profiler.EndSample();
        Profiler.BeginSample("LookRotation");
        Quaternion qa = Quaternion.LookRotation(direction);
        Profiler.EndSample();
        Profiler.BeginSample("lerp");
        body.MoveRotation(Quaternion.Lerp(transform.rotation, qa, speed * Time.deltaTime));
        Profiler.EndSample();
    }

    void WaypointChecker(){
        GameObject waypoint = trafficSystem.segments[curSeg].waypoints[curWp].gameObject;
        //Position of next waypoint relative to the car
        Vector3 nextWp = this.transform.InverseTransformPoint(new Vector3(waypoint.transform.position.x, this.transform.position.y, waypoint.transform.position.z));

        //Go to next waypoint if arrived to current
        if(nextWp.magnitude < waypointThresh){
            curWp++;
            if(curWp >= trafficSystem.segments[curSeg].waypoints.Count){
                curSeg = GetNextSegmentId();
                curWp = 0;
            }
        }
    }

    int GetNextSegmentId(){
            
        if(trafficSystem.segments[curSeg].nextSegments.Count == 0)
        {
            // maybe reached edge of map, so despawn car?
            //Debug.Log("Car has no next waypoint, destroying...");
            Destroy(this.gameObject);
            return 0;
        }
        // pick random segment in nextSegments list
        //int c = Random.Range(0, trafficSystem.segments[curSeg].nextSegments.Count);
        //int c = TrafficCarSpawner.carRNG.Next(0, trafficSystem.segments[curSeg].nextSegments.Count);
        //TrafficCarSpawner.writer.WriteLine(c);
        //TrafficCarSpawner.writer.Flush();
        return trafficSystem.segments[curSeg].nextSegments[0].id; // TESTING choose first segment
        //return trafficSystem.segments[curSeg].nextSegments[c].id;
    }

    void FindSegment()
    {
        Profiler.BeginSample("FindSegment");
        foreach (Segment segment in trafficSystem.segments)
        {
            if (segment.IsOnSegment(this.transform.position))
            {
                curSeg = segment.id;

                //Find nearest waypoint to start within the segment
                float minDist = float.MaxValue;
                for (int j = 0; j < trafficSystem.segments[curSeg].waypoints.Count; j++)
                {
                    float d = Vector3.Distance(this.transform.position, trafficSystem.segments[curSeg].waypoints[j].transform.position);
                    //Only take in front points
                    Vector3 lSpace = this.transform.InverseTransformPoint(trafficSystem.segments[curSeg].waypoints[j].transform.position);
                    if (d < minDist && lSpace.z > 0)
                    {
                        minDist = d;
                        curWp = j;
                    }
                }
                break;
            }
        }
        Profiler.EndSample();
    }

    public void OnDestroy()
    {
        trafficSystem.tcs.cars.Remove(this.gameObject);
        trafficSystem.numCars--;
    }
}