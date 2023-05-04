using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using System;

public class TaskManager : MonoBehaviour
{
    public GameObject [] tasks;
    [HideInInspector]
    public Task selectedTask;
    public float[] agentAllowedArea;
    void Start()
    {
        //int taskIndex = (int)Academy.Instance.EnvironmentParameters.GetWithDefault("selectedTaskIndex", 0);
        int taskIndex = (int)CommandLineArgs.Instance.Parameters.GetWithDefault("selectedTaskIndex", 0);
        GameObject taskObject = Instantiate(tasks[taskIndex]);
        selectedTask = taskObject.GetComponent<Task>();
        selectedTask.agentAllowedArea = agentAllowedArea;
    }
}
