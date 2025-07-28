using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WoodManAttack : MonoBehaviour
{
    [Header("벌목 공격 설정")]
    public float meleeAttackRange = 2f; // 근접 공격 범위
    public static float meleeAttackDamage = 40f; 
    public Transform attackPoint; // 공격 원점
    public float attackArcAngle = 180f; // 공격 부채꼴 각도
    public float attackDelayBeforeHit = 0.2f;
    public float currentAttackCooldown = 3f;
    private Animator animator; 
    private float lastAttackTime;
    int layerMask;
    private WoodMan woodMan;
    private void Awake()
    {
        animator = GetComponent<Animator>();
        woodMan = GetComponent<WoodMan>();
        layerMask = LayerMask.NameToLayer("Player");
       
    }
    public bool IsReady()
    {

        return Time.time >= lastAttackTime + currentAttackCooldown;
        
    }
    public void SetDamage(float value)
    {
        meleeAttackDamage = value;
    }
    public bool IsInMeleeRange(float targetPos)
    {
        return targetPos <= meleeAttackRange;
    }
    [PunRPC]
    public void PerformMeleeAttackRPC()
    {
        
         
          
           if (Time.time < lastAttackTime + currentAttackCooldown)
           {
                
                return;
           }
           lastAttackTime = Time.time;
        
        

        // 공격 애니메이션 트리거 
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        // 데미지를 주는건 마스터만
        
    }
    private void ApplyDamage()
    {


        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }
        Collider[] hitColliders = Physics.OverlapSphere(attackPoint.position, meleeAttackRange, woodMan.whatIsTarget);

        foreach (Collider hitCollider in hitColliders)
        {
            LivingEntity targetLivingEntity = hitCollider.GetComponent<LivingEntity>();
            
            if (targetLivingEntity == null) continue;

            Vector3 directionToTarget = (targetLivingEntity.transform.position - attackPoint.position);
            directionToTarget.y = 0; 
            directionToTarget.Normalize(); 

            
            Vector3 flatForward = transform.forward; 
            flatForward.y = 0; // Y축 무시
            flatForward.Normalize();

            float angleToTarget = Vector3.Angle(flatForward, directionToTarget);
            

            // 반원으로 데미지 적용
            if (angleToTarget <= attackArcAngle / 2f)
            {
                

                // hitPoint와 hitNormal 계산 
                Vector3 damageHitPoint = hitCollider.ClosestPoint(attackPoint.position);
                Vector3 damageHitNormal = (damageHitPoint - attackPoint.position).normalized;
              

                
                targetLivingEntity.OnDamage(meleeAttackDamage, damageHitPoint, damageHitNormal);

                
                
                
            }
            else
            {
                Debug.Log($"[WoodManAttack] 부채꼴 밖에 있음: {hitCollider.name}, 각도: {angleToTarget}도.");
            }
        }


    }
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.red;
        // OverlapSphere의 탐지 범위 시각화
        Gizmos.DrawWireSphere(attackPoint.position, meleeAttackRange);

        Gizmos.color = Color.yellow;
        // 부채꼴의 중심선 방향 (WoodMan의 정면 방향을 기준으로)
        Vector3 forwardDir = transform.forward;
        forwardDir.y = 0f;
        forwardDir.Normalize();

        // 부채꼴 시각화
        Gizmos.DrawRay(attackPoint.position, forwardDir * meleeAttackRange); // 중심선

        // 부채꼴의 왼쪽 경계선
        Quaternion leftRayRotation = Quaternion.AngleAxis(-attackArcAngle / 2f, Vector3.up);
        Vector3 leftRayDirection = leftRayRotation * forwardDir * meleeAttackRange;
        Gizmos.DrawRay(attackPoint.position, leftRayDirection);

        // 부채꼴의 오른쪽 경계선
        Quaternion rightRayRotation = Quaternion.AngleAxis(attackArcAngle / 2f, Vector3.up);
        Vector3 rightRayDirection = rightRayRotation * forwardDir * meleeAttackRange;
        Gizmos.DrawRay(attackPoint.position, rightRayDirection);

        // 부채꼴의 호(Arc) 시각화 (선택 사항, 더 나은 시각화를 위해)
        int segments = 20; // 호를 그릴 세그먼트 수
        Vector3 previousPoint = attackPoint.position + leftRayDirection;
        for (int i = 0; i <= segments; i++)
        {
            float angle = -attackArcAngle / 2f + (i / (float)segments) * attackArcAngle;
            Quaternion segmentRotation = Quaternion.AngleAxis(angle, Vector3.up);
            Vector3 segmentDirection = segmentRotation * forwardDir * meleeAttackRange;
            Vector3 currentPoint = attackPoint.position + segmentDirection;
            Gizmos.DrawLine(previousPoint, currentPoint);
            previousPoint = currentPoint;
        }
    }
}
