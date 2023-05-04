/*
 * 
 */
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.SideChannels;
using UnityEngine;

public class ShortestPathSideChannel : SideChannel
{
    public List<Vector3> m_path;

    public ShortestPathSideChannel()
    {
        ChannelId = new Guid("dc4b7d9a-774e-49bc-b73e-4a221070d716");
    }

    protected override void OnMessageReceived(IncomingMessage msg)
    {
        //Debug.LogWarning("Sending shortest path!");
        OutgoingMessage outMessage = new OutgoingMessage();
        List<float> output_list = new List<float>();

        if(m_path != null && m_path.Count > 0)
        {
            foreach (Vector3 v in m_path)
            {
                Vector3 temp = CoordinateConversion.ToNonNegative(v);
                output_list.Add(temp.x);
                output_list.Add(temp.y);
                output_list.Add(temp.z);
            }
        }
        else
        {
            output_list.Add(-999999f);
            output_list.Add(-999999f);
            output_list.Add(-999999f);
        }

        outMessage.WriteFloatList(output_list);
        QueueMessageToSend(outMessage);
    }
}
