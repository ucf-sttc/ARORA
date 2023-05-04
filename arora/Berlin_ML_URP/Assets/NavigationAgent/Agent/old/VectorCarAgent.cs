using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class VectorCarAgent : CarAgent
{
    RaycastHit hit;
    public override void CollectObservations(VectorSensor sensor)
    {
        base.CollectObservations(sensor);
        sensor.AddObservation(transform.position);
        sensor.AddObservation(this.rbody.velocity);
        sensor.AddObservation(transform.rotation);
        //This is assuming there is only 1 goal point
        if (m_Task.m_goalPoint.Length > 0)
            sensor.AddObservation(m_Task.m_goalPoint[0].transform.position);
        sensor.AddObservation(Physics.Raycast(transform.position, transform.forward, out hit, 500) ? hit.distance : 500);
        Debug.DrawRay(transform.position, transform.forward);
        sensor.AddObservation(Physics.Raycast(transform.position, transform.forward - transform.right, out hit, 500) ? hit.distance : 500);
        Debug.DrawRay(transform.position, transform.forward - transform.right);
        sensor.AddObservation(Physics.Raycast(transform.position, transform.forward + transform.right, out hit, 500) ? hit.distance : 500);
        Debug.DrawRay(transform.position, transform.forward + transform.right);
    }
}
