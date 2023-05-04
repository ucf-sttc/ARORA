#if UNITY_EDITOR
using System.Collections;
using UnityEditor;
#endif
using UnityEngine;

public class CsvVehicleImporter : CsvImporter
{
#if UNITY_EDITOR
    override protected IEnumerator CreateObject(AttributeClass.Attribute[] attributes)
    {
        int rotOffset = 0;

        string[] s = AttributeClass.GetValueForKeyFromAttributeArray("MODEL_NAME", attributes)?.Split(new char[] { '/', '.' });
        if (s == null)
        {
            Debug.LogError("MODEL_NAME value not found");
            yield break;
        }

        string assetPath = AttributeClass.GetValueForKeyFromAttributeArray("MODEL_PATH", attributes) + AttributeClass.GetValueForKeyFromAttributeArray("MODEL_NAME", attributes);
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
        float bearing = 0, pitch = 0, roll = 0;
        float.TryParse(attributeClass.GetValueForKey("BEARING"), out bearing);
        float.TryParse(attributeClass.GetValueForKey("PITCH"), out pitch);
        float.TryParse(attributeClass.GetValueForKey("ROLL"), out roll);

        copy.transform.rotation = Quaternion.Euler(pitch, bearing + rotOffset, roll);
        if (objectParent.isStatic)
            copy.isStatic = true;
        yield return null;
    }
#endif
}
