using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class SkinsEnumerants
{
    public static ushort SystemHeartbeat = 0;
    public static ushort Fire = 1;
    public static ushort ModeSwitch = 2;
    public static ushort ChargingHandle = 3;
    public static ushort ForwardAssist = 4;
    public static ushort Magazine = 5;
    public static ushort MagazineID = 6;
    public static ushort PowerRemaining = 7;
    public static ushort AccelerometerThreshold = 11;
    public static ushort SetWeaponID = 21;
    public static ushort SetDebugMode = 22;
    public static ushort SetHeartbeatTime = 23;
    public static ushort RequestStateValues = 30;
    public static ushort ErrorState = 254; 

}

/**
 * This class generates a serial data event from the skins ICD from events. 
 * @author Dean Reed
 */
public class SkinsEventParser:MonoBehaviour  {
    bool debugParser = true; //set to true for addidtional debug output
    bool extraDebugParser = false; //set to true for addidtional debug output
    bool swapbytes=false; //if true we swap the endianness
    public bool inDebugMode = false; //if set to true, we are using ascii. else is binary 

    public delegate void NewHeartBeatEvent(ushort heartBeatNum, long timestamp);
    public static event NewHeartBeatEvent newHeartBeatEvent;

    public delegate void NewFireEvent(ushort fireNum, long timestamp);
    public static event NewFireEvent newFireEvent;

    public delegate void NewModeSwitchEvent(ushort mode, long timestampe);
    public static event NewModeSwitchEvent newModeSwitchEvent;

    public delegate void NewChargingHandleEvent(ushort chargingHandle, long timestamp);
    public static event NewChargingHandleEvent newChargingHandleEvent;

    public delegate void NewForwardAssistEvent(ushort forwardAssist, long timestamp);
    public static event NewForwardAssistEvent newForwardAssistEvent;

    public delegate void NewPowerRemainingEvent(ushort powerRemaining, long timestamp);
    public static event NewPowerRemainingEvent newPowerRemainingEvent;
    
    public SkinsEventParser()
    {
        SerialDataEventSource.newSerialData += mySerialListener; //this adds the mySerialListener callback to the event
    }

    void mySerialListener(SerialData m_event)
    {
        Debug.Log("SkinsEventParser() have data in listener");
        parseEvents(m_event.data, m_event.timestamp);
    }
    
    /**
     * PURPOSE: Provides a parsing from serial data to events. 
     * PRE: The serialData is in weapon ICD format 
     * 
     * ICD format
     * 	Start signal - 2 bytes
     * 	System ID - 2 bytes
     * 	Packet size - 2 bytes
     * 	Event - 4 bytes (2 byte event enumerant, 2 byte event value)
     * 	...
     * 	Event - 4 bytes (2 byte event enumerant, 2 byte event value)
     * 
     * If no proper start component is found this function will send a Debug.Log and return without doing anything with
     * the message. Otherwise it will send events for each event in the message.
     */
    public void parseEvents(byte[] serialData, long timestamp)
    {
        if (extraDebugParser)
            Debug.Log("SkinsEventParser.ParseEvents has serial data size: " + serialData.Length);

        if(serialData.Length<10)
        {
            Debug.LogError("serialData is less than minimim packet size, it is byte length of " + serialData.Length);
            return;
        }
			
        int startPos=0; //where in the packet do we start? 
        bool syncFound=false;
        while( !syncFound)
        {
            if ((serialData[startPos] == 0xFF) && (serialData[startPos + 1] == 0xFF))
            {
                syncFound = true;
                if(debugParser)
                    Debug.Log("Found sync packet! ");
            }
            else
            {
                startPos++; 

                if((startPos+2) < serialData.Length)
                {
                    //ok to proceed chencking for sync
                }
                else
                {
                    Debug.LogError("Error. Did not find sync packet! " );
                    return;
                }
            }
        }
			
        //we must have found sync here 
        ushort systemID = 0;
        systemID = get16bits(serialData[startPos + 2], serialData[startPos + 3]);
        if (extraDebugParser)
            Debug.Log("SystemID: " + systemID);
        

        ushort packetSize = 0xFFFF;



        packetSize = get16bits(serialData[startPos + 4], serialData[startPos + 5]);
        if (extraDebugParser)
            Debug.Log("Packet size: " + packetSize);
        int numEvents = packetSize / 4;
        if (extraDebugParser)
            Debug.Log("Num events: " + numEvents);
        for(int i=0; i< numEvents; i++)
        {
            int startEnumerantPos=(startPos+6)+(i*4);
            if (extraDebugParser)
                Debug.Log("StartEnumerant Pos : " + startEnumerantPos );
            ushort enumerant = get16bits(serialData[startEnumerantPos], serialData[startEnumerantPos+1]);
            ushort mvalue = get16bits(serialData[startEnumerantPos + 2], serialData[startEnumerantPos+3]);
            if (debugParser)
                Debug.Log("Serial Event number : " + (i+1)+ " enumerant: " + enumerant+" value "+mvalue );
            toEvent(enumerant, mvalue, timestamp);
        }
    }

    protected void toEvent(ushort enumerant, ushort value, long timestamp)
    {
        if (enumerant == SkinsEnumerants.SystemHeartbeat)
        {
            if (newHeartBeatEvent != null)
                newHeartBeatEvent(value,timestamp);
        }
        else if (enumerant == SkinsEnumerants.Fire)
        {
            if (newFireEvent != null)
                newFireEvent(value,timestamp);
        }
        else if (enumerant == SkinsEnumerants.ModeSwitch)
        {
            if (newModeSwitchEvent != null)
                newModeSwitchEvent(value, timestamp);
        }
        else if (enumerant == SkinsEnumerants.ChargingHandle)
        {
            if (newChargingHandleEvent != null)
                newChargingHandleEvent(value, timestamp);
        }
        else if (enumerant == SkinsEnumerants.ForwardAssist)
        {
            if (newForwardAssistEvent != null)
                newForwardAssistEvent(value, timestamp);
        }
        else if (enumerant == SkinsEnumerants.PowerRemaining)
        {
            if (newPowerRemainingEvent != null)
                newPowerRemainingEvent(value, timestamp);
        }
        else
            Debug.LogError("Error. Unknown event enumerant: " + enumerant + " value:" + value);

    }

    /**
     * PURPOSE: Get the 16 bit protocol value from bytes int a short int 
     */
    protected ushort get16bits(byte high, byte low)
    {
        if(swapbytes)
            return swap(high, low);

        if (extraDebugParser)
            Debug.Log("Get16Bits: high:" + high+ " low: "+low);
        ushort retVal = high;
        retVal = (ushort)(retVal << 8);
        retVal = (ushort)(retVal | low);

        if (extraDebugParser)
            Debug.Log("Return value:" + high + " low: " + low);
        return retVal;
    }

    protected ushort swap(byte high, byte low)
    {
        ushort retVal; //the return value
        retVal = low;
        retVal = (ushort) (retVal << 8); // need to validate this.. 
        retVal = (ushort) (retVal | high);
        return retVal; 
    }
}
