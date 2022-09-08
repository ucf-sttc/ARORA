using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ModifyTagsAndLayers : MonoBehaviour
{
    public string parentName;
    public bool updateTag;
    public string replacementTag;
    public bool updateLayer;
    public LayerMask replacementLayer;

    [ContextMenu("Perform Modifications")]
    void performModifications()
    {
        List<GameObject> parentObjects = FindObjectsOfType<GameObject>().Where(obj => obj.name == parentName).ToList();

        foreach(GameObject g in parentObjects)
        {
            Debug.Log(g.scene.name);
            for(int i = 0;i < g.transform.childCount; i++)
            {
                GameObject child = g.transform.GetChild(i).gameObject;
                if (updateTag)
                    child.tag = replacementTag;
                if (updateLayer)
                    child.layer = replacementLayer;
            }
        }
    }
}
