using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkinsListener: MonoBehaviour  {

    public Text connectionText; 
    public Text hbText;
    public Text fireText;
    public Text modeSwitchText;
    public Text chargingHandleText;
    public Text forwardAssistText;
    public Text powerRemainingText;
	public Text errorMessageText;

    long lastMessageTime = 0;

	public JavaInterfaceToSerial javaInterfaceToSerial;

    public SkinsListener()
    {
        //set up the events...
        //public static event NewHeartBeatEvent newHeartBeatEvent;
       SkinsEventParser.newHeartBeatEvent += newHeartBeatEvent;
        //public static event NewFireEvent newFireEvent;
       SkinsEventParser.newFireEvent += newFireEvent; 
        //public static event NewModeSwitchEvent newModeSwitchEvent;
       SkinsEventParser.newModeSwitchEvent += newModeSwitchEvent;
        //public static event NewChargingHandleEvent newChargingHandleEvent;
       SkinsEventParser.newChargingHandleEvent += newChargingHandleEvent; 
        //public static event NewForwardAssistEvent newForwardAssistEvent;
       SkinsEventParser.newForwardAssistEvent += newForwardAssistEvent;
        //public static event NewPowerRemainingEvent newPowerRemainingEvent;
       SkinsEventParser.newPowerRemainingEvent += newPowerRemainingEvent;

    }

	void Start()
	{
		InvokeRepeating ("updateConnection",0,1);
		InvokeRepeating ("updateErrorMessage",0,1);
		lastMessageTime = currentTimeMillis ();
	}

    /** 
     * Send the last message time over
     */
    void updateConnection()
    {
		if (connectionText != null)
		{
			if ((currentTimeMillis() - lastMessageTime) < 11000)
			{
				connectionText.color = Color.black;
				connectionText.text = "Last message: " + (currentTimeMillis() - lastMessageTime) + " ms ago";
			}
	        else
			{
				connectionText.color = Color.red;
				connectionText.text = "Warning. Exceeded timeout. " + (currentTimeMillis() - lastMessageTime) + " ms since last message";
	        }
		}
    }

	public void updateErrorMessage()
	{
		if(javaInterfaceToSerial.queueOverload)
			errorMessageText.text = "Queue was overloaded. Messages received while queue is full are discarded";
	}
    
    //Event functions update text fields and fire debug messages if there is an error
    public void newHeartBeatEvent(ushort heartBeatNum, long timestamp)
    {
        if (hbText != null)
        {
            Debug.Log("Timestamp: " + timestamp);
            //long currTime = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
           // long currTime = currentTimeMillis();
            hbText.text = "Heartbeat event number: " + heartBeatNum + " latency: " + (currentTimeMillis() - timestamp) + " ms ";
            lastMessageTime=(currentTimeMillis());
        }
        else
            Debug.LogError("Error. hbText is null");
        

    }
    private static readonly System.DateTime Jan1st1970 = new System.DateTime
    (1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);

    public static long currentTimeMillis()
    {
        return (long)(System.DateTime.UtcNow - Jan1st1970).TotalMilliseconds;
    }
   
    public void newFireEvent(ushort fireNum, long timestamp)
    {
        if (fireText != null)
        {
            //long currTime = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
            fireText.text = "Fired shot number: " + fireNum + " latency: " + (currentTimeMillis() - timestamp) + " ms ";
            lastMessageTime = (currentTimeMillis());
        }
        else
            Debug.LogError("Error. fireText is null");
    }
   
    public void newModeSwitchEvent(ushort mode, long timestamp)
    {
        if (modeSwitchText != null)
        {
            //long currTime = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
            modeSwitchText.text = "Modeswitch event : " + mode + " latency: " + (currentTimeMillis() - timestamp) + " ms ";
            lastMessageTime = (currentTimeMillis());
        }
        else
            Debug.LogError("Error. modeSwitchText is null");
    }
    
    public void newChargingHandleEvent(ushort chargingHandle, long timestamp)
    {
        if (chargingHandleText != null)
        {
            //long currTime = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
            chargingHandleText.text = "Charging handle event : " + chargingHandle + " latency: " + (currentTimeMillis() - timestamp) + " ms ";
            lastMessageTime = (currentTimeMillis());
        }
        else
            Debug.LogError("Error. chargingHandleText is null");
    }
    
    public void newForwardAssistEvent(ushort forwardAssist, long timestamp)
    {
        if (forwardAssistText != null)
        {
            //long currTime = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
            forwardAssistText.text = "forwardAssist event : " + forwardAssist + " latency: " + (currentTimeMillis() - timestamp) + " ms ";
            lastMessageTime = (currentTimeMillis());
        }
        else
            Debug.LogError("Error. forwardAssistText is null");
    }
    
    public void newPowerRemainingEvent(ushort powerRemaining, long timestamp)
    {
        if (hbText != null)
        {
            long currTime = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
            powerRemainingText.text = "PowerRemainingEvent remaining %: " + powerRemaining + " latency: " + (currentTimeMillis() - timestamp) + " ms ";
            lastMessageTime = (currentTimeMillis());
        }
        else
            Debug.LogError("Error. hbText is null");
    }
 
}
