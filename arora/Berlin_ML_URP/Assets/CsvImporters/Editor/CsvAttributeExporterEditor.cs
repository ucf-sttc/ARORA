using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CsvAttributeExporterEditor : MonoBehaviour
{
    [CustomEditor(typeof(CsvAttributeExporter), true)]
    public class CsvImporterEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            CsvAttributeExporter exporter = (CsvAttributeExporter)target;
            base.OnInspectorGUI();
            if (GUILayout.Button("Export objects to CSV using data from AttributeClass files"))
                exporter.WriteCSV();
        }
    }
}
