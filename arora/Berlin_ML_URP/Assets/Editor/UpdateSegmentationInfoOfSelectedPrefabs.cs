using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class UpdateSegmentationInfoOfSelectedPrefabs : MonoBehaviour
{
    public int outputMode;
    public GameObject[] prefabsToSetup;
    // Start is called before the first frame update
    void Start()
    {
        foreach (GameObject p in prefabsToSetup)
        {
            UpdatePropertiesOnPrefab(p);
        }
    }

    void UpdatePropertiesOnPrefab(GameObject assetRoot)
    {
#if UNITY_EDITOR
        // Get the Prefab Asset root GameObject and its asset path.
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

                //r.enabled = false;
                foreach (Material m in mats)
                {
                    var id = r.gameObject.GetInstanceID();
                    var layer = r.gameObject.layer;
                    var tag = r.gameObject.tag;

                    if (m.HasProperty("_ObjectSegmentationColor") &&
                        m.HasProperty("_TagSegmentationColor") &&
                        m.HasProperty("_LayerCategoryColor"))
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
#endif  
    }
}
