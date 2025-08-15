using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Photon.Pun;
using Unity.Cinemachine;

public class TestControll : MonoBehaviour
{

    PhotonView pv;
    Animator animator;
    HealingRay healingRay;
    [SerializeField] private Camera playerCamera;
    public CinemachineCamera cineCam;
    public Transform cameraTarget;
    public GameObject playerObj;
    public Camera mainCamera;
    public GameObject deadCamera;
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


    public float moveSpeed = 5f;
    public float jumpPower = 8f;
    public float gravity = 20f;

    private Vector3 moveDirection = Vector3.zero;
    private CharacterController controller;

    private bool jumpPressed = false;


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
            userJob = job
            // 필요시 추가 데이터
        };
        GameObject gm = GameObject.Find("GameManager");
        PhotonView gmView = gm.GetComponent<PhotonView>();
        string json = JsonUtility.ToJson(myData);
        gmView.RPC("ReceivePlayerData", RpcTarget.MasterClient, json);
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        pv = GetComponent<PhotonView>();
        cineCam = GetComponentInChildren<CinemachineCamera>();
        if (!pv.IsMine)
        {
            var input = GetComponent<PlayerInput>();
            if (input != null) input.enabled = false;
            if (playerCamera != null) playerCamera.gameObject.SetActive(false); // <-- 이걸 꼭 추가
            enabled = false;
            cineCam.gameObject.SetActive(false);
            return;
        }
        healingRay = GetComponent<HealingRay>();

        playerObj = this.gameObject;
        /*mainCamera = transform.Find("Main Camera").GetComponent<Camera>();*/
        deadCamera = transform.Find("Dead Camera")?.gameObject;
        Debug.Log($"[{pv.ViewID}] 내 카메라 이름: {playerCamera.name}, 활성 상태: {playerCamera.enabled}");
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
        pv.RPC("RPC_Move", RpcTarget.Others, moveValue);
    }

    [PunRPC]
    void RPC_Move(float moveValue)
    {
        animator.SetFloat("Move", moveValue);
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        if (!pv.IsMine) return;
        lookInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (!pv.IsMine || !canMove) return;
        if (context.started)
            jumpPressed = true;
    }

    [PunRPC]
    void RPC_SetFloat(bool floating)
    {
        animator.SetBool("Float", floating);
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (!pv.IsMine || !canMove) return;
        if (context.started || context.performed)
        {
            currentSpeed = runSpeed;
        }
        else if (context.canceled)
        {
            currentSpeed = walkSpeed;
        }
    }

    public void OnResurrection(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (!pv.IsMine) return;
            PlayerHealth deadPlayer = FindClosestDeadPlayer();
            if (deadPlayer != null && deadPlayer.dead)
            {
                Debug.Log($"[Resurrection] {deadPlayer.name}에게 RPC 전송"); // 확인용 로그
                deadPlayer.Resurrection();
            }
            else
            {
                Debug.Log("[Resurrection] 죽은 플레이어가 없음");
            }
        }


    }
    public void ResetSpeed()
    {
        moveInput = Vector2.zero;
    }
    public void OnHealRay(InputAction.CallbackContext context)
    {
        if (!pv.IsMine) return;
        if (context.performed)
        {
            healingRay.FireHealingRay();
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


        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (!pv.IsMine) return;
        bool floating = !isGrounded;
        animator.SetBool("Float", floating);
        // 필요하면 네트워크로 동기화
        pv.RPC("RPC_SetFloat", RpcTarget.Others, floating);

        // 1. 입력처리 & 이동벡터 산출
        if (controller.isGrounded)
        {
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");

            // 로컬좌표 기준(forward/right)으로 방향 벡터 생성
            Vector3 inputDir = new Vector3(moveInput.x, 0, moveInput.y);
            inputDir = Vector3.ClampMagnitude(inputDir, 1f); // 대각선 속도 보정
            moveDirection = transform.TransformDirection(inputDir) * moveSpeed;

            // 점프 입력
            if (jumpPressed)
            {
                moveDirection.y = jumpPower;
                jumpPressed = false; // 점프 입력 소진
            }
        }

        // 2. 중력 적용
        moveDirection.y -= gravity * Time.deltaTime;

        // 3. 이동
        controller.Move(moveDirection * Time.deltaTime);
    }
    // Update is called once per frame
    void FixedUpdate()
    {
        if (!pv.IsMine) return;

        //Vector3 forward = playerCamera.transform.forward;
        //Vector3 right = playerCamera.transform.right;
        //forward.y = 0f;
        //right.y = 0f;
        //forward.Normalize();
        //right.Normalize();

        //Vector3 move = forward * moveInput.y + right * moveInput.x;
        //Vector3 desiredVelocity = move * currentSpeed;
        //desiredVelocity.y = rb.linearVelocity.y; // 점프 등 Y속도 유지

        //rb.linearVelocity = desiredVelocity;
        float mouseX = lookInput.x * mouseSensitivity;  
        float mouseY = lookInput.y * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -40f, 60f);

        if (playerCamera != null)
            playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        cameraTarget.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
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
    PlayerHealth FindClosestDeadPlayer()
    {
        PlayerHealth closestDeadPlayer = null;
        float minDistance = float.MaxValue;

        // 모든 PlayerHealth 컴포넌트를 찾습니다.
        PlayerHealth[] allPlayers = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);

        foreach (PlayerHealth player in allPlayers)
        {
            // 자기 자신은 제외하고, 죽어있으며, 펜 플레이어인지 확인 (직업 구분 로직 필요)
            if (player.pv.IsMine) continue; // 자기 자신은 제외
            if (!player.dead) continue; // 죽어있지 않으면 제외


            // 현재는 모든 죽은 플레이어를 대상으로 합니다.

            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestDeadPlayer = player;
            }
        }
        // 일정 거리 이상이면 부활 불가? --추가기능
        /*if (minDistance > 5f) return null; // 예시: 5유닛 이상 떨어져 있으면 부활 불가*/
        return closestDeadPlayer;
    }
    public bool IsGrounded()
    {
        return isGrounded;
    }

    public void ActivateCamera()
    {
        Debug.Log("ActivateCamera 실행됨! mainCamera:" + mainCamera + " (active=" + mainCamera.GetComponent<Camera>() + ")" +
           ", deadCamera:" + deadCamera + " (active=" + deadCamera?.activeSelf + ")", this);

        mainCamera.gameObject.SetActive(false);

        deadCamera.SetActive(true);
        deadCamera.GetComponent<Camera>().enabled = true;

        deadCamera.transform.parent = null;

        Vector3 parentPos = playerObj.transform.position;
        deadCamera.transform.position = new Vector3(parentPos.x, parentPos.y + 3f, parentPos.z);
    }

    public void Deactivate()
    {
        deadCamera.SetActive(false);
        deadCamera.transform.parent = playerObj.transform;

        mainCamera.gameObject.SetActive(true);
    }

    void OnCollisionStay(Collision col)
    {
        if (col.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            Rigidbody EnemyRb = col.gameObject.GetComponent<Rigidbody>();
            if (EnemyRb != null && !rb.isKinematic)
            {
                // 플레이어가 몬스터를 뚫으려 움직일 때, 그 움직임을 상쇄
                EnemyRb.linearVelocity = Vector3.ProjectOnPlane(EnemyRb.linearVelocity, col.GetContact(0).normal);
            }
        }
    }
}
