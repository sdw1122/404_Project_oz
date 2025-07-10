using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float mouseSensitivity = 0.2f;
    public Transform cameraTransform;

    private Rigidbody rb;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float xRotation = 0f;

    public float jumpForce = 5f;
    private bool isGrounded = false; // 땅에 닿아있는지 여부
    private int groundContactCount = 0; // 여러 지면 접촉을 처리

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (value.isPressed && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false; // 점프하면 공중 상태로 변경
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            groundContactCount++;
            isGrounded = true;
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            groundContactCount--;
            if (groundContactCount <= 0)
            {
                isGrounded = false;
                groundContactCount = 0;
            }
        }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked; // 마우스 커서 고정
        Cursor.visible = false; // 커서 숨김        
    }

    private void Update()
    {
        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }
    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 move = forward * moveInput.y + right * moveInput.x;
        rb.AddForce(move * moveSpeed, ForceMode.Acceleration);
    }
    public bool IsGrounded()
    {
        return isGrounded;
    }
}
