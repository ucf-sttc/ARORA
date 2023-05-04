using UnityEngine;
using UnityEditor;
using System;

//Clive: Sets up the segmentation properties of a prefab object that has shaders modified to have those properties
//This class is called from the Unity Editor by right clicking a prefab and should be run each time a change is made to the prefab.
public class SetupPrefabObjectSegmentation
{
    [MenuItem("Assets/SetupObjectSegmentation")]
    private static void SetupSegmentation()
    {
        foreach (UnityEngine.Object ob in Selection.objects)
        {
            Debug.Log(((GameObject)ob).name);
            // Get the Prefab Asset root GameObject and its asset path.
            GameObject assetRoot = ob as GameObject;
            string assetPath = AssetDatabase.GetAssetPath(assetRoot);

            // Modify prefab contents and save it back to the Prefab Asset
            using (var editScope = new EditPrefabAssetScope(assetPath))
            {
                foreach (Renderer r in editScope.prefabRoot.GetComponentsInChildren<Renderer>())
                {
                    Material[] mats;
                    if (r is BillboardRenderer)
                        mats = new Material[] { (r as BillboardRenderer).billboard.material };
                    else
                        mats = r.sharedMaterials;

                    foreach (Material m in mats)
                    {
                        var id = r.gameObject.GetInstanceID();
                        var layer = r.gameObject.layer;
                        var tag = r.gameObject.tag;

                        if (m.HasProperty("_ObjectSegmentationColor") &&
                            m.HasProperty("_TagSegmentationColor") &&
                            m.HasProperty("_LayerSegmentationColor"))
                        {
                            m.SetColor("_ObjectSegmentationColor", ColorEncoding.EncodeIDAsColor(id));
                            m.SetColor("_TagSegmentationColor", ColorEncoding.EncodeTagAsColor(tag));
                            m.SetColor("_LayerSegmentationColor", ColorEncoding.EncodeLayerAsColor(layer));
                        }
                        else
                        {
                            Debug.LogError(editScope.prefabRoot.name + " doesn't have a shader with the necessary fields");
                        }
                    }
                }
            }
        }
            
    }

    [MenuItem("Assets/SetupObjectSegmentation", true)]
    private static bool ObjectSegmentationOptionValidation()
    {
        // This returns true when the selected object is a GameObject (the menu item will be disabled otherwise).
        if (!Selection.activeObject) return false;
        return Selection.activeObject.GetType() == typeof(GameObject);
    }
}
