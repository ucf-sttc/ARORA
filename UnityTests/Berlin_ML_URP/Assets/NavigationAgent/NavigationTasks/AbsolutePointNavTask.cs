using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;

public class AbsolutePointNavTask : Task
{
    SegmentationSetup segmentationSetup;
    NavAgent car;
    float[,] agentToGoalSpl;
    float splDelta;

    float goalSetDistance; //The distance from the agent that the goal will be set
    float goalClearance; //The minimum distance from any obstacles that the goal will be spawned at
    protected override void Awake()
    {
        base.Awake();
        segmentationSetup = FindObjectOfType<SegmentationSetup>();

        //EnvironmentParameters envParams = Academy.Instance.EnvironmentParameters;
        CommandLineArgs.Args envParams = CommandLineArgs.Instance.Parameters;
        goalSetDistance = envParams.GetWithDefault("goalDistance", 50);
        goalClearance = envParams.GetWithDefault("goalClearance", 2);

        m_Rewards.Add("rewardForGoal", envParams.GetWithDefault("rewardForGoal", 50f));
        m_Rewards.Add("rewardForNoViablePath", envParams.GetWithDefault("rewardForNoViablePath", -50f));
        m_Rewards.Add("rewardStep", (m_Rewards["rewardForGoal"]/ goalSetDistance) *envParams.GetWithDefault("rewardStepMul", 0.1f));
        m_Rewards.Add("rewardCollision", m_Rewards["rewardStep"] * envParams.GetWithDefault("rewardCollisionMul", 4f));
        m_Rewards.Add("rewardSplDeltaMul", envParams.GetWithDefault("rewardSplDeltaMul", 1f));

        m_Rewards.Add("rewardForExplorationPointCollision", envParams.GetWithDefault("rewardForExplorationPointCollision", .005f));
        m_Rewards.Add("rewardForTrafficCollision", envParams.GetWithDefault("rewardForTrafficCollision", -.2f));

        GameObject followUIPane = GameObject.Find("RawFollow");
        if (followUIPane != null)
            followUIPane.SetActive(false);

        agentCarPhysics = (int)envParams.GetWithDefault("agentCarPhysics", 0);
        relativeSteering = envParams.GetWithDefault("relativeSteering", 1) == 1;

        int observationMode = (int)envParams.GetWithDefault("observationMode", 1);
        if ((Agent_Type)observationMode == Agent_Type.VectorAgent) m_Agent_Type = Agent_Type.VectorAgent;
        else if ((Agent_Type)observationMode == Agent_Type.VectorVisualAgent) m_Agent_Type = Agent_Type.VectorVisualAgent;
        else m_Agent_Type = m_Default_Agent_Type;
        Debug.Log("Agent type = " + m_Agent_Type);
        AgentSetup();
        agentToGoalSpl = new float[m_Agents.Length, 2];
    }

    #region Environment
    public override void AgentPlacement(Agent agent, NavArea navigationArea) 
    {
        car = (NavAgent)agent; // TODO: add checking or refactor depending on future agent types

        car.transform.position = m_NavigationArea.GetRandomLocationOnNavMesh(agent); //Place agent in random acceptable position
        car.transform.rotation = Quaternion.identity;//m_NavigationArea.GetRandomUprightRotation(); // make agent upright
        car.rbody.velocity = Vector3.zero;
        car.rbody.angularVelocity = Vector3.zero;
    }
    public override void ExplorationPointPlacement() 
    {
        for (int i = 0; i < m_NumExplorationPointObjects; i++)
        {
            if (m_explorationPoints[i] != null)
                Destroy(m_explorationPoints[i]);
            m_explorationPoints[i] = Instantiate(explorationPointPrefab, m_NavigationArea.GetRandomLocation(0) + Vector3.up / 2, Quaternion.identity, gameObject.transform);
            if (segmentationSetup != null)
                segmentationSetup.SetupGameObjectSegmentation(m_explorationPoints[i]);
        }
    }
    public override void GoalPlacement()
    {
        for (int i = 0; i < m_goalPoint.Length; i++)
            if (m_goalPoint[i] != null)
                Destroy(m_goalPoint[i]);

        m_goalPoint = new GameObject[1];
        
        m_goalPoint[0] = Instantiate(goalPointPrefab, m_NavigationArea.GetLocationOnNavMeshWithinRangeOfTarget(car, goalSetDistance, goalClearance), Quaternion.identity, gameObject.transform);
        if (segmentationSetup != null)
            segmentationSetup.SetupGameObjectSegmentation(m_goalPoint[0]);
    }

    public override void OnResetComplete(string name)
    {
        for(int i=0; i<m_Agents.Length; i++)
        {
            agentToGoalSpl[i,1] = Mathf.Round(m_NavigationArea.GetShortestPathDistanceFromAgentToGoal(m_Agents[0].GetComponent<Agent>(), m_goalPoint[0])*100)/100f;
            if (agentToGoalSpl[i, 1] == -1 || agentToGoalSpl[i, 1] < goalSetDistance * .95f || agentToGoalSpl[i, 1] > goalSetDistance * 1.05f)
            {
                m_NavigationArea.ResetArea("shortestPathFailure");
            }
            else if(!name.Equals("fastForwardEpisodes"))
            {
                
                AdditionalSideChannels.floatPropertiesChannel.Set("ShortestPath", agentToGoalSpl[i, 1]);
                Debug.Log("Shortest path calculated. Agent position = " + m_Agents[0].transform.position + ". Goal position = " + m_goalPoint[0].transform.position + ". Distance = " + agentToGoalSpl[i, 1]);
                base.OnResetComplete(name);
            }
        }
    }
    #endregion

    #region Agent
    //Controls the which agent type gets chosen
    public override void AgentSetup()
    {
        for (int i = 0; i < m_NumAgents; i++)
        {
            if (m_Agents[i] != null)
                Destroy(m_Agents[i]);

            AgentPrefab.SetActive(false); // prevents initialization until instance variables are properly assigned
            m_Agents[i] = Instantiate(AgentPrefab, m_NavigationArea.gameObject.transform);
            UniversalCarAgent a = m_Agents[i].GetComponent<UniversalCarAgent>();
            a.physicsMode = agentCarPhysics;
            a.agentType = m_Agent_Type;
            a.SetTask(this);
            m_Agents[i].GetComponent<NavAgent>().relativeSteering = relativeSteering;
            m_Agents[i].GetComponent<Agent>().MaxStep = (int)CommandLineArgs.Instance.Parameters.GetWithDefault("episodeLength", 2500);
            m_Agents[i].SetActive(true);
            AgentPrefab.SetActive(true);
        }
        base.AgentSetup();
    }
    #endregion

    #region Rewards
    public override void CollisionRewards(Agent agent, Collision collision) 
    {
        if(!collision.gameObject.name.Contains("Terrain") && !collision.gameObject.name.Contains("Road"))
        switch(collision.gameObject.tag)
        {
            case "goal":
                agent.SetReward(m_Rewards["rewardForGoal"]);
                agent.EndEpisode();
                break;
            case "food":
                agent.AddReward(m_Rewards["rewardForExplorationPoint"]);
                Destroy(collision.gameObject);
                break;
            case "AutonomousVehicle":
                agent.AddReward(m_Rewards["rewardForTrafficCollision"]);
                break;
            default:
                agent.AddReward(-m_Rewards["rewardCollision"]);
                break;
        }
    }
    public override void NoViablePathReward(Agent agent) 
    {
        for (int i = 0; i < m_Agents.Length; i++)
            if (agent.gameObject == m_Agents[i].gameObject)
            {
                if (agent.transform.position.y < -15 || agentToGoalSpl[i, 1] == -1)// Agent has fallen off map or gotten to a position where it can no longer reach the goal
                {
                    agent.SetReward(m_Rewards["rewardForNoViablePath"]);
                    agent.EndEpisode();
                }
                break;
            }
    }
    public override void StepReward(Agent agent) 
    {
        for(int i = 0; i<m_Agents.Length; i++)
            if(agent.gameObject == m_Agents[i].gameObject)
            {
                agent.AddReward(-m_Rewards["rewardStep"]);
                agentToGoalSpl[i, 0] = agentToGoalSpl[i, 1]; //Move spl to previous slot
                agentToGoalSpl[i, 1] = Mathf.Round(m_NavigationArea.GetShortestPathDistanceFromAgentToGoal(agent, m_goalPoint[i]) * 100) / 100f;
                AdditionalSideChannels.floatPropertiesChannel.Set("ShortestPath", agentToGoalSpl[i, 1]);
                splDelta = agentToGoalSpl[i, 0] - agentToGoalSpl[i, 1];
                if (splDelta == 0)
                    splDelta = -1;
                agent.AddReward(splDelta * m_Rewards["rewardSplDeltaMul"]);
                break;
            }
    }
    #endregion

}
