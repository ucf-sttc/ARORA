using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;


/*
 * The task is responsible for maintaining the environment with regards to the
 * task, agents observation and action space, and the reward structure.
 */
public class Task : MonoBehaviour
{
    public GameObject m_NavigationAreaObject;
    [HideInInspector]
    public NavArea m_NavigationArea;
    //TODO: SIDECHANNEL
    public int m_NumExplorationPointObjects;
    public GameObject explorationPointPrefab, goalPointPrefab;
    protected GameObject[] m_explorationPoints;
    [HideInInspector]
    public GameObject [] m_goalPoint;
    public float[] agentAllowedArea;

    public enum Agent_Type{
        VectorAgent,
        VectorVisualAgent
    }
    public Agent_Type m_Default_Agent_Type = Agent_Type.VectorAgent;
    protected Agent_Type m_Agent_Type;
    public GameObject AgentPrefab;

    public int m_NumAgents = 1;
    [HideInInspector]
    public GameObject[] m_Agents;
    protected Dictionary<string, float> m_Rewards;
    public int agentCarPhysics, observationMode;
    protected bool relativeSteering;

    // Start is called before the first frame update
    protected virtual void Awake()
    {
        m_NavigationArea = m_NavigationAreaObject.GetComponent<NavArea>();
        m_NavigationArea.SetTask(this);
        m_explorationPoints = new GameObject[m_NumExplorationPointObjects];

        //Default to Vector
        m_Agent_Type = m_Default_Agent_Type;
        m_Agents = new GameObject[m_NumAgents];
        m_Rewards = new Dictionary<string, float>();

        EnvironmentParameters envParams = Academy.Instance.EnvironmentParameters;
        int points = (int)envParams.GetWithDefault("numberOfExplorationPoints", -1);
        if (points >= 0)
            m_NumExplorationPointObjects = points;
    }

    #region Environment
    //Determines agent placement of goal and exploration rewards
    public virtual void EnvSetup() { }
    public void AllAgentPlacement()
    {
        foreach (GameObject a in m_Agents)
        {
            AgentPlacement(a.GetComponent<Agent>(), m_NavigationArea);
        }
        if (!name.Equals("fastForwardEpisodes"))
        {
            DynamicSceneLoader.instance.agent = m_Agents[0];
            DynamicSceneLoader.CallInitialTileLoad();
        }
    }
    public virtual void AgentPlacement(Agent agent, NavArea navigationArea) { }
    public virtual void ExplorationPointPlacement() { }
    public virtual void GoalPlacement() { }
    public virtual void OnResetComplete(string name) 
    {
        
        
    }

    #endregion

    #region Agent
    //Controls the which agent type gets chosen
    public virtual void AgentSetup() 
    {
        m_NavigationArea.SetAgent(m_Agents[0]);
        TerrainLoadingTool terrainDistanceToggle = FindObjectOfType<TerrainLoadingTool>();
        if(terrainDistanceToggle != null)
            terrainDistanceToggle.agent = m_Agents[0];
        DynamicSceneLoader dynamicSceneLoader = FindObjectOfType<DynamicSceneLoader>();
        if(dynamicSceneLoader!= null)
            dynamicSceneLoader.agent = m_Agents[0];
    }
    #endregion

    #region Rewards
    public virtual void CollisionRewards(Agent agent, Collision collision) { }
    public virtual void NoViablePathReward(Agent agent) { }
    public virtual void StepReward(Agent agent) { }
    #endregion
}
