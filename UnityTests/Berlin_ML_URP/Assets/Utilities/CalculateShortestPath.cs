using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CalculateShortestPath : MonoBehaviour
{
    public GameObject agent, goal;
    public LineRenderer r;
    public bool updateLine;
    NavMeshHit hit;

    private void Update()
    {
        if(updateLine)
            CheckPath();
    }

    [ContextMenu("CheckPath")]
    void CheckPath()
    {
        GetShortestPathDistanceFromAgentToGoal(agent, goal);
    }

    public float GetShortestPathDistanceFromAgentToGoal(GameObject a, GameObject goal)
    {
        NavMeshQueryFilter queryFilter = new NavMeshQueryFilter();
        NavMeshAgent nAgent = a.GetComponent<NavMeshAgent>();
        queryFilter.agentTypeID = nAgent.agentTypeID;
        queryFilter.areaMask = (1 << NavMesh.GetAreaFromName("ShortestPath"));
        NavMeshPath path = new NavMeshPath();
        float pathLength = 0.0f;

        NavMesh.SamplePosition(a.transform.position, out hit, 2, queryFilter);

        if (NavMesh.CalculatePath(hit.position, goal.transform.position, queryFilter, path) && (path.status == NavMeshPathStatus.PathComplete) && (path.corners.Length > 1))
        {
            for (int i = 1; i < path.corners.Length; i++)
                pathLength += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            Debug.Log("Path length: " + pathLength);
            r.SetPositions(path.corners);
            return pathLength;
        }
        else
        {
            Debug.LogError("Failed to generate path between agent at " + a.transform.position + " and goal at " + goal.transform.position);
            return -1;
        }

    }
}
