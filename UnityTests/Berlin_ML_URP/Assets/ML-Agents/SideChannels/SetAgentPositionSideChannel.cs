using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.SideChannels;
using UnityEngine;

public class SetAgentPositionSideChannel : SideChannel
{
    TaskManager taskManager;
    GameObject agent;
    Vector3 oldPosition;
    Quaternion oldRotation;
    public SetAgentPositionSideChannel(TaskManager taskManager)
    {
        ChannelId = new Guid("821d1b06-5035-4518-9e67-a34946637260");
        this.taskManager = taskManager;
        if (taskManager == null)
            Debug.LogError("Failed to locate a task manager");
    }
    protected override void OnMessageReceived(IncomingMessage msg)
    {
        OutgoingMessage outMessage = new OutgoingMessage();
        string messageType = msg.ReadString("EMPTY STRING");
        IList<float> variables = msg.ReadFloatList();

        if (variables == null || (variables.Count != 1 && variables.Count != 4 && variables.Count != 8))
        {
            Debug.LogError("Coordinate list of improper length. Expected 1, 4 or 8 entries, got " + (variables == null ? "NULL" : variables.Count.ToString()));
            return;
        }
        Task task = taskManager.selectedTask;
        agent = task.m_Agents[(int)variables[0]];
        oldPosition = agent.transform.position;
        oldRotation = agent.transform.rotation;
        if(variables.Count >= 4)
            agent.transform.position = new Vector3(variables[1], variables[2], variables[3]);
        if (variables.Count == 8)
            agent.transform.rotation = new Quaternion(variables[4], variables[5], variables[6], variables[7]);
        switch(messageType)
        {
            case "agentPosition":
                if (task.m_NavigationArea.GetShortestPathDistanceFromAgentToGoal(agent.GetComponent<Agent>(), task.m_goalPoint[(int)variables[0]]) != -1
                    && task.m_NavigationArea.CheckClearance(agent.transform.position) > 3) //Valid location for agent placement
                {
                    DynamicSceneLoader.CallInitialTileLoad();
                    outMessage.WriteBoolean(true);
                    //agent.GetComponent<Agent>().ClearObservations();
                    agent.GetComponent<Agent>().SendInfoToBrain(true);
                }
                else //Position is invalid for agent spawn
                {
                    agent.transform.position = oldPosition;
                    agent.transform.rotation = oldRotation;
                    outMessage.WriteBoolean(false);
                }
                break;
            case "getObservation":
                if(agent.transform.position != oldPosition || agent.transform.rotation != oldRotation)
                {
                    DynamicSceneLoader.CallInitialTileLoad();
                    outMessage.WriteBoolean(true);
                    //agent.GetComponent<Agent>().ClearObservations();
                    agent.GetComponent<Agent>().SendInfoToBrain(true);
                    agent.transform.position = oldPosition;
                    agent.transform.rotation = oldRotation;
                    DynamicSceneLoader.CallInitialTileLoad();
                }
                else
                {
                    outMessage.WriteBoolean(true);
                    agent.GetComponent<Agent>().SendInfoToBrain(true);
                }
                
                break;
            default:
                Debug.LogError("Unexpected message type passed to SetAgentPositionSideChannel. Accepted message types are 'agentPosition' and 'getObservation. Received: " + messageType);
                break;
        }
        
        QueueMessageToSend(outMessage);
    }
}
