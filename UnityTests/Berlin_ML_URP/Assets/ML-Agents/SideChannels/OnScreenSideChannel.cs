/*
 * TODO: Currently does not display anything in ARORA
 * Receive key, value string pairs from python to display on screen
 */
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.SideChannels;
using UnityEngine;
using UnityEngine.UI;

public class OnScreenSideChannel : SideChannel
{
    public Text m_text;

    public OnScreenSideChannel(GameObject debugDisplayGO)
    {
        ChannelId = new Guid("e018c382-a6b9-4ea7-8f22-2b73c6dc54e3");

        if (debugDisplayGO)
            m_text = debugDisplayGO.GetComponent<Text>();
        else
            Debug.LogWarning("No canvas display object passed to OnScreenSideChannel");
    }

    protected override void OnMessageReceived(IncomingMessage msg)
    {
        if (m_text == null) return;
        if (!m_text.gameObject.activeSelf) m_text.gameObject.SetActive(true);

        m_text.text = "";

        for (int i = 0; i < 100; i++)
        {
            string key = msg.ReadString();
            string value = msg.ReadString();

            if (key == null || value == null) break;

            m_text.text += string.Format("{0}: {1}\n", key, value);
        }
    }
}