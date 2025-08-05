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
    public float knockbackGravity = 10f;
    private Vector3 knockbackVelocity = Vector3.zero;
    private float knockbackTimer = 0f;

    private Vector2 moveInput;
    private Vector2 lookInput;
    private float xRotation = 0f;

    public bool isGrounded = false; // 땅에 닿아있는지 여부
    public bool isCharge = false;

    public bool canMove = true;

    public Vector3 moveDirection = Vector3.zero;
    private CharacterController controller;

    public bool jumpPressed = false;

    public float moveSpeed = 10f;
    public float jumpPower = 8f;
    float jumpBufferTime = 0.15f;
    float jumpBufferCounter = 0f;
    public float gravity = 9.81f;
    public float slideSpeed = 5f;

    public string job;

    private Coroutine slowCoroutine;
    private bool isKnockbacked = false;
    private float originalSpeed;
    public ParticleSystem knockbackEffect;
    public ParticleSystem slowEffect;
    public GameObject jumpEffect;
    private GameObject jumpEffectins;

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
        
        animator = GetComponent<Animator>();        
        pv = GetComponent<PhotonView>();
        cineCam = GetComponentInChildren<CinemachineCamera>();
        animator = GetComponent<Animator>();
        
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
        EnemyHealthBarController.LocalPlayerCamera = playerCamera;
        healingRay =GetComponent<HealingRay>();
       
        
        
        playerObj = this.gameObject;
        /*mainCamera = transform.Find("Main Camera").GetComponent<Camera>();*/
        deadCamera = transform.Find("Dead Camera")?.gameObject;
        Debug.Log($"[{pv.ViewID}] 내 카메라 이름: {playerCamera.name}, 활성 상태: {playerCamera.enabled}");
    }
    public void setBind()
    {
        moveInput = Vector2.zero; 
        /*controller.Move(Vector3.zero);*/
        moveDirection = Vector3.zero;
        canMove = false;
        isCharge = true;
        animator.SetFloat("Move", 0f);
        pv.RPC("RPC_SetMove", RpcTarget.Others, 0f);
    }
    public void clearBind()
    {
        canMove = true;
        isCharge = false;

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
        //if (context.started && !jumpPressed)
        //{
        //    jumpPressed = true;
        //}
        if (context.performed)
            jumpBufferCounter = jumpBufferTime;
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

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!pv.IsMine) return;
        // 카메라 회전은 무조건 실행
        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -40f, 60f);

        if (playerCamera != null)
            playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        cameraTarget.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
        // 넉백 
        if (knockbackTimer > 0f)
        {
            
            knockbackVelocity.y -= knockbackGravity * Time.deltaTime;
            controller.Move(knockbackVelocity * Time.deltaTime);
            knockbackTimer -= Time.deltaTime;
            if (controller.isGrounded)
            {
                isKnockbacked = false;
                knockbackVelocity = Vector3.zero;
                knockbackTimer = 0f;
            }
            return;
        }   
        else if (isKnockbacked)
        {
            isKnockbacked = false;
            knockbackVelocity.y -= knockbackGravity * Time.deltaTime;
            controller.Move(knockbackVelocity * Time.deltaTime);

        }

        if (jumpBufferCounter > 0)
            jumpBufferCounter -= Time.deltaTime;

        
        
        // 바인드 상태가 아니면 움직임
        if (canMove)
        {
            if (jumpBufferCounter > 0)
                jumpBufferCounter -= Time.deltaTime;
            // 1. 입력처리 & 이동벡터 산출
            // 로컬좌표 기준(forward/right)으로 방향 벡터 생성
            Vector3 inputDir = new Vector3(moveInput.x, 0, moveInput.y);
            inputDir = Vector3.ClampMagnitude(inputDir, 1f);
            Vector3 worldDir = transform.TransformDirection(inputDir) * moveSpeed;

            //경사 미끄러짐 관리
            Vector3 groundNormal = Vector3.up;
            bool steepSlope = controller.isGrounded && OnTooSteepGround(out groundNormal);
            if (steepSlope)
            {
                // 경사면 전용 이동 처리
                HandleSlopeMovement(groundNormal);
            }
            else
            {
                HandleNormalMovement(worldDir);
            }
            isGrounded = controller.isGrounded;

            controller.Move(moveDirection * Time.deltaTime);
        }
    }
    // 넉백 함수
    [PunRPC]
    public void StartKnockback(Vector3 knockbackForce,float duration)
    {
        Debug.Log("넉백 시작: Force=" + knockbackForce + ", duration=" + duration);
        knockbackVelocity = knockbackForce;
        knockbackTimer = duration;

        isKnockbacked = true;

     
       
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
        //float rayDistance = controller.height / 2 + 0.2f; // 조금 더 여유있게
        //return Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, rayDistance, LayerMask.GetMask("Ground"));
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
    void HandleSlopeMovement(Vector3 groundNormal)
    {
        // 슬라이드 방향과 속도 계산
        float slopeAngle = Vector3.Angle(groundNormal, Vector3.up);
        float slideMultiplier = Mathf.Lerp(1f, 2.5f, (slopeAngle - controller.slopeLimit) / (90f - controller.slopeLimit));

        Vector3 slideDirection = Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized;
        Vector3 slideVelocity = slideDirection * slideSpeed * slideMultiplier;

        moveDirection = slideVelocity;

        // 경사면에서 강제 하향력 적용 (떨림 방지의 핵심!)
        float slopeForce = 5f; // 조절 가능한 값
        moveDirection.y = slideVelocity.y - (controller.height * 0.5f * slopeForce);

        // 추가 하향력으로 지면 밀착 강화
        controller.Move(Vector3.down * slopeForce * Time.deltaTime);

        // 경사면에서는 점프 금지
        animator.SetBool("Float", false);
        pv.RPC("RPC_SetFloat", RpcTarget.Others, false);
    }

    void HandleNormalMovement(Vector3 worldDir)
    {
        moveDirection.x = worldDir.x;
        moveDirection.z = worldDir.z;

        if (controller.isGrounded)
        {
            if (jumpBufferCounter > 0)
            {
                moveDirection.y = jumpPower;
                jumpBufferCounter = 0;
                animator.SetBool("Float", true);
                pv.RPC("RPC_SetFloat", RpcTarget.Others, true);
            }
            else
            {
                moveDirection.y = -0.5f;
                animator.SetBool("Float", false);
                pv.RPC("RPC_SetFloat", RpcTarget.Others, false);
            }
        }
        else
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }
    }
    private IEnumerator DestroyJumpEffect()
    {
        jumpEffectins = Instantiate(jumpEffect, transform.position, Quaternion.identity);
        yield return new WaitForSeconds(0.5f);
        Destroy(jumpEffectins);
    }
}
