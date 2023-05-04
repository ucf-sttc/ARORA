using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.SideChannels;
using UnityEngine;

public class PositionScanSideChannel : SideChannel
{
    public PositionScanSideChannel()
    {
        ChannelId = new Guid("a599964d-d747-4696-9a2d-d14cca2fa2e5");
    }
    protected override void OnMessageReceived(IncomingMessage msg)
    {
        OutgoingMessage outMessage = new OutgoingMessage();
        string messageType = msg.ReadString("EMPTY STRING");
        IList<float> variables = msg.ReadFloatList();
        float range = 1;
        AttributeClass ac;

        if (variables == null || variables.Count < 3 || variables.Count > 4)
        {
            Debug.LogError("Coordinate list of improper length. Expected 3 or 4 entries, got " + (variables == null ? "NULL" : variables.Count.ToString()));
            return;
        }

        Vector3 position = new Vector3(variables[0], variables[1], variables[2]);
        if (variables.Count > 3)
            range = variables[3];

        Collider[] colliders = Physics.OverlapSphere(position, range);

        foreach (Collider c in colliders)
        {
            ac = c.transform.GetComponentInParent<AttributeClass>();
            if (ac != null)
            {
                outMessage.WriteString(ac.ToString());
                QueueMessageToSend(outMessage);
                return;
            }
        }
        outMessage.WriteString("No object found");
        QueueMessageToSend(outMessage);
    }
}
