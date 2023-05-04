using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public static class SurfaceFrictionValues
{
    enum FrictionType {
        Grass, Concrete, Sandy, Water, HeavyBrush, Road
    };
    static float[] frictionValues = { 0.5f, 1f, 0.2f, 0, 1.5f, 1f }; // Grass, Concrete, Sandy, Water, HeavyBrush, Road
    public static float GetFrictionFromArea(int areamask)
    {
        for (int i = 20; i < 26; i++)
        {
            if ((1 << i) == areamask)
            {
                //Debug.LogFormat("Found navmesh: {0}", i);
                return frictionValues[i - 20];
            }
        }
        Debug.LogWarning("Unexpected navmesh surface area");
        return 1f;
    }
    public static int AllFrictionMask()
    {
        int frictionMask = 0;
        foreach(FrictionType f in Enum.GetValues(typeof(FrictionType)))
            frictionMask += 1 << NavMesh.GetAreaFromName(f.ToString());

        return frictionMask;
    }

    public static void TestFriction(GameObject a)
    {
        NavMeshQueryFilter queryFilter = new NavMeshQueryFilter() {
            agentTypeID = 0, // humanoid agent ID
            areaMask = SurfaceFrictionValues.AllFrictionMask()
        };
        NavMesh.SamplePosition(a.transform.position, out NavMeshHit hit, 0.5f, queryFilter);
        Debug.LogFormat("Friction: {0}", GetFrictionFromArea(hit.mask));
    }
}