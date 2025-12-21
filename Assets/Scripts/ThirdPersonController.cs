using UnityEngine;

public class ThirdPersonController : MonoBehaviour
{
    public Transform cameraTransform; // dynamically assigned
    public float speed = 5f;
    public float rotationSpeed = 720f;

    private CharacterController controller;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // Assign camera dynamically if not assigned
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        if (cameraTransform == null) return; // safety
        MovePlayer();
    }

    void MovePlayer()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 direction = new Vector3(horizontal, 0, vertical).normalized;
        if (direction.magnitude < 0.1f) return;

        // Movement relative to camera
        float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;
        Vector3 moveDir = Quaternion.Euler(0, targetAngle, 0) * Vector3.forward;

        // Rotate smoothly
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(moveDir), rotationSpeed * Time.deltaTime);

        // Move
        controller.Move(moveDir * speed * Time.deltaTime);

        // Gravity
        controller.Move(Vector3.down * 9.81f * Time.deltaTime);
    }
}
