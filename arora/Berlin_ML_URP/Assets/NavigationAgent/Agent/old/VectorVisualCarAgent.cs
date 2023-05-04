using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class VectorVisualCarAgent : CarAgent
{
    [Tooltip("Because we want an observation right before making a decision, we can force " +
        "a camera to render before making a decision. Place cameras here if using " +
        "RenderTexture as observations.")]
    public Camera[] renderCameras;

    EnvironmentParameters m_EnvParams;
    RaycastHit hit;
    public override void Initialize()
    {
        base.Initialize();

        m_EnvParams = Academy.Instance.EnvironmentParameters;

        //float segmentationMode = m_EnvParams.GetWithDefault("segmentationMode", -1f);
        //if (segmentationMode != -1 && GetComponentInChildren<SegmentationCamera>() != null)
        //    GetComponentInChildren<SegmentationCamera>().segmentationOutputMode = (int)segmentationMode;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        base.CollectObservations(sensor);
        sensor.AddObservation(transform.position);
        sensor.AddObservation(this.rbody.velocity);
        sensor.AddObservation(transform.rotation);
        //This is assuming there is only 1 goal point
        if(m_Task.m_goalPoint.Length > 0)
            sensor.AddObservation(m_Task.m_goalPoint[0].transform.position);
        sensor.AddObservation(Physics.Raycast(transform.position, transform.forward, out hit, 500) ? hit.distance : 500);
        Debug.DrawRay(transform.position, transform.forward);
        sensor.AddObservation(Physics.Raycast(transform.position, transform.forward-transform.right, out hit, 500) ? hit.distance : 500);
        Debug.DrawRay(transform.position, transform.forward-transform.right);
        sensor.AddObservation(Physics.Raycast(transform.position, transform.forward+transform.right, out hit, 500) ? hit.distance : 500);
        Debug.DrawRay(transform.position, transform.forward+transform.right);
    }

    public override void OnActionReceived(ActionBuffers vectorAction)
    {
        base.OnActionReceived(vectorAction);
        foreach(Camera c in renderCameras)
            if (c != null && c.gameObject.activeSelf)
            {
                c.Render();
            }
    }
}
