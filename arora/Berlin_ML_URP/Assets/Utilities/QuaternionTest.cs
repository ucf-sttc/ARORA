using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuaternionTest : MonoBehaviour
{
    
    // Start is called before the first frame update
    void Start()
    {
        Random.InitState(10);
    }

    // Update is called once per frame
    void Update()
    {
        float heading = Random.Range(0, 360) * Mathf.Deg2Rad;
        Quaternion q = new Quaternion(0, Mathf.Sin(heading/2), 0, Mathf.Cos(heading/2));
        Debug.Log(q.x + " " + q.y + " " + q.z + " " + q.w + " " + q.eulerAngles);
    }
}
