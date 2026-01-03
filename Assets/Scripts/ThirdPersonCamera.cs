using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;          // Player, dynamically assigned
    public Vector3 offset = new Vector3(0, 3, -6);
    public float snapAngle = 45f;     // Angle to snap per key press
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

        // Q and E input for snappy turning left and right
        if (Input.GetKeyDown(KeyCode.Q))
        {
            yaw -= snapAngle;
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            yaw += snapAngle;
        }

        // Player center
        CharacterController controller = target.GetComponent<CharacterController>();
        Vector3 targetCenter = target.position;
        if (controller != null)
            targetCenter += controller.center;

        // Desired camera position
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 desiredPosition = targetCenter + rotation * offset;

        // Collision check
        Vector3 dir = desiredPosition - targetCenter;
        float distance = dir.magnitude;

        float minDistance = 1f;
        RaycastHit hit;
        bool hitSomething = Physics.SphereCast(
            targetCenter,
            collisionRadius,
            dir.normalized,
            out hit,
            distance,
            collisionLayers,
            QueryTriggerInteraction.Ignore
        );

        if (hitSomething)
        {
            float hitDist = hit.distance - (collisionRadius + 0.05f);
            // Clamp to minDistance
            float finalDist = Mathf.Max(hitDist, minDistance);
            desiredPosition = targetCenter + dir.normalized * finalDist;
        }
        else
        {
            // Clamp to minDistance if not hitting anything
            if (distance < minDistance)
                desiredPosition = targetCenter + dir.normalized * minDistance;
        }

        // Smooth movement
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Look at center
        transform.LookAt(targetCenter);
    }
}
