using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * This is an example reciever class. It shows how to register with the serial object. 
 */
public class ExampleReciever : MonoBehaviour {
    //public SerialDataEventSource eventSource; 

	// Use this for initialization
	void Start () {
    //    if (eventSource == null)
      //      Debug.LogError("ERROR. ExampleReciever is not set with an event source");
      //  else
       SerialDataEventSource.newSerialData += mySerialListener; //this adds the mySerialListener callback to the event
        //the event is static
	}

    void mySerialListener(SerialData m_event)
    {
        Debug.Log("Example.. have data in listener");
    }
	
	
}
