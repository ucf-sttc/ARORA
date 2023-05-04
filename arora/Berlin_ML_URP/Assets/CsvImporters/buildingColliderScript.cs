using UnityEngine;

public class buildingColliderScript : MonoBehaviour
{
    public GameObject buildingParent;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    [ContextMenu("Generate Colliders for Buildings")]
    public void generateCollidersForBuildings()
    {
        if (buildingParent == null)
        {
            Debug.Log("please set the building parent variable in the inspector");
            return;
        }
        foreach(MeshFilter mf in buildingParent.GetComponentsInChildren<MeshFilter>(false))
        {
            MeshCollider mc = null;
            if (mf.gameObject.GetComponent<MeshCollider>() == null)
                mc = mf.gameObject.AddComponent<MeshCollider>();
            else
                mc = mf.gameObject.GetComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
        }
        Debug.Log("Builings now have colliders");

    }
}
