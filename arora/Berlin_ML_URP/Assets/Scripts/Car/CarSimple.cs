using Unity.MLAgents;
using UnityEngine;
using UnityEngine.AI;

/*
 * Simplified version of Car/SkyCarController that uses basic Unity move functions instead of driving Wheel Colliders
 */
public class CarSimple : MonoBehaviour
{
    EnvironmentParameters m_EnvParams;
    private float speed = 8f;
    private float maxSteer = 25f;
    private float throttle, steering, braking;
    private Rigidbody rbody;

    bool earlyFixedUpdateOccurred = false;

    void Awake()
    {
        rbody = GetComponent<Rigidbody>();
        throttle = 0;
        steering = 0;
        braking = 0;

        m_EnvParams = Academy.Instance.EnvironmentParameters;
        NavMeshAgent navMeshAgent = GetComponent<NavMeshAgent>();
        if (navMeshAgent != null)
        {
            float useNavMeshAgent = m_EnvParams.GetWithDefault("useNavMeshAgent", 0);
            navMeshAgent.enabled = useNavMeshAgent == 1;
        }
    }

    public void SetCarControls(float throttle, float steering, float braking)
    {
        this.throttle = throttle;
        this.steering = steering;
        this.braking = braking;
        //Debug.LogWarning("SetCarControls: " + throttle + " " + steering + " " + braking + " ");
        FixedUpdate();
        earlyFixedUpdateOccurred = true;
    }

    void FixedUpdate()
    {
        if(!earlyFixedUpdateOccurred)
        {
            //Debug.LogWarning("Agent position" + transform.position);
            //Debug.LogWarning("INPUT: " + throttle + " " + steering + " " + braking + " ");

            float throttleBraking = Mathf.Clamp(Mathf.Abs(throttle) - braking, 0, 1);
            if (throttle < 0) throttleBraking *= -1;

            rbody.MovePosition(transform.position + transform.forward * throttleBraking * speed * Time.fixedDeltaTime);
            if (Mathf.Abs(throttleBraking) > 0)
                rbody.MoveRotation(rbody.rotation * Quaternion.Euler(0, steering * maxSteer * speed / 2 * Time.fixedDeltaTime, 0));
            //Debug.LogWarning("Agent position" + transform.position);
        }
        
        earlyFixedUpdateOccurred = false;
    }
}
