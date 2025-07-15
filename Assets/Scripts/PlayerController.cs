using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Photon.Pun;

public class PlayerController : MonoBehaviour
{
    PhotonView pv;
    Animator animator;

    [SerializeField] private Camera playerCamera;
    public float walkSpeed = 10f;
    public float runSpeed = 15f;
    public float mouseSensitivity = 0.5f;
    private float currentSpeed;

    private Rigidbody rb;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float xRotation = 0f;

    public float jumpForce = 5f;
    private bool isGrounded = false; // 땅에 닿아있는지 여부
    private int groundContactCount = 0; // 여러 지면 접촉을 처리

    public bool canMove = true;

    public string job;
    [PunRPC]
    public void SetJob(string _job)
    {
        job = _job;
        Debug.Log($"[PlayerController] Job 설정됨: {job}");
    }

    [PunRPC]
    void SendMyDataToHost()
    {
        if (!pv.IsMine) return;

        PlayerSaveData myData = new PlayerSaveData
        {
            userId = PhotonNetwork.LocalPlayer.UserId,
            userJob = job,
            position = transform.position,
            // 필요시 추가 데이터
        };
        GameObject gm = GameObject.Find("GameManager");
        PhotonView gmView = gm.GetComponent<PhotonView>();
        string json = JsonUtility.ToJson(myData);
        gmView.RPC("ReceivePlayerData", RpcTarget.MasterClient, json);
    }

    private void Awake()
    {
        pv = GetComponent<PhotonView>();
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        if (!pv.IsMine)
        {
            var input = GetComponent<PlayerInput>();
            if (input != null) input.enabled = false;
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (!pv.IsMine || !canMove) return;
        moveInput = context.ReadValue<Vector2>();

        float moveValue = 0f;
        // 앞으로(앞, 좌, 우, 앞+좌, 앞+우, 좌, 우) → 1
        if (moveInput.y > 0.1f || Mathf.Abs(moveInput.x) > 0.1f && moveInput.y >= -0.1f)
            moveValue = 1f;
        // 뒤로(뒤, 뒤+좌, 뒤+우) → -1
        else if (moveInput.y < -0.1f)
            moveValue = -1f;
        // 가만히
        else
            moveValue = 0f;

        animator.SetFloat("Move", moveValue);
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        if (!pv.IsMine) return;
        lookInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (!pv.IsMine) return;
        if (context.started && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
            animator.SetTrigger("Jump");
        }
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (context.started || context.performed)
        {
            currentSpeed = runSpeed;
        }
        else if (context.canceled)
        {
            currentSpeed = walkSpeed;
        }
    }

    void Start()
    {
        currentSpeed = walkSpeed;
        if (!pv.IsMine)
        {
            if (playerCamera != null)
                playerCamera.gameObject.SetActive(false);
            return;
        }
        if (playerCamera != null)
            playerCamera.gameObject.SetActive(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (!pv.IsMine) return;

        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        if (playerCamera != null)
            playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        transform.Rotate(Vector3.up * mouseX);
    }
    // Update is called once per frame
    void FixedUpdate()
    {
        if (!pv.IsMine) return;

        Vector3 forward = playerCamera.transform.forward;
        Vector3 right = playerCamera.transform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 move = forward * moveInput.y + right * moveInput.x;
        Vector3 desiredVelocity = move * currentSpeed;
        desiredVelocity.y = rb.linearVelocity.y; // 점프 등 Y속도 유지

        rb.linearVelocity = desiredVelocity;
    }
    public void ResetMoveInput()
    {
        moveInput = Vector2.zero;
        //animator.SetBool("isMove", false);
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

    public bool IsGrounded()
    {
        return isGrounded;
    }
}
