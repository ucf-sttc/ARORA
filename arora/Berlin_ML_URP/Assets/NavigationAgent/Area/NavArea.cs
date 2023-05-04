using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using UnityEngine;
using UnityEngine.AI;
using Utils;

public class NavArea : MonoBehaviour
{
    protected Task m_Task;
    public float maxSlope = 10; // max allowed angle for placement
    public List<Vector3> validSpawnLocations;

    int placementID;
    GameObject m_agent;
    NavMeshTriangulation navMeshData;
    NavMeshQueryFilter queryFilter, navigableQF;
    NavMeshPath path, navigablePath;
    NavMeshHit hit, clearanceTestHit;
    Bounds m_bounds;
    System.Random agentRandomPlacement, goalRandomPlacement, navmeshRandomPoint;
    public float trainingAreaX;

    WriteLogFile logWriter;
    void Awake()
    {
        Debug.Log("Random seed: " + Random.seed);
        agentRandomPlacement = new System.Random(Random.seed);
        goalRandomPlacement = new System.Random(Random.seed);
        navmeshRandomPoint = new System.Random(Random.seed);

        navMeshData = NavMesh.CalculateTriangulation();
        m_bounds.SetMinMax(navMeshData.vertices[0], navMeshData.vertices[0]);
        foreach (Vector3 v in navMeshData.vertices)
            m_bounds.Encapsulate(v);

        if (validSpawnLocations == null || validSpawnLocations.Count == 0)
        {
            Debug.Log("Calculating spawn locations");
            PopulateValidSpawnLocations();
        }
    }

    private void Start()
    {
        if (m_Task.agentAllowedArea != null && m_Task.agentAllowedArea.Length > 0)
            UpdateBounds();
    }

    // called from Task setup to make sure values are initialized
    public void SetAgent(GameObject a)
    {
        m_agent = a;

        navigableQF = new NavMeshQueryFilter
        {
            agentTypeID = m_agent.GetComponent<NavMeshAgent>().agentTypeID,
            areaMask = (1 << NavMesh.GetAreaFromName("ShortestPath"))
        };
    }

    void PopulateValidSpawnLocations()
    {
        validSpawnLocations = new List<Vector3>();

        for (int i = 0; i < navMeshData.indices.Length; i++)
        {
            int areaIndex = navMeshData.areas[i / 3];
            Vector3 location = navMeshData.vertices[navMeshData.indices[i]];
            if (areaIndex == 0 && NavMesh.SamplePosition(location, out NavMeshHit hit, 2, NavMesh.GetAreaFromName("ShortestPath")))
                validSpawnLocations.Add(location);
        }
        validSpawnLocations = validSpawnLocations.Distinct().OrderBy(v =>v.x).ThenBy(v=>v.z).ThenBy(v=>v.y).ToList();
    }

    public void ResetArea(string name)
    {
        Debug.Log("ResetArea(" + name + ")");
        m_Task.AllAgentPlacement();
        m_Task.ExplorationPointPlacement();
        m_Task.GoalPlacement();
        m_Task.OnResetComplete(name);
    }



    //This function gets a random location on the navmesh that is accessable by the given agent
    public Vector3 GetRandomLocationOnNavMesh(Agent a)
    {
        Vector3 randomPosition = Vector3.zero;
        queryFilter = new NavMeshQueryFilter();

        // use a separate, dedicated NavMesh that we prebaked to afford better clearance for placing the agent
        placementID = a.GetComponent<NavMeshAgent>().agentTypeID;
        string agentTypeName = NavMesh.GetSettingsNameFromID(placementID);
        for (int j = 0; j < NavMesh.GetSettingsCount(); j++)
        {
            int id = NavMesh.GetSettingsByIndex(j).agentTypeID;
            if (NavMesh.GetSettingsNameFromID(id).Equals(agentTypeName + "Placement"))
            {
                placementID = id;
                break;
            }
        }

        queryFilter.agentTypeID = placementID;
        queryFilter.areaMask = (1 << NavMesh.GetAreaFromName("Walkable"));
        float range = 1;
        int i;
        for (i = 0; i < 100; i++)
        {
            if (i > 70) range = 2;
            if (i > 90) range = 4;
            int randomInt = agentRandomPlacement.Next(validSpawnLocations.Count);
            //Debug.Log("Random int for agent placement: " + randomInt);
            Vector3 newPosition = validSpawnLocations[randomInt] + Vector3.up;// new Vector3(Random.Range(b.min.x, b.max.x), b.center.y, Random.Range(b.min.z, b.max.z));
            if (NavMesh.SamplePosition(newPosition, out NavMeshHit hit, range, queryFilter))
            {
                randomPosition = hit.position;
                break;
            }
        }
        if (i == 100)
            Debug.LogError("Failed to locate a navMesh point for agent " + NavMesh.GetSettingsNameFromID(queryFilter.agentTypeID));

        Debug.Log("New agent location: " + randomPosition.ToString());
        return randomPosition;
    }

    public Vector3 GetLocationOnNavMeshWithinRangeOfTarget(Agent a, float distance, float clearance = 0)
    {
        Vector3 randomPosition = Vector3.zero;
        queryFilter = new NavMeshQueryFilter
        {
            agentTypeID = a.GetComponent<NavMeshAgent>().agentTypeID,
            areaMask = (1 << NavMesh.GetAreaFromName("ShortestPath"))
        };
        path = new NavMeshPath();
        float pathLength;
        int i, j, searchRange = 2;
        //int count_contained = 0, count_clear = 0;
        List<NavMeshPath> paths = new List<NavMeshPath>();

        for (i = 0; i < 500; i++)
        {
            pathLength = 0.0f;
            if (i > 50) searchRange = 4;
            if (i > 100) searchRange = 8;
            if (i > 300) searchRange = 16;

            Vector3 offsetPosition = new Vector3(Mathf.Round((float)goalRandomPlacement.NextDouble()*1000f)/1000, 0, Mathf.Round((float)goalRandomPlacement.NextDouble() * 1000f) / 1000).normalized * (distance + searchRange);
            offsetPosition = a.transform.position + offsetPosition;
            //offsetPosition.y = TerrainUtils.getTerrainHeight(offsetPosition);

            if (m_bounds.Contains(offsetPosition) && NavMesh.SamplePosition(offsetPosition, out hit, 2f, queryFilter))
            {
                //count_contained++;
                if(clearance > 0) //If a clearance requirement has been set check for it. Select another position if the requirement is not met
                {
                    if (!NavMesh.FindClosestEdge(offsetPosition, out clearanceTestHit, queryFilter) || CheckClearance(offsetPosition) < clearance)
                        continue;
                }
                //count_clear++;
                randomPosition = hit.position;
                if (NavMesh.CalculatePath(a.transform.position, randomPosition, queryFilter, path))
                {
                    if ((path.status != NavMeshPathStatus.PathInvalid) && (path.corners.Length > 1))
                    {
                        for (j = 1; j < path.corners.Length; j++)
                        {
                            pathLength += Vector3.Distance(path.corners[j - 1], path.corners[j]);
                            if (pathLength >= distance)
                            {
                                randomPosition = path.corners[j] + ((path.corners[j - 1] - path.corners[j]).normalized * (pathLength - distance));
                                if (clearance > 0 && CheckClearance(randomPosition) < clearance)
                                {
                                    paths.Add(path); // if the position doesn't meet clearance req, add to fallback list
                                    break;
                                }
                                Debug.Log("Goal location: " + randomPosition.ToString());
                                return randomPosition;
                            }
                        }
                    }
                }
            }
        }
        // no points found, fallback to checking points that met clearance requirement
        if(clearance > 0)
            for(i=0;i<paths.Count();i++)
            {
                pathLength = 0;
                path = paths[i];
                for (j = 1; j < path.corners.Length; j++)
                {
                    pathLength += Vector3.Distance(path.corners[j - 1], path.corners[j]);
                    if (pathLength >= distance)
                    {
                        randomPosition = path.corners[j];
                        if (CheckClearance(randomPosition) >= clearance)
                        {
                            Debug.LogError(string.Format("Fallback Goal location {0}\nFailed to locate a goal point within range for agent at position {1}\n",
                                randomPosition.ToString(), a.transform.position));
                            return randomPosition;
                        }
                    }
                }
            }

        // NOTE agent positions that fail sample very few points that fall into the "count_contained" category
        // algorithm can be improved in this scenario (probably only narrow roads in radius)
        //Debug.LogError(string.Format("Points tested: {0} :: {1} :: {2}",count_contained,count_clear,paths.Count()));
        Debug.LogError("Failed to locate a goal point within range for agent at position " + a.transform.position);
        return randomPosition;
    }

    public Vector3 GetRandomLocation(int recursionLevel)
    {
        int maxIndices = navMeshData.indices.Length - 3;
        // Pick the first indice of a random triangle in the nav mesh
        int firstVertexSelected = Random.Range(0, maxIndices);
        int secondVertexSelected = Random.Range(0, maxIndices);
        //Spawn on Vertices
        Vector3 point = navMeshData.vertices[navMeshData.indices[firstVertexSelected]];

        Vector3 firstVertexPosition = navMeshData.vertices[navMeshData.indices[firstVertexSelected]];
        Vector3 secondVertexPosition = navMeshData.vertices[navMeshData.indices[secondVertexSelected]];
        //Eliminate points that share a similar X or Z position to stop spawning in square grid line formations
        if ((
                (int)firstVertexPosition.x == (int)secondVertexPosition.x ||
                (int)firstVertexPosition.z == (int)secondVertexPosition.z
            ) &&
            recursionLevel < 3)
        {
            point = GetRandomLocation(recursionLevel + 1); //Re-Roll a position - I'm not happy with this recursion it could be better
        }
        else
        {
            // Select a random point on it
            point = Vector3.Lerp(
                                    firstVertexPosition,
                                    secondVertexPosition, //[t + 1]],
                                    Random.Range(0.05f, 0.95f) // Not using Random.value as clumps form around Verticies 
                                );
        }

        return point;
    }

    public Quaternion GetRandomUprightRotation()
    {
        //float heading = Random.Range(0, 360) * Mathf.Deg2Rad;
        //Quaternion randomRotation = new Quaternion(0, Mathf.Sin(heading / 2), 0, Mathf.Cos(heading / 2)).normalized;
        Quaternion randomRotation = Quaternion.Euler(0, agentRandomPlacement.Next(360), 0);

        return randomRotation;
    }

    public float GetShortestPathDistanceFromAgentToGoal(Agent a, GameObject goal)
    {
        queryFilter = new NavMeshQueryFilter
        {
            agentTypeID = a.GetComponent<NavMeshAgent>().agentTypeID,
            areaMask = (1 << NavMesh.GetAreaFromName("ShortestPath"))
        };
        path = new NavMeshPath();
        float pathLength = 0.0f;
        List<Vector3> allCorners = new List<Vector3>();

        NavMesh.SamplePosition(a.transform.position, out hit, 2, queryFilter);

        Vector3 start = hit.position;
        for (int i = 0; i < 100 && NavMesh.CalculatePath(start, goal.transform.position, queryFilter, path) && (path.status == NavMeshPathStatus.PathPartial) && (path.corners.Length > 1); i++)
        {
            allCorners.AddRange(path.corners);
            start = path.corners[path.corners.Length - 1];
        }

        if (path.status == NavMeshPathStatus.PathComplete)
        {
            allCorners.AddRange(path.corners);
            for (int i = 1; i < allCorners.Count; i++)
                pathLength += Vector3.Distance(allCorners[i - 1], allCorners[i]);

            if (logWriter != null)
                logWriter.WriteToFile(string.Join(",", allCorners.Select(x => x.ToString()).ToArray()));
            //Debug.Log("Path found. Length: " + pathLength);

            AdditionalSideChannels.shortestPathSideChannel.m_path = allCorners;
            return pathLength;
        }
        else
        {
            Debug.Log("Failed to generate path between agent at " + a.transform.position + " and goal at " + goal.transform.position);
            return -1;
        }
    }

    public float CheckClearance(Vector3 position)
    {

        if (NavMesh.FindClosestEdge(position, out clearanceTestHit, navigableQF))
            return Vector3.Distance(position, clearanceTestHit.position);
        else return 0;
    }

    #region Navigable
    // SampleNavmesh and IsNavigableFromAgent use global m_agent unlike the above functions
    // this is mainly a simple or temp solution to allow being called from python
    //
    // SampleNavmesh returns a random navigable point on the navmesh
    public Vector3 SampleNavigableNavmesh()
    {
        Vector3 p = GetRandomNavmeshPoint();
        int i;

        // capped at 100 iterations to prevent running too long
        for (i = 0; i < 100 && !IsNavigableFromAgent(p); i++)
            p = GetRandomNavmeshPoint();

        if (i == 100)
        {
            Debug.LogWarning("Failed to find navigable point");
            return Vector3.negativeInfinity;
        }

        return p;
    }

    // returns a random point lerped from a random navmesh triangle's 3 vertices
    Vector3 GetRandomNavmeshPoint()
    {
        int t = navmeshRandomPoint.Next(0, navMeshData.indices.Length - 3);

        Vector3 p = Vector3.Lerp(navMeshData.vertices[navMeshData.indices[t]], navMeshData.vertices[navMeshData.indices[t + 1]], (float)navmeshRandomPoint.NextDouble());
        p = Vector3.Lerp(p, navMeshData.vertices[navMeshData.indices[t + 2]], (float)navmeshRandomPoint.NextDouble());

        return p;
    }

    // checks if the specified x,z has a 3D point that is reachable by agent
    public bool IsNavigableFromAgent(Vector2 p, out Vector3 o)
    {
        o = Vector3.negativeInfinity;

        NavMesh.SamplePosition(m_agent.transform.position, out hit, 1f, navigableQF);
        if (!hit.hit) { Debug.LogError("Agent not on NavMesh"); return false; }

        Vector3 a = hit.position;
        Vector3 temp = new Vector3(p[0], 0, p[1]);

        for (float i = m_bounds.min.y; i < m_bounds.max.y; i += 0.5f)
        {
            temp.y = i;
            if (NavMesh.SamplePosition(temp, out hit, 0.3f, navigableQF))
                break;
        }

        if (!hit.hit) { return false; }
        Vector3 b = hit.position;

        if (IsNavigable(a, b))
        {
            o = b;
            return true;
        }
        return false;
    }

    // checks if the specified point is reachable by agent
    public bool IsNavigableFromAgent(Vector3 p)
    {
        NavMesh.SamplePosition(m_agent.transform.position, out hit, 1f, navigableQF);
        if (!hit.hit) { Debug.LogError("Agent not on NavMesh"); return false; }
        Vector3 a = hit.position;

        NavMesh.SamplePosition(p, out hit, 0.1f, navigableQF);
        if (!hit.hit) { return false; }
        Vector3 b = hit.position;

        return IsNavigable(a, b);
    }

    public bool IsNavigable(Vector3 a, Vector3 b)
    {
        navigablePath = new NavMeshPath();

        // CalculatePath returns partial path if the destination is too far, so we repeat using the end of the partial path as our new starting point
        // capped at 100 iterations to prevent running too long
        for (int i = 0; i < 100 && NavMesh.CalculatePath(a, b, navigableQF, navigablePath) && (navigablePath.status == NavMeshPathStatus.PathPartial); i++)
        {
            a = navigablePath.corners[navigablePath.corners.Length - 1];
        }

        if (navigablePath.status == NavMeshPathStatus.PathComplete)
            return true;

        return false;
    }
    #endregion

    public void UpdateBounds()
    {
        float[] b = m_Task.agentAllowedArea;
        m_bounds.min = new Vector3(b[0], m_bounds.min.y, b[1]);
        m_bounds.max = new Vector3(b[2], m_bounds.max.y, b[3]);
        foreach (Vector3 v in validSpawnLocations.Reverse<Vector3>())
            if (!m_bounds.Contains(v))
                validSpawnLocations.Remove(v);
        Debug.Log("Area bounds updated from args");
    }

    public void SetTask(Task t)
    {
        m_Task = t;
    }

    public Task GetTask()
    {
        return m_Task;
    }
}