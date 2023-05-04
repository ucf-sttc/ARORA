using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class JavaInterfaceToSerial : MonoBehaviour {

    public  SerialDataEventSource mEventSource=new SerialDataEventSource();

    protected AndroidJavaObject serialMetaObject = null; //represents the java instance of the serial class 
	// Use this for initialization

	public bool queueOverload;

	void Start () {
        //create a meta-object to represent a java object
        AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer"); //get's the class of the player
        AndroidJavaObject jo = jc.GetStatic<AndroidJavaObject>("currentActivity"); // resolves the static context
        Debug.Log("Have current Activity context! " ); 
        object[] args = { jo }; //bundle the params
        serialMetaObject = new AndroidJavaObject("embedded.ist.ucf.edu.serialoverusblib.UnitySerialOverUSBDevice", args); //instance the serial class meta object
        // matches signiture void testVoid()
       // serialMetaObject.Call("testVoid");

        object[] args2 = { "Parm from Unity" };
	}
	
	// Update is called once per frame
	/*
	 * If the UnitySerialOverUSBDevice has transmissions (SerialDataTransmition.java) in its data queue dataQue hasData 
	 * will return true. If so it will call getData to get an AndroidJavaObject to get the oldest transmission from the 
	 * queue. It then stores the transmission data in bytes and the creation time in timeCreated before calling newData
	 * from mEventSource.
	 */
	void Update () 
	{
        if(serialMetaObject.Call<bool>("hasData"))
        {
            Debug.Log("hasData true!");
            AndroidJavaObject data = serialMetaObject.Call<AndroidJavaObject>("getData");
            long timeCreated = data.Call<long>("getTimeCreated");
            Debug.Log("Time created: "+timeCreated);
            byte[] bytes = data.Call<byte[]>("getBytes");
            string s = System.Text.Encoding.UTF8.GetString(bytes, 0, bytes.Length);
            Debug.Log("bytes as string:  " + s);
            if(mEventSource!=null)
            {
                mEventSource.newData(bytes, timeCreated);
            }
        }

		if (serialMetaObject.Call<bool> ("dataQueOverload"))
			queueOverload = true;
	}
}
