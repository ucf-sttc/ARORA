using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using UnityEngine.UI;
using System;

public class ContrivedObjectNavTask : Task
{
    public string m_GoalType = "VehicleX";
    SegmentationSetup segmentationSetup;
    Vector3 m_startPosition = new Vector3(2377.029f, 38.14f, 1527.77f);
    //Test position
    //Vector3 m_startPosition = new Vector3(2432.9f, 38.14f, 1540f);
    Vector3 m_endPosition = new Vector3(2428.45f, 37.52f, 1524.35f);
    protected override void Awake()
    {
        m_NumExplorationPointObjects = 30;

        base.Awake();
        segmentationSetup = FindObjectOfType<SegmentationSetup>();

        //EnvironmentParameters envParams = Academy.Instance.EnvironmentParameters;
        CommandLineArgs.Args envParams = CommandLineArgs.Instance.Parameters;
        m_Rewards.Add("rewardForGoalCollision", envParams.GetWithDefault("rewardForGoalCollision", .5f));
        m_Rewards.Add("rewardForExplorationPointCollision", envParams.GetWithDefault("rewardForExplorationPointCollision", .005f));
        m_Rewards.Add("rewardForTrafficCollision", envParams.GetWithDefault("rewardForTrafficCollision", -.2f));
        m_Rewards.Add("rewardForOtherCollision", envParams.GetWithDefault("rewardForOtherCollision", -.1f));
        m_Rewards.Add("rewardForFallingOffMap", envParams.GetWithDefault("rewardForFallingOffMap", -1f));
        m_Rewards.Add("rewardForEachStep", envParams.GetWithDefault("rewardForEachStep", -.0001f));
        //TODO Get goal through string side channel from python
        //m_GoalType = envParams.GetWithDefault("goalObjectType", -.0001f).ToString();

        //Goal Related
        //TODO Set Layer segmentation automatically, currently by setting segmentation objects to ParkedVehicle layer in editor
        //This needs to be done by selecting based upon some kind of attribute
        //Types could be Vehicles, Buildings, Trees, Streets, TerrianType
        GameObject goalDisplayText = GameObject.Find("GoalDisplay");
        if(goalDisplayText!=null)
            goalDisplayText.GetComponent<Text>().text = m_GoalType;

        //Need to automatically set goals based upon the selected type

        agentCarPhysics = (int)envParams.GetWithDefault("agentCarPhysics", 0);
        relativeSteering = envParams.GetWithDefault("relativeSteering", 1) == 1;

        int observationMode = (int)envParams.GetWithDefault("observationMode", -1);
        if ((Agent_Type)observationMode == Agent_Type.VectorAgent) m_Agent_Type = Agent_Type.VectorAgent;
        else if ((Agent_Type)observationMode == Agent_Type.VectorVisualAgent) m_Agent_Type = Agent_Type.VectorVisualAgent;
        else m_Agent_Type = m_Default_Agent_Type;
        AgentSetup();
    }

    #region Environment
    public override void AgentPlacement(Agent agent, NavArea navigationArea) 
    {
        NavAgent car = (NavAgent)agent; // TODO: add checking or refactor depending on future agent types
        car.transform.position = m_startPosition; //Place agent in random acceptable position
        car.transform.rotation = Quaternion.Euler(0, 90, 0); // make agent upright
        car.rbody.velocity = Vector3.zero;
        car.rbody.angularVelocity = Vector3.zero;
    }
    public override void ExplorationPointPlacement()
    {
        Vector3 exp_startPostion = m_startPosition + new Vector3(12.0f,0f,0f);
        Vector3 start_pos = exp_startPostion;
        Vector3 end_pos = m_endPosition;
        Vector3 direction_vector = (end_pos - start_pos)/m_NumExplorationPointObjects;
        //Start at 1 and end before numExpPoints to start after the agent spawn position and before goal position
        for (int i = 0; i < m_NumExplorationPointObjects-1; i++)
        {
            if (m_explorationPoints[i] != null)
                Destroy(m_explorationPoints[i]);

            Vector3 linearPosition = start_pos + (i * direction_vector);
            //Vector3 linearPositionWithNoise = new Vector3(  start_pos.x + (i * direction_vector.x) + UnityEngine.Random.Range(-1.0f, 1.0f),
            //                                                start_pos.y + (i * direction_vector.y) + UnityEngine.Random.Range(-1.0f, 1.0f),
            //                                                start_pos.z + (i * direction_vector.z) + UnityEngine.Random.Range(-1.0f, 1.0f));
            m_explorationPoints[i] = Instantiate(explorationPointPrefab, linearPosition, Quaternion.identity, gameObject.transform);
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
        m_goalPoint[0] = Instantiate(goalPointPrefab, m_endPosition, Quaternion.identity, gameObject.transform);
        if (segmentationSetup != null)
            segmentationSetup.SetupGameObjectSegmentation(m_goalPoint[0]);
        
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

        switch (collision.gameObject.tag)
        {
            case "goal":
                agent.AddReward(m_Rewards["rewardForGoalCollision"]);
                agent.EndEpisode();
                break;
            case "food":
                agent.AddReward(m_Rewards["rewardForExplorationPointCollision"]);
                Destroy(collision.gameObject);
                break;
            case "AutonomousVehicle":
                agent.AddReward(m_Rewards["rewardForTrafficCollision"]);
                break;
            default:
                agent.AddReward(m_Rewards["rewardForOtherCollision"]);
                break;
        }
    }
    public override void NoViablePathReward(Agent agent) 
    {
        if (agent.transform.position.y < -15)// Agent has fallen off map
        {
            agent.AddReward(m_Rewards["rewardForFallingOffMap"]);
            agent.EndEpisode();
        }
    }
    public override void StepReward(Agent agent) {
        agent.AddReward(m_Rewards["rewardForEachStep"]);
    }
    #endregion

}
