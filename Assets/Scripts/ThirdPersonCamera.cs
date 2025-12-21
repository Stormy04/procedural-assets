using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;          // Player, dynamically assigned
    public Vector3 offset = new Vector3(0, 3, -6);
    public float rotationSpeed = 5f;
    public float smoothSpeed = 10f;
    public float pitchMin = -20f;
    public float pitchMax = 60f;
    public float collisionRadius = 0.3f;   // radius for camera collision
    public LayerMask collisionLayers;      // walls/floors

    private float yaw = 0f;
    private float pitch = 20f;

    void LateUpdate()
    {
        // Find player if not assigned
        if (target == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
                target = player.transform;
            else
                return;
        }

        // Mouse input
        yaw += Input.GetAxis("Mouse X") * rotationSpeed;
        pitch -= Input.GetAxis("Mouse Y") * rotationSpeed;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 desiredPosition = target.position + rotation * offset;

        // Check for collisions
        RaycastHit hit;
        Vector3 dir = desiredPosition - target.position;
        if (Physics.SphereCast(target.position, collisionRadius, dir.normalized, out hit, dir.magnitude, collisionLayers))
        {
            desiredPosition = hit.point - dir.normalized * collisionRadius;
        }

        // Smooth movement
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Look at player
        transform.LookAt(target.position + Vector3.up * 1.5f);
    }
}
