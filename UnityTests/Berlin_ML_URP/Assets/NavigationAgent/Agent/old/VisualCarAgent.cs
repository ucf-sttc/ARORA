using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class VisualCarAgent : CarAgent
{
    [Tooltip("Because we want an observation right before making a decision, we can force " +
        "a camera to render before making a decision. Place cameras here if using " +
        "RenderTexture as observations.")]
    public Camera[] renderCameras;

    EnvironmentParameters m_EnvParams;
    public override void Initialize()
    {
        base.Initialize();

        m_EnvParams = Academy.Instance.EnvironmentParameters;

        float segmentationMode = m_EnvParams.GetWithDefault("segmentationMode", -1f);
        //if (segmentationMode != -1 && GetComponentInChildren<SegmentationCamera>() != null)
        //    GetComponentInChildren<SegmentationCamera>().segmentationOutputMode = (int)segmentationMode;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        base.CollectObservations(sensor);
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
