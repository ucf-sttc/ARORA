using System.IO;
using System.Text;
using UnityEngine;

public class CsvAttributeExporter : MonoBehaviour
{
#if UNITY_EDITOR
    public double xoffset, yoffset;
    public bool updateXFromGameObjectPositionX, updateYFromGameObjectPositionZ, updateScaleXFromGameObjectScale, updateScaleYFromGameObjectScale, updateScaleZFromGameObjectScale, updateBearingFromGameObjectYRotation;
    [Tooltip("This will be the name of the output .csv file. Just enter the name, no path or extension.")]
    public string fileName;
    [Tooltip("This is the object that contains the objects whose attribute data you wish to include in the file. Labels will be generated from first AttributeClass found.")]
    public GameObject objectParent;
    AttributeClass[] attributeClasses;
    string[][] output;
    
    [ContextMenu("Export objects attribute data to CSV file")]
    public void WriteCSV()
    {
        CollectData();
        WriteToFile();
    }

    void CollectData()
    {
        int i, j;
        string attributeValueString;
        double attributeValue;
        float gameObjectValue;
        attributeClasses = objectParent.GetComponentsInChildren<AttributeClass>();

        if (attributeClasses.Length == 0)
        {
            Debug.LogError("No AttributeClass files found in the children of objectParent");
            return;
        }
        //Make output array size equal to the number of AttributeClass files +1 for the labels line and the inner arrays equal to the numberOfAttributes
        int numberOfAttributes = attributeClasses[0].attributes.Length;
        output = new string[attributeClasses.Length + 1][];
        for (j = 0; j < output.Length; j++)
            output[j] = new string[numberOfAttributes];

        for (i = 0; i < numberOfAttributes; i++) //For each attribute in the first AttributeClass
        {
            //Get the label data
            output[0][i] = attributeClasses[0].attributes[i].key;

            //Collect values from each AttributeClass
            for (j = 1; j < output.Length; j++)
            {
                switch (output[0][i])
                {
                    case "X": //Update value if lower precision value used in Unity doesn't match the downscaled Attribute value. If these values are not the same it means the gameobject has been moved in the scene
                        attributeValueString = attributeClasses[j - 1].GetValueForKey("X");
                        attributeValue = double.Parse(attributeValueString);
                        gameObjectValue = attributeClasses[j - 1].gameObject.transform.position.x;
                        if (Mathf.Abs((float)(attributeValue - xoffset) - gameObjectValue) > 0.0001 && updateXFromGameObjectPositionX)
                            output[j][i] = ((double)gameObjectValue + xoffset).ToString();
                        else
                            output[j][i] = attributeValueString;
                        break;
                    case "Y": //Update value if lower precision value used in Unity doesn't match the downscaled Attribute value. If these values are not the same it means the gameobject has been moved in the scene
                        attributeValueString = attributeClasses[j - 1].GetValueForKey("Y");
                        attributeValue = double.Parse(attributeValueString);
                        gameObjectValue = attributeClasses[j - 1].gameObject.transform.position.z;
                        if (Mathf.Abs((float)(attributeValue - yoffset) - gameObjectValue) > 0.0001 && updateYFromGameObjectPositionZ)
                            output[j][i] = ((double)gameObjectValue + yoffset).ToString();
                        else
                            output[j][i] = attributeValueString;
                        break;
                    case "SCALE_X":
                        attributeValueString = attributeClasses[j - 1].GetValueForKey("SCALE_X");
                        attributeValue = double.Parse(attributeValueString);
                        gameObjectValue = attributeClasses[j - 1].gameObject.transform.localScale.x;
                        if (Mathf.Abs((float)attributeValue - gameObjectValue) > 0.0001 && updateScaleXFromGameObjectScale)
                            output[j][i] = (gameObjectValue).ToString();
                        else
                            output[j][i] = attributeValueString;
                        break;
                    case "SCALE_Y":
                        attributeValueString = attributeClasses[j - 1].GetValueForKey("SCALE_Y");
                        attributeValue = double.Parse(attributeValueString); gameObjectValue = attributeClasses[j - 1].gameObject.transform.localScale.y;
                        if (Mathf.Abs((float)attributeValue - gameObjectValue) > 0.0001 && updateScaleYFromGameObjectScale)
                            output[j][i] = (gameObjectValue).ToString();
                        else
                            output[j][i] = attributeValueString;
                        break;
                    case "SCALE_Z":
                        attributeValueString = attributeClasses[j - 1].GetValueForKey("SCALE_Z");
                        attributeValue = double.Parse(attributeValueString); gameObjectValue = attributeClasses[j - 1].gameObject.transform.localScale.z;
                        if (Mathf.Abs((float)attributeValue - gameObjectValue) > 0.0001 && updateScaleZFromGameObjectScale)
                            output[j][i] = (gameObjectValue).ToString();
                        else
                            output[j][i] = attributeValueString;
                        break;
                    case "BEARING":
                        attributeValueString = attributeClasses[j - 1].GetValueForKey("BEARING");
                        attributeValue = double.Parse(attributeValueString); gameObjectValue = attributeClasses[j - 1].gameObject.transform.rotation.eulerAngles.y;
                        if (attributeValue < 0)
                            gameObjectValue -= 360; //Accounts for the fact that rotations are stored in Unity as 0-360 and in the CSV as -180 to 180
                        if (Mathf.Abs((float)attributeValue - gameObjectValue) > 0.0001 && updateBearingFromGameObjectYRotation)
                            output[j][i] = (gameObjectValue).ToString();
                        else
                            output[j][i] = attributeValueString;
                        break;
                    default:
                        output[j][i] = attributeClasses[j - 1].GetValueForKey(output[0][i]);
                        break;
                }
            }
        }
    }
    void WriteToFile()
    {
        int length = output.Length;
        string delimiter = ",";

        StringBuilder sb = new StringBuilder();

        for (int index = 0; index < length; index++)
            sb.AppendLine(string.Join(delimiter, output[index]));

        string filePath = Application.dataPath + "/" + fileName + ".csv";

        StreamWriter outStream = System.IO.File.CreateText(filePath);
        outStream.WriteLine(sb);
        outStream.Close();
        Debug.Log("Exported csv to " + Application.dataPath + "/" + fileName + ".csv");
    }
#endif
}
