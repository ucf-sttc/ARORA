using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CsvImporter), true)]
public class CsvImporterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        CsvImporter importer = (CsvImporter)target;
        base.OnInspectorGUI();
        if (GUILayout.Button("Import objects using data from CSV file"))
            importer.StartReadCSV();
    }
}
