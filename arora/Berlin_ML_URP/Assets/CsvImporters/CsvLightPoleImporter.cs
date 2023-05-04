#if UNITY_EDITOR
using System.Collections;
using UnityEditor;
#endif
using UnityEngine;

public class CsvLightPoleImporter : CsvImporter
{
#if UNITY_EDITOR
    override protected IEnumerator CreateObject(AttributeClass.Attribute[] attributes)
    {
        string[] s = AttributeClass.GetValueForKeyFromAttributeArray("MODEL", attributes)?.Split(new char[] { '/', '.' });
        if (s == null || (s.Length == 1 && s[0] == ""))
        {
            Debug.LogError("MODEL value not found");
            yield break;
        }
        int rotOffset = 0;
        if (s[s.Length - 2] == "Object31" || s[s.Length - 2] == "Object22")
            rotOffset = 90;


        string assetPath = AttributeClass.GetValueForKeyFromAttributeArray("MODEL", attributes);
        if(assetPath[0] == '/')
            assetPath = assetPath.Substring(1);
        GameObject prefab = AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject)) as GameObject;
        if (prefab == null)
        {
            Debug.LogError("Prefab at " + assetPath + " not found");
            yield break;
        }
        GameObject copy = (GameObject)PrefabUtility.InstantiatePrefab(prefab, objectParent.transform);
        attributeClass = copy.AddComponent<AttributeClass>();
        attributeClass.attributes = attributes;
        SetObjectPositionAndScale(copy);
        copy.transform.rotation = Quaternion.Euler(0, float.Parse(attributeClass.GetValueForKey("BEARING")) + rotOffset, 0);
        if (objectParent.isStatic)
            copy.isStatic = true;
        yield return null;
    }
#endif
}
