using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class AttributeClassEditor : MonoBehaviour
{
    [CustomEditor(typeof(AttributeClass), true)]
    public class CsvImporterEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            AttributeClass attributeClass = (AttributeClass)target;
            foreach (AttributeClass.Attribute a in attributeClass.attributes)
            {
                EditorGUILayout.BeginHorizontal();
                if(a.val == null || a.val == "")
                    EditorGUILayout.LabelField(a.key, EditorStyles.whiteBoldLabel);
                else
                    EditorGUILayout.LabelField(a.key);
                EditorGUILayout.TextField(a.val);
                EditorGUILayout.EndHorizontal();
            }
                
        }
    }
}
