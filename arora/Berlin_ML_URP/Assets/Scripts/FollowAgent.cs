using UnityEngine;

public class FollowAgent : MonoBehaviour
{
    // The target we are following
    public Transform target;
    private GameObject MLArea;

    private float wantedRotationAngle;
    public Vector3 offsetPosition = new Vector3(0, 5, -5);
    public float cameraSensitivity = 90;
    bool controlEnabled = false;
    public bool keysEnabled = true;
    private float rotationX = 0.0f;
    private float rotationY = 0.0f;
    public bool debugMode = false;

    private void Start()
    {
        if (MLArea)
            target = MLArea.transform.GetChild(0).Find("LookAt");
    }

    private void Update()
    {
        if(debugMode && !target)
        {
            MLArea = GameObject.Find("MLArea");
            if (MLArea)
                target = MLArea.transform.GetChild(0).Find("LookAt");
        }

        if (keysEnabled)
        {
            if (Input.GetKeyDown("t"))
                Toggle();
            else if (Input.GetKeyDown("r"))
                Reset();
        }

        if (controlEnabled)
        {
            rotationX += Input.GetAxis("Mouse X") * cameraSensitivity * Time.deltaTime;
            rotationY += Input.GetAxis("Mouse Y") * cameraSensitivity * Time.deltaTime;
            rotationY = Mathf.Clamp(rotationY, -45, 90);
        }
    }

    public void Toggle()
    {
        controlEnabled = !controlEnabled;
        if (controlEnabled) Cursor.lockState = CursorLockMode.Locked;
        else Cursor.lockState = CursorLockMode.None;
    }

    public void Reset()
    {
        rotationX = rotationY = 0;
    }

    private void LateUpdate()
    {
        if (!target) return;

        wantedRotationAngle = target.eulerAngles.y + rotationX;
        float zoom = 1 + (rotationY / 100);

        transform.position = target.position + (Quaternion.Euler(0, wantedRotationAngle, 0) * offsetPosition * zoom);

        // Always look at the target
        transform.LookAt(target);
    }
}