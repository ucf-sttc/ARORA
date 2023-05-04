using UnityEngine;

public class SmoothFollowAgent : MonoBehaviour {
    public bool shouldRotate = true;

    // The target we are following
    private Transform target;
    public GameObject MLArea;

    private float wantedRotationAngle;
    private float currentRotationAngle;

    private Quaternion currentRotation;
    private new Camera camera;

    public Vector3 offsetPosition;

    private void Start() {
        //offsetPosition = new Vector3(0, 5, -10);

        camera = GetComponentInChildren<Camera>();

        if (MLArea)
            target = MLArea.transform.GetChild(0).Find("LookAt");
    }

    private void FixedUpdate()
    {
        camera.Render();
    }

    private void LateUpdate() {
        if (!target) {
            return;
        }

        // Calculate the current rotation angles
        wantedRotationAngle = target.eulerAngles.y;
        currentRotationAngle = transform.eulerAngles.y;

        // Damp the rotation around the y-axis
        currentRotationAngle = Mathf.LerpAngle(currentRotationAngle, wantedRotationAngle, 0.3f);

        // Convert the angle into a rotation
        currentRotation = Quaternion.Euler(0, currentRotationAngle, 0);

        // Set the position of the camera on the x-z plane to:
        // distance meters behind the target
        transform.position = target.position + (currentRotation * offsetPosition);

        // Always look at the target
        transform.LookAt(target);
    }
}