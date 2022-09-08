using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class CarAgent : NavAgent
{
    [HideInInspector]
    public Car car;
    [HideInInspector]
    public CarSimple carSimple;
    public int physicsMode = 0;
    public CarControls car_controls;
    public bool manual_input = false;

    float prev_steering;

    public override void Initialize()
    {
        ResetCarValues();

        base.Initialize();
        car = GetComponentInChildren<Car>();
        if (car)
            car.SetEnableApi(!manual_input);
        else
            carSimple = GetComponentInChildren<CarSimple>();
    }

    public void Start() // this has to execute prior to SkyCarController.Start()
    {
        switch (physicsMode)
        {
            case 1: // wheel torque, but disable sideslip, TC, downforce, springs
                car.carController.m_SteerHelper = 1f; // effectively negates sideslip
                car.carController.m_TractionControl = 0f;
                car.carController.m_Downforce = 0f;
                car.carController.suspensionSpringsEnabled = false;
                car.carController.varyFrictionEnabled = false;
                break;
            case 2: // add suspension, downforce, sideslip
                car.carController.m_SteerHelper = 0f;
                car.carController.m_TractionControl = 0f;
                car.carController.varyFrictionEnabled = false;
                break;
            case 10: // full physics model
                car.carController.m_SteerHelper = 0f;
                break;
            default: // simple physics
                break;
        }
    }

    public override void OnEpisodeBegin()
    {
        if(!Academy.Instance.IsCommunicatorOn)
            m_Task.m_NavigationArea.ResetArea("episode");
        ResetCarValues();
        Debug.Log("Episode beginning");
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        //Debug.Log("Collecting observations for " + sensor?.GetName() + ". Timescale: " + Time.timeScale);

        base.CollectObservations(sensor);
        //sensor.AddObservation(transform.position);
        //sensor.AddObservation(transform.rotation);
        //sensor.AddObservation(Task.Instance.m_goalPoint.transform.position);
    }

    public override void OnActionReceived(ActionBuffers vectorAction)
    {
        //vectorAction.ContinuousActions[0]: -1 to 1 for throttle
        //vectorAction.ContinuousActions[1]: -1 to 1 for steering
        //vectorAction.ContinuousActions[2]: -1 to 1 for braking (NOTE: we scale this to 0 to 1 before sending to the physics controller)
        float throttle = Mathf.Clamp(vectorAction.ContinuousActions[0], -1f, 1f);
        float steering = Mathf.Clamp(vectorAction.ContinuousActions[1], -1f, 1f);
        float brake    = (Mathf.Clamp(vectorAction.ContinuousActions[2], -1f, 1f) + 1) / 2f;

        if(relativeSteering)
        {
            steering = Mathf.Clamp(prev_steering + steering, -1f, 1f);
            prev_steering = steering;
        }

        if (car)
        {
            car_controls.throttle = throttle;
            car_controls.steering = steering;
            car_controls.brake    = brake;
        
            car.SetCarControls(car_controls);
        }
        else // simple movement
        {
            carSimple.SetCarControls(throttle, steering, brake);
        }
        //print("Agent Position" + this.transform.position);
        m_Task.StepReward(this);
        m_Task.NoViablePathReward(this);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        //Debug.LogWarning("Heuristic method called but not implemented. Returning placeholder actions.");
    }

    void OnCollisionEnter(Collision collision)
    {
        m_Task.CollisionRewards(this, collision);
    }

    void OnCollisionStay(Collision collision)
    {
        m_Task.CollisionRewards(this, collision);
    }

    void ResetCarValues()
    {
        prev_steering = 0;
    }
}
