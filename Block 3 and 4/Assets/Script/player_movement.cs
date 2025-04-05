using UnityEngine;

public class Player : MonoBehaviour
{
    public float moveSpeed = 2f;               // How fast the player moves
    public float mouseSensitivity = 1000f;      // How fast the mouse moves the view
    public Transform cameraTransform;          // The camera we use
    public float jumpHeight = 2f;              // How high the player can jump

    private float xRotation = 0f;              // The up and down view angle
    private Rigidbody rb;                      // The player's Rigidbody

    private bool isGrounded;                   // Whether the player is on the ground

    void Start()
    {
        rb = GetComponent<Rigidbody>(); // Get the Rigidbody component
    }

    void Update()
    {
        // Player movement
        float moveInputX = Input.GetAxis("Horizontal");
        float moveInputZ = Input.GetAxis("Vertical");
        Vector3 move = transform.right * moveInputX + transform.forward * moveInputZ;
        transform.position += move * moveSpeed * Time.deltaTime;

        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Turn player left or right
        transform.Rotate(Vector3.up * mouseX);

        // Look up and down with camera
        xRotation -= mouseY; // Move view up when mouse moves up
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // Limit the up and down angle
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Jump logic
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpHeight, ForceMode.Impulse); // Apply an upward force to make the player jump
        }
    }

    // Check if the player is touching the ground
    private void OnCollisionStay(Collision collision)
    {
        isGrounded = true;
    }

    private void OnCollisionExit(Collision collision)
    {
        isGrounded = false;
    }
}
