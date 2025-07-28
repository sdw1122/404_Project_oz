using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Photon.Pun;
using Unity.Cinemachine;
using System.Collections;

public class PlayerController : MonoBehaviour
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
    public float runSpeed;
    public float mouseSensitivity = 0.5f;

    
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float xRotation = 0f;

    public bool isGrounded = false; // 땅에 닿아있는지 여부
    public bool isCharge = false;

    public bool canMove = true;

    private Vector3 moveDirection = Vector3.zero;
    private CharacterController controller;

    public bool jumpPressed = false;

    public float moveSpeed = 10f;
    public float jumpPower = 8f;
    public float gravity = 20f;
    public float slideSpeed = 5f;

    public string job;

    private Coroutine slowCoroutine;
    private bool isKnockbacked = false;
    private float knockbackEndTime = 0f;
    private float originalSpeed;
    private Rigidbody rb;
    CapsuleCollider col;
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
        col=GetComponent<CapsuleCollider>();
        animator = GetComponent<Animator>();        
        pv = GetComponent<PhotonView>();
        cineCam = GetComponentInChildren<CinemachineCamera>();
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        runSpeed = 1.5f * walkSpeed;
        if (!pv.IsMine)
        {
            var input = GetComponent<PlayerInput>();
            if (input != null) input.enabled = false;
            if (playerCamera != null) playerCamera.gameObject.SetActive(false); 
            enabled = false;
            cineCam.gameObject.SetActive(false);
            return;
        }
        healingRay =GetComponent<HealingRay>();
       
        
        
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
        pv.RPC("RPC_SetMove",RpcTarget.Others,moveValue);
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
        if (context.started && !jumpPressed)
        {
            jumpPressed = true;
        }
    }

    [PunRPC]
    void RPC_SetFloat(bool floating)
    {
        animator.SetBool("Float", floating);
    }

    [PunRPC]
    void RPC_SetMove(float moveValue)
    {
        animator.SetFloat("Move",moveValue);
    }
    public void OnSprint(InputAction.CallbackContext context)
    {
        if (!pv.IsMine || !canMove) return;
        if (context.started || context.performed)
        {
            moveSpeed = runSpeed;
        }
        else if (context.canceled)
        {
            moveSpeed = walkSpeed;
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
    {   if (!pv.IsMine) return;
        if (context.performed)
        {
            healingRay.FireHealingRay();
        }
    }
    void Start()
    {
        moveSpeed = walkSpeed;
        originalSpeed = moveSpeed;
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
       /* if (!pv.IsMine) return;
        // 넉백중이면 update 금지!
        if (isKnockbacked)
        {
            moveDirection = Vector3.zero;
            if (Time.time > knockbackEndTime)
            {
                isKnockbacked = false;
               
                controller.enabled = true;

            }
            return;
        }

        // 1. 입력처리 & 이동벡터 산출
        if (controller.isGrounded)
        {
            // 로컬좌표 기준(forward/right)으로 방향 벡터 생성
            Vector3 inputDir = new Vector3(moveInput.x, 0, moveInput.y);
            inputDir = Vector3.ClampMagnitude(inputDir, 1f); // 대각선 속도 보정
            moveDirection = transform.TransformDirection(inputDir) * moveSpeed;

            if (jumpPressed)
            {
                moveDirection.y = jumpPower;
                jumpPressed = false;
                animator.SetBool("Float", true);
                pv.RPC("RPC_SetFloat", RpcTarget.Others, true);
            }
            else
            {
                animator.SetBool("Float", false);
                pv.RPC("RPC_SetFloat", RpcTarget.Others, false);
            }
        }

        isGrounded = controller.isGrounded;

        HandleSlope();

        // 2. 중력 적용
        moveDirection.y -= gravity * Time.deltaTime;

        // 3. 이동
     
            controller.Move(moveDirection * Time.deltaTime);*/




    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!pv.IsMine) return;
        // 넉백중이면 update 금지!
        if (isKnockbacked)
        {
            moveDirection = Vector3.zero;
            if (Time.time > knockbackEndTime)
            {
                isKnockbacked = false;
                rb.useGravity = false;
                gravity = 20f;
                col.enabled = false;
                controller.enabled = true;

            }
            
            return;
        }
        else
        {
            
            rb.useGravity = false;
            gravity = 20f;
            col.enabled = false;
            controller.enabled = true;
        }
        
        // 1. 입력처리 & 이동벡터 산출
        if (controller.isGrounded)
        {
            // 로컬좌표 기준(forward/right)으로 방향 벡터 생성
            
            Vector3 inputDir = new Vector3(moveInput.x, 0, moveInput.y);
            inputDir = Vector3.ClampMagnitude(inputDir, 1f); // 대각선 속도 보정
            moveDirection = transform.TransformDirection(inputDir) * moveSpeed;

            if (jumpPressed)
            {
                moveDirection.y = jumpPower;
                jumpPressed = false;
                animator.SetBool("Float", true);
                pv.RPC("RPC_SetFloat", RpcTarget.Others, true);
            }
            else
            {
                animator.SetBool("Float", false);
                pv.RPC("RPC_SetFloat", RpcTarget.Others, false);
            }
        }

        isGrounded = controller.isGrounded;

        HandleSlope();

        // 2. 중력 적용
        moveDirection.y -= gravity * Time.deltaTime;

        // 3. 이동

        controller.Move(moveDirection * Time.deltaTime);
        // 넉백중이면 update 금지!
        if (isKnockbacked)
        {
            if (Time.time > knockbackEndTime)
            {
                isKnockbacked = false;
            }
            return;
        }

        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -40f, 60f);

        if (playerCamera != null)
            playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        cameraTarget.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            transform.Rotate(Vector3.up * mouseX);
    }
    // 넉백 함수
    [PunRPC]
    public void StartKnockback(Vector3 knockbackForce,float duration)
    {
        Debug.Log("넉백 호출됨");
        // 플레이어 현재 속도와 무관하게
        rb.linearVelocity = Vector3.zero;

        // 넉백 힘 적용
        isKnockbacked = true;
        gravity = 0f;
        rb.useGravity = true;
        
        controller.enabled = false;
        col.enabled = true;
        rb.AddForce(knockbackForce, ForceMode.VelocityChange);
        Debug.Log("넉백 힘 : "+knockbackForce);
        
        knockbackEndTime = Time.time + duration;
    }
    // 슬로우 함수
    [PunRPC]
    public void RPC_ApplyMoveSpeedDecrease(float amount,float duration)
    {
        if(!pv.IsMine) return;
        if (slowCoroutine != null)
        {
            StopCoroutine(slowCoroutine);
        }
        slowCoroutine = StartCoroutine(slowRoutine(amount,duration));
    }
    private IEnumerator slowRoutine(float amount,float duration)
    {
        float slowFactor = 1f - amount;
        moveSpeed = originalSpeed * (1f-amount);
        /*runSpeed =1.5f*originalSpeed * (1f - amount);*/
        walkSpeed =originalSpeed * (1f-amount);
        runSpeed =originalSpeed * (1f-amount);
        Debug.Log("속도 감소 완료 :"+ moveSpeed);
        yield return new WaitForSeconds(duration);
        moveSpeed = originalSpeed;
        walkSpeed = originalSpeed;
        runSpeed = originalSpeed * 1.5f;
        slowCoroutine = null;
        Debug.Log("속도 복구 완료 :"+moveSpeed);
    }
    //
    public void ResetMoveInput()
    {
        moveInput = Vector2.zero;
        //animator.SetBool("isMove", false);
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

    //void OnControllerColliderHit(ControllerColliderHit col)
    //{
    //    if (col.gameObject.layer != LayerMask.NameToLayer("Enemy"))
    //        return;

    //    Vector3 normal = col.normal;
    //    float verticalAngle = Vector3.Angle(normal, Vector3.up);

    //    // 1) 위에서 착지했을 때 (점프 착지) 보정하지 않음
    //    if (verticalAngle < 45f)
    //    {
    //        // 경사면 충돌로 간주 → 슬라이드 로직만 처리
    //        return;
    //    }

    //    // 2) 옆면에서 서로 밀고 있는 상황만 보정
    //    float horizontalAngle = Vector3.Angle(normal, Vector3.forward);
    //    // 예: forward 기준 0°–90° 사이면 옆면 충돌
    //    if (horizontalAngle > 15f && horizontalAngle < 165f)
    //    {
    //        Rigidbody enemyRb = col.gameObject.GetComponent<Rigidbody>();
    //        if (enemyRb != null)
    //        {
    //            enemyRb.linearVelocity = Vector3.ProjectOnPlane(enemyRb.linearVelocity, normal);
    //        }
    //    }
    //}

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.gameObject.CompareTag("Enemy"))
        {
            // 경사도·노멀 값 구해서
            Vector3 slideDir = Vector3.ProjectOnPlane(Vector3.down, hit.normal).normalized;
            controller.Move(slideDir * slideSpeed * Time.deltaTime);
            // 또는 isGrounded 강제 해제 등
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            isKnockbacked = false;
        }
    }
    bool OnTooSteepGround(out Vector3 groundNormal)
    {
        RaycastHit hit;
        // 캐릭터 발밑에서 짧은 Raycast
        if (Physics.Raycast(transform.position, Vector3.down,
                            out hit, controller.height * 0.6f))
        {
            groundNormal = hit.normal;
            float angle = Vector3.Angle(groundNormal, Vector3.up);
            return angle > controller.slopeLimit;
        }
        groundNormal = Vector3.up;
        return false;
    }

    void HandleSlope()
    {
        Vector3 n;
        if (controller.isGrounded && OnTooSteepGround(out n))
        {
            ///* 방법 A : 접지 해제 → 중력으로 굴러떨어짐 */
            //moveDirection.y = 0f;              // 위로 기운 속도 제거
            //controller.Move(Vector3.zero); // 먼저 위치 갱신
            //isGrounded = false;          // 내부 플래그(직접 쓰는 경우)

            /* 방법 B : 다운-슬라이드 강제 */
            Vector3 slide = Vector3.ProjectOnPlane(Vector3.down, n).normalized * slideSpeed;
            moveDirection = slide;
        }
    }
    
}
