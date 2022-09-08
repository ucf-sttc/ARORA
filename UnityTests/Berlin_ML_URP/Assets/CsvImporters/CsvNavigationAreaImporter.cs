using System;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using Utils;

public class CsvNavigationAreaImporter : CsvImporter
{
#if UNITY_EDITOR
    public bool saveMeshes, createRenderers, showDebugPoints;
    string geometryValue;
    override protected IEnumerator CreateObject(AttributeClass.Attribute[] attributes)
    {
        //Create gameObject for area and add MeshFilter and AttributeClass
        GameObject newMeshObject = new GameObject();
        newMeshObject.name = "Object " + entryNumber;
        attributeClass = newMeshObject.AddComponent<AttributeClass>();
        attributeClass.attributes = attributes;
        MeshFilter meshFilter = newMeshObject.AddComponent<MeshFilter>();

        //Put the newMeshObject in the appropriate category
        SetParent(newMeshObject, attributeClass);

        //Gets value, removes opening tags and checks for null
        geometryValue = attributeClass.GetValueForKey("GEOMETRY")?.Replace("POLYGON(", "").Replace("\"", "");
        if (geometryValue == null)
        {
            Debug.LogError("Couldn't find GEOMETRY key value.");
            yield break;
        }

        //Split polygons into their own strings and remove parentheses and extra spaces
        geometryValue = geometryValue.Replace("), ( ", "\n").Replace("( ", "").Replace(")", "").Replace(", ", ",");
        string[] polygons_String = geometryValue.Split('\n');

        string[] vertices_String, vertex_String;
        Vector2[] vertices_2D;
        Vector3[] vertices;

        PolygonCollider2D newCollider = newMeshObject.AddComponent<PolygonCollider2D>();
        newCollider.pathCount = polygons_String.Length;

        for (int j = 0; j < polygons_String.Length; j++)
        {
            vertices_String = polygons_String[j].Split(',');//Splits entry into xy pairs representing points
            vertices_2D = new Vector2[vertices_String.Length];
            vertices = new Vector3[vertices_String.Length];

            //Parse geometry entry into vector arrays. 2D array is used in function that calculates triangles
            int i;
            for (i = 0; i < vertices.Length; i++)
            {
                vertex_String = vertices_String[i].Split(' ');
                Vector2 vertex = new Vector2((float)(double.Parse(vertex_String[0]) - X_offset), (float)(double.Parse(vertex_String[1]) - Y_offset));
                vertices_2D[i] = vertex;
                vertices[i] = new Vector3(vertices_2D[i][0], 0, vertices_2D[i][1]);

                if (showDebugPoints)
                {
                    GameObject testPoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    testPoint.name = "TestPoint: " + i;
                    testPoint.transform.parent = newMeshObject.transform;
                    testPoint.transform.localScale = Vector3.one * .1f;
                    testPoint.transform.position = vertices[i];

                }
            }
            newCollider.SetPath(j, vertices_2D);
            yield return null;
        }

        

        meshFilter.sharedMesh = BuildMeshWithPolygonCollider(newCollider);
        if (saveMeshes)
        {
            SaveMesh(meshFilter.sharedMesh, newMeshObject.name, true, false);
        }

        if (createRenderers)
        {
            MeshRenderer meshRenderer = newMeshObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = new Material(Shader.Find("Diffuse"));
        }
    }

    Mesh BuildMeshWithPolygonCollider(PolygonCollider2D newCollider)
    {
        int tries = 0;
        Mesh mesh = newCollider.CreateMesh(false, false);
        while (mesh == null)
        {
            string colliderPoints = "";
            foreach (Vector2 point in newCollider.points)
                colliderPoints += point.x + " " + point.y + ",";
            
            tries++;
            for (int i = 0; i < newCollider.pathCount; i++)
                newCollider.SetPath(i, SpreadPoints(newCollider.GetPath(i), 0.005f*tries));

            Debug.Log("Retrying mesh creation from polygon collider. Attempt: " + tries + ". Line number: " + entryNumber + ". Collider points: " + colliderPoints);
            mesh = newCollider.CreateMesh(false, false);

            if (tries > 3)
            {
                Debug.LogError("Failed to create mesh from collider. Line number: " + entryNumber + " Polygon string: " + geometryValue + ". Collider points: " + colliderPoints);
                return null;
            }
        }
        DestroyImmediate(newCollider);

        //Convert mesh points to xz plane and set y value to terrain height
        Vector3[] convertedPoints = mesh.vertices;
        for (int j = 0; j < convertedPoints.Length; j++)
        {
            convertedPoints[j].z = convertedPoints[j].y;
            if (TerrainUtils.getNearestTerrain(convertedPoints[j]) != null)
                convertedPoints[j].y = TerrainUtils.getTerrainHeight(convertedPoints[j]) + 0.05f;
            else
                convertedPoints[j].y = 0;
        }
        mesh.vertices = convertedPoints;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
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


    protected override void OnFileEnd()
    {
        //objectParent.transform.Rotate(Vector3.right * 90);
        if (createRenderers)
            for (int i = 0; i < objectParent.transform.childCount; i++)
            {
                GameObject container = objectParent.transform.GetChild(i).gameObject;
                MaterialPropertyBlock mpb = new MaterialPropertyBlock();
                var hash = container.name.GetHashCode();
                var a = (byte)255;
                var r = (byte)(hash >> 16);
                var g = (byte)(hash >> 8);
                var b = (byte)(hash);

                mpb.SetColor("_Color", new Color32(r, g, b, a));
                foreach (Renderer renderer in container.GetComponentsInChildren<Renderer>())
                {
                    renderer.SetPropertyBlock(mpb);
                }
            }
        base.OnFileEnd();
    }

    void SetParent(GameObject targetObject, AttributeClass attributeClass)
    {
        string surfType = attributeClass.GetValueForKey("Surf_Type");

        if (surfType != null)
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
        catch(Exception e)
        {
            Debug.LogError("Failed to save mesh to " + path + ". Error:" + e);

        }
    }
#endif
}
