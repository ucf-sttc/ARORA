using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class UniversalCarAgent : CarAgent
{
    [Tooltip("Because we want an observation right before making a decision, we can force " +
       "a camera to render before making a decision. Place cameras here if using " +
       "RenderTexture as observations.")]
    public Camera[] renderCameras;
    public Task.Agent_Type agentType; //0-Vector 1-VectorVisual
    public SkyCarController controller;
    RaycastHit hit;

    private new void Start()
    {
        base.Start();
    }

    public override void Initialize()
    {
        switch (physicsMode)
        {
            case 1:
            case 2:
            case 10:
                controller.enabled = true;
                car = gameObject.AddComponent<Car>();
                break;
            default:
                carSimple = gameObject.AddComponent<CarSimple>();
                break;
        }

        if (agentType == Task.Agent_Type.VectorVisualAgent)
            foreach (Camera c in renderCameras)
                c.gameObject.SetActive(true);

        base.Initialize();
    }

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

        if (agentType == Task.Agent_Type.VectorVisualAgent)
            foreach (Camera c in renderCameras)
                if (c != null && c.gameObject.activeSelf)
                    c.Render();
    }

    public override void OnActionReceived(ActionBuffers vectorAction)
    {
        base.OnActionReceived(vectorAction);
        
    }
}
