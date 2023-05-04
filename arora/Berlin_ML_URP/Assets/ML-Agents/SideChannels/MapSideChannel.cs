using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.SideChannels;
using UnityEngine;

public class MapSideChannel : SideChannel
{
    GetNavMeshMap mapController;

    public MapSideChannel(GetNavMeshMap gnmm)
    {
        ChannelId = new Guid("24b099f1-b184-407c-af72-f3d439950bdb");
        mapController = gnmm;
    }
    protected override void OnMessageReceived(IncomingMessage msg)
    {
        OutgoingMessage outMessage = new OutgoingMessage();
        string messageType = msg.ReadString("EMPTY STRING");

        if (messageType == "binaryMapZoom")
        {
            IList<float> variables = msg.ReadFloatList();

            if (variables == null || variables.Count != 2)
            {
                Debug.LogError("Expected 2 arguments. Received: " + (variables == null ? "NULL" : variables.Count.ToString()));
                return;
            }
            int row = Mathf.FloorToInt(variables[0]);
            int col = Mathf.FloorToInt(variables[1]);

            outMessage.SetRawBytes(mapController.CreateZoomMap(col, row));
        }
        else if(messageType == "mapSizeRequest")
        {
            outMessage.WriteString("mapSizeResponse");
            outMessage.WriteFloat32(mapController.m_lengthX);
            outMessage.WriteFloat32(mapController.m_lengthZ);
        }
        else // binaryMap is default
        {
            IList<float> variables = msg.ReadFloatList();

            if (variables != null && variables.Count == 0)
            {
                outMessage.SetRawBytes(mapController.GetMap());
            }
            else if(variables != null && variables.Count == 3)
            {
                int xResolution = Mathf.FloorToInt(variables[0]);
                int yResolution = Mathf.FloorToInt(variables[1]);
                float threshold = Mathf.Clamp01(variables[2]);

                outMessage.SetRawBytes(mapController.DownsampleMap(xResolution, yResolution, threshold));
            }
            else
            {
                Debug.LogError("binaryMap expects 0 or 3 arguments. Received: " + (variables == null ? "NULL" : variables.Count.ToString()));
                return;
            }
        }

        QueueMessageToSend(outMessage);
    }
}
