using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

/*
 * So far this only tests the different physics configurations of the car,
 * to make sure that they are configured correctly when the mode is chosen via Academy
 */
public class TestSkyCarPhysics : MonoBehaviour
{
    DynamicSceneLoader m_dsl;
    EnvironmentParameters m_envParams;
    Car m_car;
    bool m_done = false;
    int m_count;

    void Awake()
    {
        Log("Started");
        m_count = 0;
        m_envParams = Academy.Instance.EnvironmentParameters;

        m_dsl = FindObjectOfType<DynamicSceneLoader>();

        if (m_dsl == null)
            LogError("Dynamic Scene Loader not found");
    }

    void Update()
    {
        if (m_dsl == null || m_done) return;

        GameObject car = m_dsl.agent;

        if (car == null) return;

        int agentCarPhysics = (int)m_envParams.GetWithDefault("agentCarPhysics", 0);
        TestPhysicsMode(car, agentCarPhysics);
        m_done = true;

        if(m_count > 0)
        {
            Log("Failed with " + m_count + " errors");
        }
        else
            Log("Passed");
    }

    // returns true only if tests besides component check were run
    bool TestPhysicsMode(GameObject a, int mode)
    {
        Log("Mode: " + mode);

        if (!CheckCarComponent(a, mode)) return false;

        switch (mode)
        {
            case 0: // simple physics
                break;

            case 1: // wheel torque, but disable sideslip, TC, downforce, springs
                if(m_car.carController.m_SteerHelper != 1f)
                    LogError("SteerHelper incorrectly disabled");

                if (m_car.carController.m_TractionControl != 0f)
                    LogError("TractionControl incorrectly enabled");

                if (m_car.carController.m_Downforce != 0f)
                    LogError("Downforce incorrectly enabled");

                CheckSuspension(false);

                if (m_car.carController.varyFrictionEnabled != false)
                    LogError("Surface Friction incorrectly enabled");
                break;

            case 2: // add suspension, downforce, sideslip
                if (m_car.carController.m_SteerHelper > 0)
                    LogError("SteerHelper incorrectly enabled");

                if (m_car.carController.m_TractionControl != 0f)
                    LogError("TractionControl incorrectly enabled");

                if (m_car.carController.m_Downforce == 0f)
                    LogError("Downforce incorrectly disabled");

                CheckSuspension(true);

                if (m_car.carController.varyFrictionEnabled != false)
                    LogError("Surface Friction incorrectly enabled");
                break;

            case 10: // full physics model
                if (m_car.carController.m_SteerHelper > 0)
                    LogError("SteerHelper incorrectly enabled");

                if (m_car.carController.m_TractionControl != 1f)
                    LogError("TractionControl incorrectly disabled");

                if (m_car.carController.m_Downforce == 0f)
                    LogError("Downforce incorrectly disabled");

                CheckSuspension(true);

                if (m_car.carController.varyFrictionEnabled != true)
                    LogError("Surface Friction incorrectly disabled");
                break;

            default: // unknown mode
                LogError("Unknown mode");
                break;
        }

        return true;
    }

    // returns false if missing component(s)
    bool CheckCarComponent(GameObject a, int mode)
    {
        switch (mode)
        {
            case 0: // simple physics
                CarSimple cs = a.GetComponent<CarSimple>();
                if (cs == null)
                {
                    LogError("CarSimple not attached to GameObject");
                    return false;
                }
                break;

            case 1: // normal physics modes
            case 2:
            case 10: 
                m_car = a.GetComponent<Car>();
                if (m_car == null)
                {
                    LogError("Car not attached to GameObject");
                    return false;
                }

                SkyCarController controller = m_car.carController;
                Log(controller.ToString());
                if (controller == null)
                {
                    LogError("SkyCarController not attached to GameObject");
                    return false;
                }
                break;
        }
        return true;
    }

    bool CheckSuspension(bool enabled)
    {
        WheelCollider[] wheels = m_car.carController.GetWheelColliders();
        for(int i = 0; i < wheels.Length; i++)
        {
            //Log("SuspensionDistance = " + wheels[i].suspensionDistance);
            if(enabled && wheels[i].suspensionDistance == 0)
            {
                LogError("Suspension incorrectly disabled");
                return false;
            }
            else if(!enabled && wheels[i].suspensionDistance != 0)
            {
                LogError("Suspension incorrectly enabled");
                return false;
            }
        }
        return true;
    }

    void Log(string text) {
        Debug.Log("SkyCarPhysics> " + text);
    }

    void LogError(string text) {
        Debug.LogError("SkyCarPhysics> " + text);
        m_count++;
    }
}
