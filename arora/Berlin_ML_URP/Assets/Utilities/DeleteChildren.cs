using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class DeleteChildren : MonoBehaviour
{
    public int count;
#if UNITY_EDITOR
    [ContextMenu("Delete children")]
    void DeleteChild()
    {
        for (int i = 0; i < count; i++)
        {
            GameObject child = gameObject.transform.GetChild(0).gameObject;
            if (child != null)
            {
                DestroyImmediate(child);
                Debug.Log("Destroyed child: " + i);
            }
        }
    }
#endif
}
