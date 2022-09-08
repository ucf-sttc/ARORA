using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionScanTest : MonoBehaviour
{
    public bool test;
    public List<float> variables;
    float range;
    AttributeClass ac;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(test)
        {
            test = false;
            Vector3 position = new Vector3(variables[0], variables[1], variables[2]);
            if (variables.Count > 3)
                range = variables[3];

            Collider[] colliders = Physics.OverlapSphere(position, range);

            foreach (Collider c in colliders)
            {
                ac = c.transform.GetComponentInParent<AttributeClass>();
                if (ac != null)
                {
                    Debug.Log(ac.ToString());
                    return;
                }
            }
        }
        
    }
}
