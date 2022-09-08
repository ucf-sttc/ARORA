﻿// Traffic Simulation
// https://github.com/mchrbn/unity-traffic-simulation

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class TrafficEditorInspector {
    //Whole Inspector layout
    public static void DrawInspector(TrafficSystem trafficSystem, out bool restructureSystem) {
        //-- Gizmo settings
        Header("Gizmo Config");
        Toggle("Show Path Gizmos", ref trafficSystem.showPathGuizmos);
        Toggle("Show Label Gizmos", ref trafficSystem.showLabelGuizmos);
            
        //Arrow config
        DrawArrowTypeSelection(trafficSystem);
        FloatField("Waypoint Size", ref trafficSystem.waypointSize);
        EditorGUILayout.Space();
            
        //-- System config
        Header("System Config");
        FloatField("Segment Detection Threshold", ref trafficSystem.segDetectThresh);
        EditorGUILayout.Space();

        //Helper
        //HelpBox("Ctrl + Left Click to create a new segment\nShift + Left Click to create a new waypoint.\nAlt + Left Click to create a new intersection");
        //HelpBox("Reminder: The cars will follow the point depending on the sequence you added them. (go to the 1st waypoint added, then to the second, etc.)");
        //EditorGUILayout.Space();

        //restructureSystem = Button("Re-Structure Traffic System");
        restructureSystem = false;
    }

    //-- Helper to draw the Inspector
    private static void Label(string label) {
        EditorGUILayout.LabelField(label);
    }

    private static void Header(string label) {
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
    }

    private static void Toggle(string label, ref bool toggle) {
        toggle = EditorGUILayout.Toggle(label, toggle);
    }

    private static void IntField(string label, ref int value) {
        value = EditorGUILayout.IntField(label, value);
    }
        
    private static void IntField(string label, ref int value, int min, int max) {
        value = Mathf.Clamp(EditorGUILayout.IntField(label, value), min, max);
    }
        
    private static void FloatField(string label, ref float value) {
        value = EditorGUILayout.FloatField(label, value);
    }

    private static void HelpBox(string content) {
        EditorGUILayout.HelpBox(content, MessageType.Info);
    }

    private static bool Button(string label) {
        return GUILayout.Button(label);
    }
        
    private static void DrawArrowTypeSelection(TrafficSystem trafficSystem) {
        trafficSystem.arrowDrawType = (ArrowDraw) EditorGUILayout.EnumPopup("Arrow Draw Type", trafficSystem.arrowDrawType);
        EditorGUI.indentLevel++;
            
        switch (trafficSystem.arrowDrawType) {
            case ArrowDraw.FixedCount:
                IntField("Count", ref trafficSystem.arrowCount, 1, int.MaxValue);
                break;
            case ArrowDraw.ByLength:
                FloatField("Distance Between Arrows", ref trafficSystem.arrowDistance);
                break;
            case ArrowDraw.Off:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
            
        if (trafficSystem.arrowDrawType != ArrowDraw.Off) {
            FloatField("Arrow Size Waypoint", ref trafficSystem.arrowSizeWaypoint);
            FloatField("Arrow Size Intersection", ref trafficSystem.arrowSizeIntersection);
        }
            
        EditorGUI.indentLevel--;
    }
}