using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class getvert : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            //GameObject.GetComponent<MeshRenderer>()
            Mesh mesh = GetComponent<MeshFilter>().mesh;
            Vector3[] vertices = mesh.vertices;
            Debug.Log(vertices[0]);
        }
    }
}
