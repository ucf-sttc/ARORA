#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.EditorCoroutines.Editor;
using UnityEditor;

public class SampleNavigableTest : MonoBehaviour
{
    public GameObject m_agent;
    public GameObject m_goal;
    public LineRenderer lr;
    public int maxTrials = 30;
    public float m_x = 0, m_z = 0;
    public bool reportSurfaceFriction = false;

    NavMeshQueryFilter navigableQF;
    NavMeshTriangulation navMeshData;
    NavMeshPath navigablePath;
    NavMeshHit hit;
    Bounds m_bounds;
    System.Random navmeshRandomPoint;
    bool isPlaying = false;

    [ContextMenu("Init")]
    private void Start()
    {
        navmeshRandomPoint = new System.Random();
        navMeshData = NavMesh.CalculateTriangulation();
        m_bounds.SetMinMax(navMeshData.vertices[0], navMeshData.vertices[0]);
        foreach (Vector3 v in navMeshData.vertices)
            m_bounds.Encapsulate(v);

        Debug.LogFormat("Bounds: {0} ----- {1}", m_bounds.min.ToString("F2"), m_bounds.max.ToString("F2"));

        navigableQF = new NavMeshQueryFilter()
        {
            agentTypeID = m_agent.GetComponent<NavMeshAgent>().agentTypeID,
            areaMask = (1 << NavMesh.GetAreaFromName("ShortestPath"))
        };
    }

    [ContextMenu("Run x times")]
    public void Play() {
        EditorCoroutineUtility.StartCoroutine(TrialRunner(), this);
    }

    [ContextMenu("Stop")]

    public void OnParticleSystemStopped() {
        isPlaying = false;
    }

    public IEnumerator TrialRunner()
    {
        Start();
        isPlaying = true;
        for(int i = 0; i < maxTrials && isPlaying; i++) {
            Debug.LogFormat("====== Trial {0} ======", i);
            SampleNavigableNavmesh();
            yield return new EditorWaitForSeconds(0.5f);
        }
    }

    [ContextMenu("Check x,z")]
    public void FindPointXZ()
    {
        Start();
        if(IsNavigableFromAgent(new Vector2(m_x, m_z), out Vector3 result))
            m_goal.transform.position = result;
        Debug.Log(result);
    }

    [ContextMenu("SampleNavmesh")]
    public void RunOnce()
    {
        Start();
        SampleNavigableNavmesh();
    }

    // SampleNavmesh returns a random navigable point on the navmesh
    public Vector3 SampleNavigableNavmesh()
    {
        Vector3 p = GetRandomNavmeshPoint();
        int i;

        // capped at 100 iterations to prevent running too long
        for(i = 0; i < 100 && !IsNavigableFromAgent(p); i++)
            p = GetRandomNavmeshPoint();

        if(i == 100)
        {
            Debug.LogWarning("Failed to find navigable point");
            return Vector3.negativeInfinity;
        }

        m_goal.transform.position = p;

        if (reportSurfaceFriction) PrintFriction();
        Debug.LogFormat("{0}: {1}", i, p);
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
        if (!hit.hit) { Debug.LogError("Agent not on NavMesh");  return false; }

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

        if(IsNavigable(a,b))
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
        List<Vector3> allCorners = new List<Vector3>();
        navigablePath = new NavMeshPath();

        // CalculatePath returns partial path if the destination is too far, so we repeat using the end of the partial path as our new starting point
        // capped at 100 iterations to prevent running too long
        for (int i = 0; i < 100 && NavMesh.CalculatePath(a, b, navigableQF, navigablePath) && (navigablePath.status == NavMeshPathStatus.PathPartial); i++)
        {
            allCorners.AddRange(navigablePath.corners);
            a = navigablePath.corners[navigablePath.corners.Length - 1];
        }

        if (navigablePath.status == NavMeshPathStatus.PathComplete)
        {
            allCorners.AddRange(navigablePath.corners);
            lr.positionCount = allCorners.Count;
            lr.SetPositions(allCorners.ToArray());
            Debug.LogFormat("{0}({1} corners)", navigablePath.status, allCorners.Count);
            return true;
        }

        return false;
    }

    [ContextMenu("TestSPL")]
    public void TestSPL()
    {
        GetShortestPathDistanceFromAgentToGoal(m_agent, m_goal);
    }


    public float GetShortestPathDistanceFromAgentToGoal(GameObject a, GameObject goal)
    {
        NavMeshQueryFilter queryFilter = new NavMeshQueryFilter
        {
            agentTypeID = a.GetComponent<NavMeshAgent>().agentTypeID,
            areaMask = (1 << NavMesh.GetAreaFromName("ShortestPath"))
        };
        NavMeshPath path = new NavMeshPath();
        float pathLength = 0.0f;
        List<Vector3> allCorners = new List<Vector3>();

        NavMesh.SamplePosition(a.transform.position, out hit, 2, queryFilter);

        Vector3 start = hit.position;
        for (int i = 0; i < 100 && NavMesh.CalculatePath(start, goal.transform.position, queryFilter, path) && (path.status == NavMeshPathStatus.PathPartial) && (path.corners.Length > 1); i++)
        {
            allCorners.AddRange(path.corners);
            start = path.corners[path.corners.Length - 1];
        }

        if(path.status == NavMeshPathStatus.PathComplete)
        {
            allCorners.AddRange(path.corners);
            for (int i = 1; i < allCorners.Count; i++)
                pathLength += Vector3.Distance(allCorners[i - 1], allCorners[i]);

            Debug.Log("Path found. Length: " + pathLength);

            return pathLength;
        }
        else
        {
            Debug.LogError("Failed to generate path between agent at " + a.transform.position + " and goal at " + goal.transform.position);
            return -1;
        }
    }

    void PrintFriction()
    {
        SurfaceFrictionValues.TestFriction(m_goal);
    }

    [CustomEditor(typeof(SampleNavigableTest))]
    public class SampleNavigableTestEditor : Editor
    {
        SampleNavigableTest snt;
        void OnSceneGUI()
        {
            snt = target as SampleNavigableTest;
            Event guiEvent = Event.current;
            if (guiEvent.type == EventType.Repaint)
                DebugDraw();
        }

        void DebugDraw()
        {
            if (!snt || !snt.m_agent || !snt.m_goal) return;
            Handles.color = Color.green;
            Handles.DrawLine(snt.m_agent.transform.position, snt.m_agent.transform.position + Vector3.up * 1000f);
            Handles.DrawLine(snt.m_goal.transform.position, snt.m_goal.transform.position + Vector3.up * 1000f);
        }
    }
}
#endif