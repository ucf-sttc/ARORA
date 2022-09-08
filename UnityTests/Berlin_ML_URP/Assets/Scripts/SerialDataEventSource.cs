using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//The local equivalent of the transmissions from SerialDataTransmition.java
public class SerialData
{
    public byte[] data;
    public long timestamp; 

    public SerialData(byte[] data, long timestamp)
    {
        this.data = data;
        this.timestamp = timestamp;
    }
}

//Triggers a NewSerialData event.  Currently the event is only listened for by SkinsEventParser
public class SerialDataEventSource 
{
    public delegate void NewSerialData(SerialData m_event);
    public static event NewSerialData newSerialData;
    public JavaInterfaceToSerial javaInterfaceToSerial= null;
    protected int myInt = 0;

    public void newData(byte[] data, long timestamp)
    {
        SerialData ssdata = new SerialData(data, timestamp);

        if (newSerialData != null)
            newSerialData(ssdata);   
    }
}
