using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ExtractMaterials : MonoBehaviour
{
    [MenuItem("Assets/Extract Materials")]
    private static void SetupSegmentation()
    {
        foreach (UnityEngine.Object ob in Selection.objects)
        {
            Debug.Log(((GameObject)ob).name);
            ModelImporter m = (ModelImporter.GetAtPath(AssetDatabase.GetAssetPath(ob)) as ModelImporter);
            if(m.materialLocation != ModelImporterMaterialLocation.External)
            {
                m.materialLocation = ModelImporterMaterialLocation.External;
                m.SaveAndReimport();
            }
            
        }
        AssetDatabase.SaveAssets();
    }
}
