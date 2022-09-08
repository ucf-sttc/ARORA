using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utils;
#if UNITY_EDITOR
using Unity.EditorCoroutines.Editor;
#endif

public class CsvImporter : MonoBehaviour
{
#if UNITY_EDITOR
    public double X_offset = 390187.070616464F;
    public double Y_offset = 5818754.16470284F;
    [Tooltip("The object that will contain the newly created objects")]
    public GameObject objectParent;
    protected AttributeClass attributeClass;
    [HideInInspector]
    public int entryNumber;

    [Tooltip("The file that contains the objects to be spawned in this scene")]
    public TextAsset sceneDescriptor;

    [ContextMenu("Import objects using data from CSV file")]
    public void StartReadCSV()
    {
        EditorCoroutineUtility.StartCoroutine(ReadCSV(), this);
    }
    public IEnumerator ReadCSV()
    {
        string[] entries = sceneDescriptor.text.Split('\n');
        int i, j;
        //Remove all nonprintable characters
        for(i=0; i<entries.Length;i++)
            foreach (char c in entries[i])
                if (c < 32)
                    entries[i]=entries[i].Replace(""+c, "");
        string[] labels = entries[0].Split(',');

        //Handle each entry
        for(entryNumber = 0;entryNumber<entries.Length;entryNumber++)
        {
            UnityEditor.EditorUtility.DisplayProgressBar("Importing objects from CSV", "Reading line " + entryNumber + " of " + entries.Length, entryNumber / entries.Length);
            string s = entries[entryNumber];
            i=0;
            Debug.Log(s);

            if (entryNumber > 0 && !s.Equals(""))
            {
                string[] values;

                //This section is necessary for the CsvNavigationAreaImporter
                values = s.Split('"');
                if (values.Length > 1 && values.Length %2 != 0) //If entry had parentheses and those parentheses came in pairs add the sections between parentheses as one value
                {
                    List<string> valuesIntermediate = new List<string>();
                    valuesIntermediate.AddRange(values[0].Split(','));
                    while(i+1 < values.Length)
                    {
                        string[] valuesAfterParentheses = values[i + 2].Split(',');
                        valuesIntermediate[valuesIntermediate.Count - 1] = valuesIntermediate[valuesIntermediate.Count - 1] +"\""+ values[i + 1] + "\"" + valuesAfterParentheses[0];
                        for(j = 1;j<valuesAfterParentheses.Length;j++)
                            valuesIntermediate.Add(valuesAfterParentheses[j]);
                        i += 2;
                    }
                    values = valuesIntermediate.ToArray();
                }
                else
                    values = s.Split(',');

                if (values.Length == labels.Length)
                {
                    AttributeClass.Attribute[] entryAttributes = new AttributeClass.Attribute[labels.Length];
                    for (i = 0; i < entryAttributes.Length; i++)
                    {
                        entryAttributes[i] = new AttributeClass.Attribute(labels[i], values[i]);
                    }
                    yield return EditorCoroutineUtility.StartCoroutine(CreateObject(entryAttributes), this);
                }
                else
                    Debug.LogError("Entry " + s + " has " + values.Length + " fields and is expected to have " + labels.Length);
            }
        }
        UnityEditor.EditorUtility.DisplayProgressBar("Importing objects from CSV", "Performing cleanup", entryNumber / entries.Length);
        OnFileEnd();
    }
    protected virtual IEnumerator CreateObject(AttributeClass.Attribute[] attributes)
    {
        Debug.LogError("CreateObject function not implemented");
        GameObject newGameObject = new GameObject();
        newGameObject.transform.parent =objectParent.transform;
        AttributeClass addedAttributeClass = newGameObject.AddComponent<AttributeClass>();
        addedAttributeClass.attributes = attributes;
        yield return null;
    }

    protected virtual void OnFileEnd() 
    {
        UnityEditor.EditorUtility.ClearProgressBar();
    }

    protected virtual void SetObjectPositionAndScale(GameObject target)
    {
        RaycastHit hit;
        Vector3 position = new Vector3(((float)(double.Parse(attributeClass.GetValueForKey("X")) - X_offset)), 100, ((float)(double.Parse(attributeClass.GetValueForKey("Y")) - Y_offset)));
        if (TerrainUtils.getNearestTerrain(position) != null)
            position.y = TerrainUtils.getTerrainHeight(position);
        else if (Physics.Raycast(target.transform.position, Vector3.down, out hit, float.MaxValue))
            position.y = hit.point.y;
        else if(Physics.Raycast(target.transform.position, Vector3.up, out hit, float.MaxValue))
            position.y = hit.point.y;
        target.transform.position = position;

        //Setup object scale if attribute class has scale values
        string
            scaleX = attributeClass.GetValueForKey("SCALE_X"), 
            scaleY = attributeClass.GetValueForKey("SCALE_Y"), 
            scaleZ = attributeClass.GetValueForKey("SCALE_Z");
        if(scaleX != null && scaleY != null && scaleZ != null)
            target.transform.localScale = new Vector3(float.Parse(scaleX), float.Parse(scaleY), float.Parse(scaleZ));
        
    }
#endif
}
