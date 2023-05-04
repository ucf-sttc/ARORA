using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;

public class NavAgent : Agent
{
    protected Task m_Task;
    [HideInInspector]
    public Rigidbody rbody;
    public bool relativeSteering;
    public override void Initialize()
    {
        base.Initialize();
        rbody = GetComponent<Rigidbody>();
    }

    public void SetTask(Task t)
    {
        m_Task = t;
    }

    public Task GetTask()
    {
        return m_Task;
    }
}
