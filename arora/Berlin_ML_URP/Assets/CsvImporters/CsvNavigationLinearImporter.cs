using System;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
using Unity.EditorCoroutines.Editor;
#endif
using UnityEngine;
using Utils;

//V1.0.0
public class CsvNavigationLinearImporter : CsvImporter
{
#if UNITY_EDITOR
    public bool saveMeshes, destroyLineRenderers, showDebugPoints, noUserInput, separateUndergroundPaths;
    public enum splitModes
    {
        KeepOriginal,
        KeepNew,
        FlagForLater,
    }
    public splitModes noUserInputMode;

    Vector2[] vertices_2D;
    Camera c;
    bool createdCamera;
    GameObject newMeshObject;
    override protected IEnumerator CreateObject(AttributeClass.Attribute[] attributes)
    {
        if(Camera.main == null)
        {
            c = gameObject.AddComponent<Camera>();
            c.tag = "MainCamera";
            createdCamera = true;
        }

        //Create gameObject for area and add MeshFilter and AttributeClass
        newMeshObject = new GameObject("Object " + entryNumber);
        attributeClass = newMeshObject.AddComponent<AttributeClass>();
        attributeClass.attributes = attributes;
        MeshFilter meshFilter = newMeshObject.AddComponent<MeshFilter>();

        LineRenderer lineRenderer = newMeshObject.AddComponent<LineRenderer>();
        lineRenderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        lineRenderer.alignment = LineAlignment.TransformZ;
        newMeshObject.transform.rotation = Quaternion.Euler(90, 0, 0);//Rotate containing object so that the line faces up

        //Put the newMeshObject in the appropriate category
        SetParent(newMeshObject, attributeClass);

        //Gets value, removes opening tags and checks for null
        string geometryValue = attributeClass.GetValueForKey("GEOMETRY")?.Replace("LINESTRING(", "").Replace("\"", "");
        if (geometryValue == null)
        {
            Debug.LogError("Couldn't find GEOMETRY key value.");
            CleanupState();
            yield break;
        }

        //remove parentheses and extra spaces
        geometryValue = geometryValue.Replace("( ", "").Replace(")", "").Replace(", ", ",");

        string[] vertices_String, vertex_String;

        vertices_String = geometryValue.Split(',');//Splits entry into xy pairs representing points
        vertices_2D = new Vector2[vertices_String.Length];
        Vector3[] vertices = new Vector3[vertices_String.Length];

        //Parse geometry entry into vector arrays.
        int i;
        for (i = 0; i < vertices.Length; i++)
        {
            vertex_String = vertices_String[i].Split(' ');
            vertices_2D[i] = new Vector2((float)(double.Parse(vertex_String[0]) - X_offset), (float)(double.Parse(vertex_String[1]) - Y_offset));
            vertices[i] = new Vector3(vertices_2D[i][0], 100, vertices_2D[i][1]);
            /*
            if (TerrainUtils.getNearestTerrain(vertices[i]) != null)
                vertices[i].y = TerrainUtils.getTerrainHeight(vertices[i]) + 0.05f;
            */
            if (Physics.Raycast(vertices[i], Vector3.down, out RaycastHit hit, float.MaxValue))
                vertices[i].y = hit.point.y;
            else
                vertices[i].y = 0;

            if (showDebugPoints)
            {
                GameObject testPoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                testPoint.name = "TestPoint: " + i;
                testPoint.transform.parent = newMeshObject.transform;
                testPoint.transform.localScale = Vector3.one * .1f;
                testPoint.transform.position = vertices[i];
            }
        }

        if (generateMeshFromLineRenderers(vertices, lineRenderer, meshFilter))
        {
            MeshRenderer meshRenderer = newMeshObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));

            if (saveMeshes)
            {
                SaveMesh(meshFilter.sharedMesh, newMeshObject.name, true, true);
            }
        }
        CleanupState();

        string onewayValue = attributeClass.GetValueForKey("oneway");
        string surfType = attributeClass.GetValueForKey("Surf_Type");
        if (surfType.Equals("Road") && onewayValue != "yes")
        {   
            yield return EditorCoroutineUtility.StartCoroutine(SplitPath(attributes), this);
        }
            
    }

    private bool generateMeshFromLineRenderers(Vector3[] vertices, LineRenderer lineRenderer, MeshFilter meshFilter)
    {
        try
        {
            Mesh mesh = new Mesh();
            mesh.Clear();

            //Convert points for y forward z down to counteract the rotation applied to make the lineRenderer lines face upward. Zero the height to simplify mesh creation
            Vector3[] convertedVertices = (Vector3[])vertices.Clone();
            for (int i = 0; i < convertedVertices.Length; i++)
            {
                convertedVertices[i].y = convertedVertices[i].z;
                convertedVertices[i].z = 0;
            }

            lineRenderer.positionCount = convertedVertices.Length;
            lineRenderer.SetPositions(convertedVertices);
            float width = float.Parse(attributeClass.GetValueForKey("width"));
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width;
            lineRenderer.BakeMesh(mesh);
            lineRenderer.SetPositions(vertices);

            //Set mesh points height equal to the terrain. 
            Vector3[] meshPoints = mesh.vertices;
            for (int j = 0; j < meshPoints.Length; j++)
            {
                //Terrain utils needs points converted back to z forward to calculate height correctly. This height value is then assigned to the negative z axis
                Vector3 convertedPoint = new Vector3(meshPoints[j].x, 100, meshPoints[j].y);

                RaycastHit hit;
                if (Physics.Raycast(convertedPoint, Vector3.down, out hit, float.MaxValue))
                    meshPoints[j].z = -hit.point.y;
                else if (Physics.Raycast(convertedPoint, Vector3.up, out hit, float.MaxValue))
                    meshPoints[j].z = -hit.point.y;
                else if (TerrainUtils.getNearestTerrain(convertedPoint) != null)
                    meshPoints[j].z = -(TerrainUtils.getTerrainHeight(convertedPoint) + 0.05f);
            }
            mesh.vertices = meshPoints;
            
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            meshFilter.mesh = mesh;

            lineRenderer.enabled = false;
            return true;
        }
        catch(Exception e)
        {
            string linePoints = "";
            for(int i = 0; i<lineRenderer.positionCount;i++)
            {
                Vector3 point = lineRenderer.GetPosition(i);
                linePoints += point.x + " " + point.y + " " + point.z + ",";
            }

            Debug.LogError("Failed to properly generate mesh for Entry: " + entryNumber + ". Line points are " + linePoints + ". Error:" + e);
            return false;
        }
    }

    //Checks an existing array for points that are too close together
    Vector2[] SpreadPoints(Vector2[] vertices_2D, float spreadDistance)
    {
        for (int currentIndex = 1; currentIndex < vertices_2D.Length - 1; currentIndex++)
            vertices_2D[currentIndex] = SpreadPoint(vertices_2D, vertices_2D[currentIndex], currentIndex, spreadDistance);
        return vertices_2D;
    }
    //Checks if a new point is to close to an existing point in the array and if so modifies it
    Vector2 SpreadPoint(Vector2[] vertices_2D, Vector2 vertex, int currentIndex, float spreadDistance)
    {
        for (int problemIndex = 0; problemIndex < currentIndex; problemIndex++)
        {
            if (vertex == vertices_2D[problemIndex])
            {
                if (problemIndex == currentIndex - 1)//The last point already in the array is the one that matches
                {
                    if (currentIndex > 1) //If there is a previous point that's doesn't have the same position as the current point use it to determine the new offset
                        vertex += (vertex - vertices_2D[currentIndex - 2]).normalized * spreadDistance;
                    else
                        vertex += Vector2.one * spreadDistance;
                }
                else //A point other than the last one entered into the array is the one that matches
                    vertex += (vertex - vertices_2D[currentIndex - 1]).normalized * spreadDistance;
            }
            else if ((vertex - vertices_2D[problemIndex]).magnitude < spreadDistance)
                vertex += (vertex - vertices_2D[currentIndex - 1]).normalized * spreadDistance;
        }
        return vertex;
    }

    IEnumerator SplitPath(AttributeClass.Attribute[] attributes)
    {
        GameObject originalMeshObject = newMeshObject;
        AttributeClass.Attribute[] 
            attributesA = (AttributeClass.Attribute[])attributes.Clone(),
            attributesB = (AttributeClass.Attribute[])attributes.Clone();

        //Modify attributes for two lanes
        //Set the path width of the new paths to half of the original path's width and set them to oneway
        float width = float.Parse(attributeClass.GetValueForKey("width"));
        AttributeClass.SetValueForKeyFromAttributeArray("width", (width / 2).ToString(), attributesA);
        AttributeClass.SetValueForKeyFromAttributeArray("width", (width / 2).ToString(), attributesB);
        AttributeClass.SetValueForKeyFromAttributeArray("oneway", "yes", attributesA);
        AttributeClass.SetValueForKeyFromAttributeArray("oneway", "yes", attributesB);

        //Setup the vertices for the new paths
        Vector2[]
            verticesA_2D = new Vector2[vertices_2D.Length],
            verticesB_2D = new Vector2[vertices_2D.Length];
        for (int i = 0;i<vertices_2D.Length;i++)
        {
            //Get the direction of the path
            Vector2 direction = Vector2.zero;
            if (i - 1 > -1)
                direction += vertices_2D[i] - vertices_2D[i - 1];
            if(i+1 < vertices_2D.Length)
                direction += vertices_2D[i+1] - vertices_2D[i];

            //Get a mormalized vector that is perpendicular to the path and set it's magnitude to 1/4 the width of the original path
            Vector2 perpendicularToPath = Vector2.Perpendicular(direction).normalized;
            perpendicularToPath *= width/4;

            //Set the points of the new lanes by offsetting the original points by the perpendicular path and adding the area offset
            Vector2 areaOffset = new Vector2((float)X_offset, (float)Y_offset);
            verticesA_2D[i] = vertices_2D[i] + perpendicularToPath + areaOffset;
            verticesB_2D[i] = vertices_2D[i] - perpendicularToPath + areaOffset;
        }

        //Convert vector2 arrays to strings and update the attribute arrays
        string 
            geometryStringA = "\"LINESTRING(", 
            geometryStringB = "\"LINESTRING(";
        for (int i = 0; i < vertices_2D.Length; i++)
        {
            geometryStringA += verticesA_2D[i].x + " " + verticesA_2D[i].y;
            geometryStringB += verticesB_2D[i].x + " " + verticesB_2D[i].y;
            if (i < vertices_2D.Length - 1)
            {
                geometryStringA += ", ";
                geometryStringB += ", ";
            }
                
        }
        geometryStringA += ")\"";
        geometryStringB += ")\"";
        AttributeClass.SetValueForKeyFromAttributeArray("GEOMETRY", geometryStringA, attributesA);
        AttributeClass.SetValueForKeyFromAttributeArray("GEOMETRY", geometryStringB, attributesB);


        //Create linear objects for each path
        yield return EditorCoroutineUtility.StartCoroutine(CreateObject(attributesA), this);
        GameObject attributesA_MeshObject = newMeshObject;
        attributesA_MeshObject.name += "-Generated A";
        yield return EditorCoroutineUtility.StartCoroutine(CreateObject(attributesB), this);
        GameObject attributesB_MeshObject = newMeshObject;
        attributesB_MeshObject.name += "-Generated B";

        int result;
        if (noUserInput)
            result = (int)noUserInputMode;
        else
        {
            //Position camera over linear object
            Selection.activeGameObject = originalMeshObject;
            SceneView.FrameLastActiveSceneView();
            SceneView.RepaintAll();
            yield return new EditorWaitForSeconds(1);
            //Decide whether or not to keep the newly generated lines. If they are not kept the original line will be generated
            result = EditorUtility.DisplayDialogComplex("Supervised two-way path conversion", "Keep separated paths?", "Yes", "No", "Flag for later");
        }
            
        switch(result)
		{
            case 0://Keeping only the original line
                DestroyImmediate(attributesA_MeshObject);
                DestroyImmediate(attributesB_MeshObject);
                break;
            case 1://Keeping only the new lines
                DestroyImmediate(originalMeshObject);
                break;
            case 2:
                GameObject container = new GameObject(originalMeshObject.name + " (flagged container)");
                container.transform.parent = originalMeshObject.transform.parent;
                originalMeshObject.transform.parent = container.transform;
                originalMeshObject.name += "-Original";
                attributesA_MeshObject.transform.parent = container.transform;
                attributesB_MeshObject.transform.parent = container.transform;
                break;
        }
    }
    
    protected override void OnFileEnd()
    {
        for (int i = 0; i < objectParent.transform.childCount; i++)
        {
            GameObject container = objectParent.transform.GetChild(i).gameObject;
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            var hash = container.name.GetHashCode();
            var a = (byte)255;
            var r = (byte)(hash >> 16);
            var g = (byte)(hash >> 8);
            var b = (byte)(hash);

            mpb.SetColor("_BaseColor", new Color32(r, g, b, a));
            foreach (Renderer renderer in container.GetComponentsInChildren<Renderer>())
            {
                renderer.SetPropertyBlock(mpb);
            }
        }
        base.OnFileEnd();
    }

    void SetParent(GameObject targetObject, AttributeClass attributeClass)
    {
        string location = attributeClass.GetValueForKey("location");
        string tunnel = attributeClass.GetValueForKey("tunnel");
        string surfType = attributeClass.GetValueForKey("Surf_Type");

        if(separateUndergroundPaths && (location == "underground" || tunnel != null))
        {
            for (int i = 0; i < objectParent.transform.childCount; i++)
            {
                if (objectParent.transform.GetChild(i).name.Equals("underground"))
                {
                    targetObject.transform.parent = objectParent.transform.GetChild(i);
                    return;
                }
            }
            GameObject newCategory = new GameObject("underground");
            newCategory.transform.parent = objectParent.transform;
            targetObject.transform.parent = newCategory.transform;
        }
        else if (surfType != null)
        {
            for (int i = 0; i < objectParent.transform.childCount; i++)
            {
                if (objectParent.transform.GetChild(i).name.Equals(surfType))
                {
                    targetObject.transform.parent = objectParent.transform.GetChild(i);
                    return;
                }
            }
            GameObject newCategory = new GameObject(surfType);
            newCategory.transform.parent = objectParent.transform;
            targetObject.transform.parent = newCategory.transform;
        }
        else
            targetObject.transform.parent = objectParent.transform;
    }

    public static void SaveMesh(Mesh mesh, string name, bool makeNewInstance, bool optimizeMesh)
    {
        if(!AssetDatabase.IsValidFolder("Assets/NavigationAreaMeshes"))
            AssetDatabase.CreateFolder("Assets", "NavigationAreaMeshes");
        string path = "Assets/NavigationAreaMeshes/" + name + ".asset";
        //path = EditorUtility.SaveFilePanel("Save Separate Mesh Asset", "Assets/", name, "asset");
        //path = FileUtil.GetProjectRelativePath(path);

        Mesh meshToSave = (makeNewInstance) ? Instantiate(mesh) as Mesh : mesh;

        if (optimizeMesh)
            MeshUtility.Optimize(meshToSave);
        try
        {
            AssetDatabase.CreateAsset(meshToSave, path);
            AssetDatabase.SaveAssets();
            
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to save mesh to " + path + ". Error:" + e);
        }
    }

    void CleanupState()
    {
        if (destroyLineRenderers)
            DestroyImmediate(newMeshObject.GetComponent<LineRenderer>());
        if (c != null && createdCamera)
            DestroyImmediate(c);
    }
#endif
}
