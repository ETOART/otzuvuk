using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FPS : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 9f;

    [Header("Mouse Look")]
    [SerializeField, Range(0.1f, 10f)] private float mouseSensitivity = 2f;
    [SerializeField] private Camera playerCamera;

    [Header("Physics")]
    [SerializeField] private float gravity = -9.81f; // negative value
    [SerializeField] private float groundedStick = -2f; // small downward force to keep grounded

    private CharacterController controller;
    private float xRotation = 0f;
    private float verticalVelocity = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        if (playerCamera == null)
            Debug.LogWarning("FPS: No Camera assigned or found as child. Assign a Camera to playerCamera.");

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleLook();
        HandleMove();
        // escape unlock for convenience
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        if (playerCamera != null)
            playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        transform.Rotate(Vector3.up * mouseX);
    }

    private void HandleMove()
    {
        float targetSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;

        Vector3 input = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
        Vector3 desiredMove = transform.TransformDirection(input);
        if (desiredMove.sqrMagnitude > 1f) desiredMove.Normalize();
        Vector3 move = desiredMove * targetSpeed;

        if (controller.isGrounded)
        {
            verticalVelocity = groundedStick; // keep grounded
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        move.y = verticalVelocity;
        controller.Move(move * Time.deltaTime);
    }

    // Optional: call to explicitly lock cursor again (useful from UI)
    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
