#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class AssignFrictionMeshes : MonoBehaviour
{
    [ContextMenu("Assign Friction Meshes")]
    void AssignMeshes()
    {
        // get MeshFilter components in children and assign matching meshes from folder
        MeshFilter[] meshObjects = GetComponentsInChildren<MeshFilter>();
        int count = 0;
        foreach(MeshFilter mf in meshObjects)
        {
            string pathname = "Assets/NavigationAreaMeshes/" + mf.name + ".asset";
            Debug.Log(pathname);
            Mesh mesh = (Mesh) AssetDatabase.LoadAssetAtPath(pathname, typeof(Mesh));
            if (mesh == null) continue;
            mf.mesh = mesh;
            count++;
        }

        Debug.Log("Assign meshes done ("+count+")");
    }
}
#endif