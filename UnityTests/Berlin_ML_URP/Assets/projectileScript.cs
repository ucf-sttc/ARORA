using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class projectileScript : MonoBehaviour
{
    public GameObject laser, spawnedLaser;
    public Vector2 center;
    public terrainDeformScript tds;

    // Start is called before the first frame update
    void Start()
    {
        tds = GameObject.FindObjectOfType<terrainDeformScript>();
        spawnedLaser = Instantiate(laser, new Vector3(center.x, 200, center.y), Quaternion.identity);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnCollisionEnter(Collision coll)
    {
        GetComponent<CapsuleCollider>().enabled = false;
        GetComponent<Rigidbody>().velocity = Vector3.down * 200;
        Destroy(spawnedLaser);
        tds.generateCrater(new Vector2(center.y, center.x));
        Invoke("destroySelf", 5.0f);
    }

    void destroySelf()
    {
        Destroy(gameObject);
    }
}
