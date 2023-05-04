using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TrafficCarSpawner))]
[CanEditMultipleObjects]
public class TrafficCarSpawnerEditor : Editor
{
    private TrafficCarSpawner tcs;
    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        DrawTypeSelection(tcs);
        EditorGUILayout.Space();
        base.OnInspectorGUI();
        if (EditorGUI.EndChangeCheck())
            EditorUtility.SetDirty(target);
    }

    private void OnEnable()
    {
        tcs = target as TrafficCarSpawner;
    }

    private static void DrawTypeSelection(TrafficCarSpawner tcs)
    {
        tcs.trafficType = (TrafficCarSpawner.TrafficType)EditorGUILayout.EnumPopup("Traffic Type", tcs.trafficType);

        switch (tcs.trafficType)
        {
            case TrafficCarSpawner.TrafficType.Kinematic:
                break;
            case TrafficCarSpawner.TrafficType.Physics:
                tcs.obstacleDetection = EditorGUILayout.Toggle("Obstacle Detection", tcs.obstacleDetection);
                break;
            default:
                break;
        }
    }
}
