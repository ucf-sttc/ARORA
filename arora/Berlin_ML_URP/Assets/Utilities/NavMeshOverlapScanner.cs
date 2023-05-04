using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshOverlapScanner : MonoBehaviour
{
    public NavMeshTriangulation triangulation;
    public List<Vector3> roadPoints, shortestPathPoints, nonOverlappingPoints;
    public float xMin=5, xMax=3275, zMin=5, zMax=2670;

#if UNITY_EDITOR
    [ContextMenu("Gather Navmesh Points")]
    void GatherNavmeshPoints()
    {
        roadPoints = new List<Vector3>();
        shortestPathPoints = new List<Vector3>();
        nonOverlappingPoints = new List<Vector3>();
        triangulation = NavMesh.CalculateTriangulation();

        for (int i = 0; i < triangulation.areas.Length; i++)
        {
            UnityEditor.EditorUtility.DisplayProgressBar("Gathering points", "Point " + i + " of " + triangulation.areas.Length, i / triangulation.areas.Length);
            int j = i * 3;

            for (int x = 0; x < 3; x++)
            {
                Vector3 position = triangulation.vertices[triangulation.indices[j + x]];
                if (triangulation.areas[i] == 0)
                {
                    if (!roadPoints.Contains(position) && position.x > xMin && position.x < xMax && position.z > zMin && position.z < zMax)
                        roadPoints.Add(position);
                }
                else
                {
                    if (!shortestPathPoints.Contains(position) && position.x > xMin && position.x < xMax && position.z > zMin && position.z < zMax)
                        shortestPathPoints.Add(position);
                }
            }
        }
        UnityEditor.EditorUtility.ClearProgressBar();
    }

    [ContextMenu("Locate non-overlapping points")]
    void LocateNonOverlappingPoints()
    {
        for(int i=0; i<roadPoints.Count; i++)
        {
            Vector3 point = roadPoints[i];
            //bool overlapping = false;
            NavMeshHit hit;
            NavMeshQueryFilter filter = new NavMeshQueryFilter();
            filter.agentTypeID = GetComponent<NavMeshSurface>().agentTypeID;
            filter.areaMask = (1 << NavMesh.GetAreaFromName("ShortestPath"));

            UnityEditor.EditorUtility.DisplayProgressBar("Comparing points", "Road point " + i + " of " + roadPoints.Count, i / roadPoints.Count);

            if (!NavMesh.SamplePosition(point, out hit, 3, filter))
                nonOverlappingPoints.Add(point);
            /*
            for (int j=0; j<shortestPathPoints.Count; j++)
            {
                Vector3 sPoint = shortestPathPoints[j];
                UnityEditor.EditorUtility.DisplayProgressBar("Comparing points", "Road point " + i + " of " + roadPoints.Count + ". Shortest path point " + j + " of " + shortestPathPoints.Count, i / roadPoints.Count);
                if (Vector3.Distance(point, sPoint) < 3)
                {
                    overlapping = true;
                    break;
                }
            }
            if (!overlapping)
                nonOverlappingPoints.Add(point);
            */
        }
        UnityEditor.EditorUtility.ClearProgressBar();
    }

    [ContextMenu("Print points")]
    void PrintPoints()
    {
        string path = "Assets/Resources/overlappingPointList.txt";
        StreamWriter writer = new StreamWriter(path, true);
        foreach (Vector3 point in nonOverlappingPoints)
        {
            writer.WriteLine(point);
            Debug.Log(point);
        }
        writer.Close();
            
    }

    [ContextMenu("Print tiles from non-overlapping points")]
    void PrintTilesFromPoints()
    {
        List<string> tileList = new List<string>();
        DynamicSceneLoader dsl = FindObjectOfType<DynamicSceneLoader>();
        foreach (Vector3 point in nonOverlappingPoints)
        {
            string tileName = dsl.getTileForPosition(new Vector2(point.x, point.z));
            if (!tileList.Contains(tileName))
                tileList.Add(tileName);

        }

        string path = "Assets/Resources/tileList.txt";
        StreamWriter writer = new StreamWriter(path, true);
        foreach (string tile in tileList)
        {
            writer.WriteLine(tile);
            Debug.Log(tile);
        }
        writer.Close();

    }
#endif
}
