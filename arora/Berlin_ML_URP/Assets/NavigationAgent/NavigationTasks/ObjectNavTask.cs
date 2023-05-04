using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using UnityEngine.UI;
using Utils;

public class ObjectNavTask : Task
{
    public string m_GoalType = "Vehicle";
    SegmentationSetup segmentationSetup;

    public TextAsset vehicleCSV;

    List<Vector3> m_GoalLocations;
    protected override void Awake()
    {
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


        //Currently select from a finite selection of goals (Only vehicles atm)
        //TODO: Sidechannel to select based upon key value from the attribute class of objects in scene
        int goalSelectionIndex = (int)envParams.GetWithDefault("goalSelectionIndex", 0f);

        //string key = "MODEL_NAME";
        string value = "Tocus.prefab";
        if (goalSelectionIndex == 0)
            value = "Tocus.prefab";
        else if (goalSelectionIndex == 1)
            value = "sedan1.prefab";
        else if (goalSelectionIndex == 2)
            value = "Car1.prefab";
        else if (goalSelectionIndex == 3)
            value = "Car2.prefab";
        else if (goalSelectionIndex == 4)
            value = "City Bus.prefab";
        else if (goalSelectionIndex == 5)
            value = "Sporty_Hatchback.prefab";
        else
            value = "SEDAN.prefab";


        //Hard coded from the CSV vehicle importer 
        double X_offset = 390187.070616464F;
        double Y_offset = 5818754.16470284F;

        m_GoalType = value;
        m_GoalLocations = new List<Vector3>();

        //Reading CSV to get goal vehicles
        string[] entries = vehicleCSV.text.Split('\n');

        Debug.Log("ObjectNavTask: vehicleCSV length: " + entries.Length);
        //Remove all nonprintable characters
        for (int i = 0; i < entries.Length; i++)
            foreach (char c in entries[i])
                if (c < 32)
                    entries[i] = entries[i].Replace("" + c, "");
        string[] labels = entries[0].Split(',');

        for (int j  = 0; j < entries.Length; j++)
        {
            string s = entries[j];
            if (j > 0 && !s.Equals(""))
            {
                string[] values;
                values = s.Split(',');

                if (values.Length == labels.Length)
                {
                    AttributeClass.Attribute[] entryAttributes = new AttributeClass.Attribute[labels.Length];
                    for (int k = 0; k < entryAttributes.Length; k++)
                    {
                        entryAttributes[k] = new AttributeClass.Attribute(labels[k], values[k]);
                    }

                    if (AttributeClass.GetValueForKeyFromAttributeArray("MODEL_NAME", entryAttributes) == value)
                    {
                        Debug.Log("ObjectNavTask: " + s);
                        //RaycastHit hit;
                        Vector3 goalPosition = new Vector3(((float)(double.Parse(AttributeClass.GetValueForKeyFromAttributeArray("X", entryAttributes)) - X_offset)),
                                                            100,
                                                           ((float)(double.Parse(AttributeClass.GetValueForKeyFromAttributeArray("Y", entryAttributes)) - Y_offset)));
                        //TODO: Find y need only works when the terrian is active (TerrianUtils) or if
                        //there is terrian collider present (raycast)
                        //35 is the approximate y where all vehicles are
                        goalPosition.y = 35;

                        m_GoalLocations.Add(goalPosition);
                    }
                }
            }
        }


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

        car.transform.position = m_NavigationArea.GetRandomLocationOnNavMesh(agent); //Place agent in random acceptable position
        car.transform.rotation = Quaternion.identity;// m_NavigationArea.GetRandomUprightRotation(); // make agent upright
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

        //GameObject goalObjectParent = GameObject.Find("ParkedVehicles");
        m_goalPoint = new GameObject[m_GoalLocations.Count];
        for (int j = 0; j < m_GoalLocations.Count; j++)
        {
            m_goalPoint[j] = Instantiate(goalPointPrefab, m_GoalLocations[j], Quaternion.identity, gameObject.transform);
            if (segmentationSetup != null)
                segmentationSetup.SetupGameObjectSegmentation(m_goalPoint[j]);
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
