using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using System.Diagnostics;
using Unity.MLAgents.Actuators;

public class FlyingAgent : Agent
{

    Rigidbody rBody;
    void Start()
    {
        rBody = GetComponent<Rigidbody>();
    }

    public Transform Target;
    public override void OnEpisodeBegin()
    {
        UnityEngine.Debug.Log("In OnEpisodeBegin");

        if (this.transform.localPosition.y < 0)
        {
            // If the Agent fell, zero its momentum
            this.rBody.angularVelocity = Vector3.zero;
            this.rBody.velocity = Vector3.zero;
            this.transform.localPosition = new Vector3(Random.value * 612 + 820,
                                           40f,
                                           Random.value * 816 + 1);
        }

        Target.localPosition = new Vector3(Random.value * 612 + 820,
                                           40f,
                                           Random.value * 816 + 1);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        UnityEngine.Debug.Log("In CollectObservations");

        // Target and Agent positions
        sensor.AddObservation(Target.localPosition);
        sensor.AddObservation(this.transform.localPosition);
        // Agent velocity
        sensor.AddObservation(rBody.velocity.x);
        sensor.AddObservation(rBody.velocity.z);
    }

    public float speed = 10;
    public override void OnActionReceived(ActionBuffers vectorAction)
    {
        UnityEngine.Debug.Log("In OnActionReceived");

        Vector3 controlSignal = Vector3.zero;
        controlSignal.x = vectorAction.ContinuousActions[0];
        controlSignal.z = vectorAction.ContinuousActions[1];
        rBody.AddForce(controlSignal * speed);

        float distanceToTarget = Vector3.Distance(this.transform.localPosition, Target.localPosition);
        UnityEngine.Debug.Log("Distance to Target: " + distanceToTarget);
        // Reached target
        if (distanceToTarget < 50f)
        {

            UnityEngine.Debug.Log("Target Reached");
            SetReward(1.0f);
            EndEpisode();
        }

        // Fell off platform
        if (this.transform.localPosition.y < 0)
        {
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
    }
}