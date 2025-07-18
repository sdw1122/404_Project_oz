using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;
using Photon.Realtime;

public class Hammer : MonoBehaviour
{
    Rigidbody rb;
    private PlayerController playerController;
    Animator animator;
    PhotonView pv;

    public GameObject weapon;
    public float damage = 40f;

    private bool isAttackButtonPressed = false;
    private float attackDelay = 1.0f;
    private float attackTimer = 0f;
    private float delayTimer = 0f;
    private int attackLayerIndex;

    public float skill1;
    private float skill1HoldTime = 0;
    public float skill1CoolDown; //10초

    public float skill2 = 60f;
    private float skill2CoolDown = 18f;         // Skill2 쿨타임(초)
    public float skill2CoolDownTimer = 18f;    // 현재 쿨타임 진행상태(초)

    private InputSystem_Actions controls;
    private bool skill1Pressed = false;

    private bool isAttacking = false;      // 공격 중 여부
    private int attackStep = 1;            // 1: 왼쪽, 2: 오른쪽
    private float timeSinceLastAttack = 0f;// 마지막 공격 이후 경과 시간
    private bool canAttack = true;         // 공격 가능 여부

    private void Awake()
    {        
        rb = GetComponent<Rigidbody>();
        playerController = GetComponent<PlayerController>();
        controls = new InputSystem_Actions();
        skill1CoolDown = 10f;
        animator = GetComponent<Animator>();
        pv = GetComponent<PhotonView>();
        attackLayerIndex = animator.GetLayerIndex("Upper Body");
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (!pv.IsMine) return;
        if (context.started)
        {
            isAttackButtonPressed = true;            
        }
        if (context.canceled)
            isAttackButtonPressed = false;
    }

    public void OnSkill1(InputAction.CallbackContext context)
    {
        if (context.started && skill1CoolDown >= 10f && playerController.IsGrounded())
        {
            skill1Pressed = true;
        }
        if (context.canceled && skill1Pressed && playerController.IsGrounded())
        {
            skill1Pressed = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            skill1 = 0;
            if (skill1HoldTime < 1)
            {
                skill1 = 0;
                skill1CoolDown = 5f;
                skill1HoldTime = 0f;
                Debug.Log("0charging");
                animator.SetTrigger("CancelCharge");
                return;
            }
            else if (skill1HoldTime < 2)
            {
                Debug.Log("1charging");
                skill1 = damage * 2;
            }
            else if (skill1HoldTime < 3)
            {
                Debug.Log("2charging");
                skill1 = damage * 4;
            }
            else
            {
                Debug.Log("3charging");
                skill1 = damage * 8;
            }            
            skill1HoldTime = 0;
        }
    }

    public void OnSkill2(InputAction.CallbackContext context)
    {
        if (context.performed && skill2CoolDownTimer >= skill2CoolDown && playerController.IsGrounded())
        {
            Debug.Log("UsingSkill2");
            animator.SetTrigger("Big Attack");            
            skill2CoolDownTimer = 0f;
        }
    }

    void Update()
    {
        if (skill1Pressed)
        {
            skill1HoldTime += Time.deltaTime;
            playerController.canMove = false;
            playerController.ResetMoveInput();
            animator.SetTrigger("Charge");
        }
        else
        {
            playerController.canMove = true;
        }

        if (skill1CoolDown != 10f)
        {
            skill1CoolDown += Time.deltaTime;
        }
        else if (skill1CoolDown >= 10f)
        {
            skill1CoolDown = 10f;
        }

        if (skill2CoolDownTimer < skill2CoolDown)
        {
            skill2CoolDownTimer += Time.deltaTime;
            if (skill2CoolDownTimer > skill2CoolDown)
                skill2CoolDownTimer = skill2CoolDown;
        }

        // 버튼을 누르고 있는 동안만 타이머 증가
        if (isAttackButtonPressed && playerController.IsGrounded())
        {
            attackTimer += Time.deltaTime;
            if (attackTimer >= attackDelay)
            {
                Attack();
                attackTimer = 0f;
            }
        }
        else
        {
            // 버튼을 뗐거나 공중이면, 쿨타임이 끝날 때까지 공격 불가
            attackTimer = Mathf.Min(attackTimer + Time.deltaTime, attackDelay);
        }

        // 마지막 공격 이후 시간 업데이트
        if (!isAttacking)
            timeSinceLastAttack += Time.deltaTime;
    }

    public void Attack()
    {
        if (!canAttack || !playerController.IsGrounded())
            return;
        Debug.Log("Attacking");
        // 2초 이상 공격 안 했으면 1타로 초기화
        if (timeSinceLastAttack >= 2f)
            attackStep = 1;

        // 애니메이션 필요        
        animator.SetLayerWeight(attackLayerIndex, 1f);
        animator.SetTrigger("Attack");        

        // 다음 공격 스텝으로 전환
        attackStep = (attackStep == 1) ? 2 : 1;

        // 타이머 초기화
        timeSinceLastAttack = 0f;
    }

    public void UpperAniEnd()
    {
        animator.SetLayerWeight(attackLayerIndex, 0.01f); // 기본값으로 복귀
    }

    public void ApplySkiil1()
    {
        Skill1(skill1);
    }

    void Skill1(float damage)
    {
        // 박스 중심: 플레이어 앞쪽 2.5만큼 (원하는 거리로 조정)
        Vector3 boxCenter = transform.position + transform.forward * 2.5f;
        // 박스의 반 크기: 가로 2.5, 높이 1, 깊이 2.5 (전체 크기: 5x2x5)
        Vector3 boxHalfExtents = new Vector3(2.5f, 1f, 2.5f);
        // 박스 회전: 플레이어의 회전과 동일
        Quaternion boxOrientation = transform.rotation;
        // "Enemy" 레이어만 감지 (레이어를 사용하지 않는다면 생략 가능)
        int layerMask = LayerMask.GetMask("Enemy");

        // 박스 범위 내의 "Enemy"레이어만 감지
        Collider[] hitColliders = Physics.OverlapBox(boxCenter, boxHalfExtents, boxOrientation, layerMask);

        Vector3 center = transform.position + transform.forward * 1.5f;

        foreach (var hit in hitColliders)
        {
            if (hit.gameObject.layer == LayerMask.NameToLayer("Enemy"))
            {
                // 적의 Collider에서 가장 가까운 점(=공격 중심에서 적까지의 ClosestPoint)
                Vector3 hitPoint = hit.ClosestPoint(center);
                // 공격 방향(=적 표면의 법선 벡터)
                Vector3 hitNormal = (hitPoint - center).normalized;

                Enemy enemy = hit.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.OnDamage(damage, hitPoint, hitNormal);
                    Debug.Log("Attack맞음");
                }
            }
        }
        animator.SetTrigger("Charge Attack");
        skill1CoolDown = 0;
    }

    public void ApplySkill2()
    {
        Skill2();
    }

    void Skill2()
    {
        float range = 5f;         // 공격 거리(반경)
        float angle = 90f;        // 부채꼴 각도(도 단위)
        float attackDamage = skill2;

        // 1. 전방 구 범위 내 적 감지
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, range, LayerMask.GetMask("Enemy"));

        foreach (var hit in hitColliders)
        {
            // 2. 플레이어 → 적 방향 벡터
            Vector3 dirToTarget = (hit.transform.position - transform.position).normalized;
            // 3. 전방 벡터와 각도 비교
            float dot = Vector3.Dot(transform.forward, dirToTarget);
            float theta = Mathf.Acos(dot) * Mathf.Rad2Deg;

            if (theta <= angle / 2f)
            {
                //4.부채꼴 안에 들어온 적에게만 데미지
                Enemy enemy = hit.GetComponent<Enemy>();
                if (enemy != null)
                {
                    // 피격 위치와 방향 계산
                    Vector3 hitPoint = hit.ClosestPoint(transform.position);
                    Vector3 hitNormal = (hitPoint - transform.position).normalized;

                    enemy.OnDamage(attackDamage, hitPoint, hitNormal);
                }
                Debug.Log("Skill2 맞음");
            }
        }
    }
    
    // 정해진 애니메이션 타이밍에 호출
    public void ApplyAttack()
    {
        DealDamageInSwing();
    }

    void DealDamageInSwing()
    {
        // 플레이어 위치와 방향
        Vector3 playerPos = transform.position;
        Vector3 playerForward = transform.forward;
        Vector3 playerRight = transform.right;
        Vector3 playerUp = transform.up;

        // 오프셋 (x: 오른쪽, y: 위, z: 전방)
        Vector3 offset = new Vector3(0f, 1.1f, 1f); // 필요에 따라 값 조정

        // 오프셋을 월드 좌표로 변환
        Vector3 worldOffset = playerRight * offset.x + playerUp * offset.y + playerForward * offset.z;

        // 박스 중심 좌표
        Vector3 swingCenter = playerPos + worldOffset;

        Vector3 halfExtents = new Vector3(1f, 1f, 1f); // 필요에 따라 값 조정
        Quaternion orientation = transform.rotation;
        int layerMask = LayerMask.GetMask("Enemy");

        Collider[] hits = Physics.OverlapBox(swingCenter, halfExtents, orientation, layerMask);
        foreach (var hit in hits)
        {
            if (hit.gameObject.layer == LayerMask.NameToLayer("Enemy"))
            {
                Debug.Log("기본 공격 적중: " + hit.gameObject.name);

                // Enemy 스크립트 가져와서 데미지 주기
                Enemy enemy = hit.GetComponent<Enemy>();
                if (enemy != null)
                {
                    // 피격 위치와 방향 계산
                    Vector3 hitPoint = hit.ClosestPoint(transform.position);
                    Vector3 hitNormal = (hitPoint - transform.position).normalized;

                    enemy.OnDamage(damage, hitPoint, hitNormal); // damage는 원하는 값으로
                }
            }
        }
    }


    void DrawSkill2ConeGizmo()
    {
        float range = 5f;      // Skill2 범위
        float angle = 90f;     // Skill2 각도

        int segments = 30;     // 부채꼴을 나눌 선 개수
        Vector3 origin = transform.position;
        Vector3 forward = transform.forward;

        Gizmos.color = new Color(0, 0.5f, 1, 0.3f);

        // 중심선
        Gizmos.DrawLine(origin, origin + forward * range);

        // 부채꼴의 양 끝 방향 벡터
        Quaternion leftRot = Quaternion.AngleAxis(-angle / 2, Vector3.up);
        Quaternion rightRot = Quaternion.AngleAxis(angle / 2, Vector3.up);
        Vector3 leftDir = leftRot * forward;
        Vector3 rightDir = rightRot * forward;

        Gizmos.DrawLine(origin, origin + leftDir * range);
        Gizmos.DrawLine(origin, origin + rightDir * range);

        // 부채꼴 호 그리기
        Vector3 prevPoint = origin + leftDir * range;
        for (int i = 1; i <= segments; i++)
        {
            float lerp = (float)i / segments;
            Quaternion rot = Quaternion.AngleAxis(Mathf.Lerp(-angle / 2, angle / 2, lerp), Vector3.up);
            Vector3 point = origin + (rot * forward) * range;
            Gizmos.DrawLine(prevPoint, point);
            prevPoint = point;
        }
    }

    // skill 범위 시각화
    void OnDrawGizmosSelected()
    {
        if (skill1Pressed)
        {
            Vector3 boxCenter = transform.position + transform.forward * 2.5f;
            Vector3 boxHalfExtents = new Vector3(2.5f, 1f, 2.5f); //이 범위 조절
            Quaternion boxOrientation = transform.rotation;

            Gizmos.color = new Color(1, 0, 0, 0.5f);
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(boxCenter, boxOrientation, Vector3.one);
            Gizmos.matrix = rotationMatrix;
            Gizmos.DrawCube(Vector3.zero, boxHalfExtents * 2);
        }
        DrawSkill2ConeGizmo();

        if (weapon != null)
        {
            //Vector3 gizmo = transform.position; //플레이어 위치 가져오기
            //Vector3 offset = new Vector3(0f, 1f, 1f); //x, y, z만큼 움직이기
            //Vector3 gizmocentor = gizmo + offset;

            //Vector3 boxHalfExtents = new Vector3(1f, 0.6f, 0.1f); //x, y, z 크기

            //Quaternion orientation = weapon.transform.rotation;

            //Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.4f);
            //Matrix4x4 rotationMatrix = Matrix4x4.TRS(gizmo, orientation, Vector3.one);
            //Gizmos.matrix = rotationMatrix;
            //Gizmos.DrawCube(Vector3.zero, boxHalfExtents * 2);
            //Gizmos.matrix = Matrix4x4.identity;

            // 플레이어 위치와 방향
            Vector3 playerPos = transform.position;
            Vector3 playerForward = transform.forward;
            Vector3 playerRight = transform.right;
            Vector3 playerUp = transform.up;

            // 오프셋 (x: 오른쪽, y: 위, z: 전방)
            Vector3 offset = new Vector3(0f, 1f, 1f);

            // 오프셋을 월드 좌표로 변환
            Vector3 worldOffset = playerRight * offset.x + playerUp * offset.y + playerForward * offset.z;

            // 박스 중심 좌표
            Vector3 gizmoCenter = playerPos + worldOffset;

            // 박스 크기와 회전
            Vector3 boxHalfExtents = new Vector3(1f, 1f, 1f);
            Quaternion orientation = transform.rotation;

            // 기즈모 그리기
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.4f);
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(gizmoCenter, orientation, Vector3.one);
            Gizmos.matrix = rotationMatrix;
            Gizmos.DrawCube(Vector3.zero, boxHalfExtents * 2);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}