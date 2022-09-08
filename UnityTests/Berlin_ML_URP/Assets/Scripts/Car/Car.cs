using Unity.MLAgents;
using UnityEngine;
using UnityEngine.AI;

/*
* Uses the SkyCarController component to move the car based on Unity physics.
* The car can be controlled either through keyboard or through client api calls.
*/

public class Car : Vehicle
{
    EnvironmentParameters m_EnvParams;
    [HideInInspector] public SkyCarController carController;

    private CarControls carControls;

    protected float steering, throttle, footBrake, handBrake;
    float wheelTorque, reverseWheelTorque, normalBrake;
    bool earlyFixedUpdateOccurred = false;
    bool m_started = false;

    private void Awake()
    {
        carController = GetComponent<SkyCarController>();
    }

    private void Start()
    {
        m_EnvParams = Academy.Instance.EnvironmentParameters;
        NavMeshAgent navMeshAgent = GetComponent<NavMeshAgent>();
        if(navMeshAgent != null)
        {
            float useNavMeshAgent = m_EnvParams.GetWithDefault("useNavMeshAgent", 0);
            navMeshAgent.enabled = useNavMeshAgent == 1;
        }

        normalBrake = carController.m_BrakeTorque;
        wheelTorque = carController.m_FullTorqueOverAllWheels / 4;
        reverseWheelTorque = carController.m_ReverseTorque / 4;

        m_started = true;
    }

    private void FixedUpdate()
    {
        if(!earlyFixedUpdateOccurred)
        { 
            if (isApiEnabled)
            {
                steering = carControls.steering;
                throttle = carControls.throttle;
                handBrake = carControls.handbrake ? 1 : 0;
                footBrake = carControls.brake;
                //footBrake = throttle; // need this to reverse without using brake inputs like manual control
                                        // the demo scripts assume this for some reason
            }
            else
            {
                steering = Input.GetAxis("Horizontal");
                throttle = Input.GetAxis("Vertical");
                handBrake = Input.GetAxis("Jump");
                footBrake = Input.GetAxis("Fire1");
            }

            // ignore footbrake if attempting to accelerate from a stationary position
            // (added to overcome training difficulties for I3M paper due to Unity's Wheel Colliders ignoring throttle when any brakes are applied)
            if (carController.m_Rigidbody.velocity.magnitude < 0.0001f && footBrake * normalBrake < (throttle > 0 ? throttle * wheelTorque : -throttle * reverseWheelTorque))
                carController.m_BrakeTorque = 0;
            else
                carController.m_BrakeTorque = normalBrake;

            carController.Move(steering, throttle, footBrake, handBrake);
        }

        earlyFixedUpdateOccurred = false;
    }

    public override bool SetCarControls(CarControls controls)
    {
        SetCarControls(controls, ref carControls);

        if (!m_started) return false; // need to initialize variables before starting updates

        FixedUpdate();
        earlyFixedUpdateOccurred = true;

        return true;
    }

    public static void SetCarControls(CarControls src, ref CarControls dst)
    {
        dst.brake = src.brake;
        dst.gear_immediate = src.gear_immediate;
        dst.handbrake = src.handbrake;
        dst.is_manual_gear = src.is_manual_gear;
        dst.manual_gear = src.manual_gear;
        dst.steering = src.steering;
        dst.throttle = src.throttle;
    }
}