using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class Hammer : MonoBehaviour
{
    Rigidbody rb;
    private PlayerController playerController;

    public GameObject weapon;
    public float damage = 40f;

    private bool isAttackButtonPressed = false;
    private float attackDelay = 1.0f;
    private float attackTimer = 0f;
    private float delayTimer = 0f;

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

        //기본 공격
        controls.Player.Attack.started += ctx => isAttackButtonPressed = true;
        controls.Player.Attack.canceled += ctx => isAttackButtonPressed = false;

        // Skill1 시작(누름)
        controls.Player.Skill1.started += ctx =>
        {
            if (skill1CoolDown >= 10f && playerController.IsGrounded())
            {
                skill1Pressed = true;
            }

        };

        // Skill1 해제(뗌)
        controls.Player.Skill1.canceled += ctx =>
        {
            if (skill1Pressed && playerController.IsGrounded())
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

                Skill1(skill1);
                skill1HoldTime = 0;
            }

        };

        // Skill2 (Q키)
        controls.Player.Skill2.performed += ctx =>
        {
            if (skill2CoolDownTimer >= skill2CoolDown && playerController.IsGrounded())
            {
                Debug.Log("UsingSkill2");
                Skill2();
                skill2CoolDownTimer = 0f; // 쿨타임 시작
            }
        };
    }

    void OnEnable()
    {
        controls.Enable();
    }

    void OnDisable()
    {
        controls.Disable();
    }
    void Update()
    {
        if (skill1Pressed)
        {
            skill1HoldTime += Time.deltaTime;
            rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY
                | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
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

    private void Attack()
    {
        if (!canAttack || !playerController.IsGrounded())
            return;
        Debug.Log("Attacking");
        // 2초 이상 공격 안 했으면 1타로 초기화
        if (timeSinceLastAttack >= 2f)
            attackStep = 1;

        // 애니메이션 필요
        //isAttacking = true;
        //attackTimer = 0f;
        //canAttack = false;

        // 데미지 즉시 적용
        DealDamageInSwing();

        // 다음 공격 스텝으로 전환
        attackStep = (attackStep == 1) ? 2 : 1;

        // 타이머 초기화
        timeSinceLastAttack = 0f;
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

        foreach (var hit in hitColliders)
        {
            // 적에게 데미지 주기 (Enemy 스크립트에 TakeDamage 함수가 있다고 가정)
            //Enemy enemy = hit.GetComponent<Enemy>();
            //if (enemy != null)
            //{
            //    enemy.TakeDamage(attackDamage);
            //}
            Debug.Log("맞음");
        }
        skill1CoolDown = 0;
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
                // 4. 부채꼴 안에 들어온 적에게만 데미지
                //Enemy enemy = hit.GetComponent<Enemy>();
                //if (enemy != null)
                //{
                //    enemy.TakeDamage(attackDamage);
                //}
                Debug.Log("Skill2 맞음");
            }
        }
    }

    void DealDamageInSwing()
    {
        // 해머 위치 기준 범위 감지(예시)
        Vector3 swingCenter = weapon.transform.position + weapon.transform.forward * (weapon.transform.localScale.z / 2f);
        Vector3 halfExtents = (weapon.transform.localScale / 2f) * 2f;
        Quaternion orientation = weapon.transform.rotation;
        int layerMask = LayerMask.GetMask("Enemy");

        Collider[] hits = Physics.OverlapBox(swingCenter, halfExtents, orientation, layerMask);
        foreach (var hit in hits)
        {
            //Enemy enemy = hit.GetComponent<Enemy>();
            //if (enemy != null) enemy.TakeDamage(damage);
            Debug.Log("기본 공격 적중");
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
            // 공격 범위 중심 계산 (해머 위치 + 오른쪽 방향 * 1.5)
            Vector3 swingCenter = weapon.transform.position + weapon.transform.forward * (weapon.transform.localScale.z / 2f);
            Vector3 halfExtents = weapon.transform.localScale / 2f;
            Quaternion orientation = weapon.transform.rotation;

            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.4f); // 노란색 반투명
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(swingCenter, orientation, Vector3.one);
            Gizmos.matrix = rotationMatrix;
            Gizmos.DrawCube(Vector3.zero, halfExtents * 4);
            Gizmos.matrix = Matrix4x4.identity; // 매트릭스 원복
        }
    }
}