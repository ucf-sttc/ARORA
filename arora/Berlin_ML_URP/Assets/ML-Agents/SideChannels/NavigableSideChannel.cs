/*
 * Used for either querying if a point is navigable by the agent or requesting a random navigable point
 */
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.SideChannels;
using UnityEngine;

public class NavigableSideChannel : SideChannel
{
    NavArea m_navArea;

    public NavigableSideChannel()
    {
        ChannelId = new Guid("fbae7da3-76e8-4c37-86c9-ad647c74fd69");
    }

    protected override void OnMessageReceived(IncomingMessage msg)
    {
        if(!m_navArea)
        {
            m_navArea = GameObject.FindObjectOfType<NavArea>();
            if(!m_navArea)
            {
                Debug.LogError("No NavArea found. Usually means task has not been initialized");
                return;
            }
        }

        OutgoingMessage outMessage = new OutgoingMessage();
        string messageType = msg.ReadString("EMPTY STRING");
        IList<float> variables = msg.ReadFloatList();
        IList<float> outValues = new List<float>();

        if (variables == null || variables.Count == 0) // no arguments, return random navigable point
        {
            Vector3 navPoint = m_navArea.SampleNavigableNavmesh();
            if(navPoint != Vector3.negativeInfinity)
            {
                outValues.Add(navPoint.x);
                outValues.Add(navPoint.y);
                outValues.Add(navPoint.z);
            }
        }
        else if(variables.Count == 2) // no height given, find a point at the given x,z and check if navigable
        {
            if(m_navArea.IsNavigableFromAgent(new Vector2(variables[0], variables[1]), out Vector3 o))
            {
                outValues.Add(o.x);
                outValues.Add(o.y);
                outValues.Add(o.z);
            }

        }
        else if(variables.Count == 3) // check if point is navigable
        {
            if (m_navArea.IsNavigableFromAgent(new Vector3(variables[0], variables[1], variables[2])))
            {
                outValues.Add(variables[0]);
                outValues.Add(variables[1]);
                outValues.Add(variables[2]);
            }
        }
        else
        {
            Debug.LogError("Expected 0, 2, or 3 arguments, but instead received " + variables.Count);
            return;
        }

        outMessage.WriteFloatList(outValues);
        QueueMessageToSend(outMessage);
    }
}
