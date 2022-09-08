using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.SideChannels;
using UnityEngine;

public class AdditionalSideChannels : MonoBehaviour
{
    public static MapSideChannel mapChannel;
    public static PositionScanSideChannel positionScanChannel;
    public static NavigableSideChannel navigableSideChannel;
    public static FloatPropertiesChannel floatPropertiesChannel;
    public static SetAgentPositionSideChannel setAgentPositionSideChannel;
    public static OnScreenSideChannel onScreenSideChannel;
    public static ShortestPathSideChannel shortestPathSideChannel;

    GetNavMeshMap mapController;

    void Awake()
    {
        mapController = gameObject.AddComponent<GetNavMeshMap>();
        mapController.ImportMapPng(); // read map image file from Assets

        mapChannel = new MapSideChannel(mapController);
        positionScanChannel = new PositionScanSideChannel();
        navigableSideChannel = new NavigableSideChannel();
        floatPropertiesChannel = new FloatPropertiesChannel();
        setAgentPositionSideChannel = new SetAgentPositionSideChannel(FindObjectOfType<TaskManager>());
        onScreenSideChannel = new OnScreenSideChannel(null);
        shortestPathSideChannel = new ShortestPathSideChannel();

        SideChannelManager.RegisterSideChannel(mapChannel);
        SideChannelManager.RegisterSideChannel(positionScanChannel);
        SideChannelManager.RegisterSideChannel(navigableSideChannel);
        SideChannelManager.RegisterSideChannel(floatPropertiesChannel);
        SideChannelManager.RegisterSideChannel(setAgentPositionSideChannel);
        SideChannelManager.RegisterSideChannel(onScreenSideChannel);
        SideChannelManager.RegisterSideChannel(shortestPathSideChannel);
    }

    private void OnDestroy()
    {
        SideChannelManager.UnregisterSideChannel(mapChannel);
        SideChannelManager.UnregisterSideChannel(positionScanChannel);
        SideChannelManager.UnregisterSideChannel(navigableSideChannel);
        SideChannelManager.UnregisterSideChannel(floatPropertiesChannel);
        SideChannelManager.UnregisterSideChannel(setAgentPositionSideChannel);
        SideChannelManager.UnregisterSideChannel(onScreenSideChannel);
        SideChannelManager.UnregisterSideChannel(shortestPathSideChannel);
    }
}
