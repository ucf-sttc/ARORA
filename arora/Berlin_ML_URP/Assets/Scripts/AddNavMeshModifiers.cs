#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;

public class AddNavMeshModifiers : MonoBehaviour
{
    /* Assumes GameObject hierarchy:
     * Root/Parent <- this script goes here
     * - FrictionType (i.e. Concrete, Grass, etc)
     * -- ObjectType
     * --- Object
     */
    [ContextMenu("Add NavMeshModifiers to Children")]
    void AddModifiers()
    {
        Undo.SetCurrentGroupName("Add NavMeshModifier components");
        int group = Undo.GetCurrentGroup();
        int count = 0;

        foreach(Transform frictionType in transform)
        {
            if (!frictionType.gameObject.activeInHierarchy) continue;

            string frictionName = frictionType.name;
            Debug.Log("Adding "+frictionName);
            foreach(Transform objectType in frictionType.transform)
            {
                // check if zone
                foreach (Transform surface in objectType.transform)
                {
                    NavMeshModifier mod = surface.GetComponent<NavMeshModifier>();
                    if (mod) DestroyImmediate(mod); // remove existing NavMeshModifiers

                    mod = Undo.AddComponent<NavMeshModifier>(surface.gameObject);
                    mod.overrideArea = true;
                    mod.area = NavMesh.GetAreaFromName(frictionName);
                    count++;
                }
            }
        }

        Undo.CollapseUndoOperations(group);

        Debug.LogFormat("Add NavMeshModifiers done ({0})", count);
    }

    [ContextMenu("Remove NavMeshModifiers")]
    void RemoveMods()
    {
        var mods = GetComponentsInChildren<NavMeshModifier>();
        int count = 0;
        foreach (NavMeshModifier mod in mods)
        {
            DestroyImmediate(mod);
            count++;
        }
        Debug.LogFormat("Remove NavMeshModifiers done ({0})", count);
    }
}
#endif