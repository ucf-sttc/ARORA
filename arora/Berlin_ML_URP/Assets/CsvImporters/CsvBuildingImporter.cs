#if UNITY_EDITOR
using System.Collections;
using UnityEditor;
#endif
using UnityEngine;

public class CsvBuildingImporter : CsvImporter
{
#if UNITY_EDITOR
    public string pathPrefixForPrefabs = "Assets/TPIM_with_Po2_Normals_On_Crop_off/Tiles/0/0/";
    override protected IEnumerator CreateObject(AttributeClass.Attribute[] attributes)
    {
        string s = AttributeClass.GetValueForKeyFromAttributeArray("href", attributes);
        if (s == null)
        {
            Debug.LogError("href value not found");
            yield break;
        }
        

        string assetPath = pathPrefixForPrefabs + s;
        GameObject prefab = AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject)) as GameObject;
        if(prefab == null)
        {
            Debug.LogError("Prefab at " + assetPath + " not found");
            yield break;
        }

        GameObject copy = (GameObject)PrefabUtility.InstantiatePrefab(prefab, objectParent.transform);
        attributeClass = copy.AddComponent<AttributeClass>();
        attributeClass.attributes = attributes;
        SetObjectPositionAndScale(copy);
        copy.transform.rotation = Quaternion.Euler(0, float.Parse(attributeClass.GetValueForKey("BEARING")), 0);
        copy.transform.Rotate(-90, 180, 0); //Corrects building orientation
        if (objectParent.isStatic)
            copy.isStatic = true;
        yield return null;
    }
#endif
}
