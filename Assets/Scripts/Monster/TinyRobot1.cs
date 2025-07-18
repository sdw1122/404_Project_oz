using Photon.Pun;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class TinyRobot1 : Enemy
{    
    public float jumpAttackRange = 3f;
    public float jumpPower = 0.1f;
    public float jumpAttackCooldown = 2f;

    private float lastJumpAttackTime = -99f;
    private bool isJumpAttacking = false;

    public override bool CanAct()
    {
        // 점프 공격 중에는 부모에서 Attack() 못하도록
        return !isJumpAttacking;
    }


    public override void Attack()
    {
        Debug.Log("Attack 실행");
        if (targetEntity == null || dead) return;
        if (Time.time < lastJumpAttackTime + jumpAttackCooldown) return;
        if (isJumpAttacking) return;

        if (targetEntity != null)
        {
            Vector3 lookPos = targetEntity.transform.position - transform.position;
            lookPos.y = 0; // 수평 방향만 고려
            if (lookPos != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(lookPos);
        }        

        isJumpAttacking = true;
        lastJumpAttackTime = Time.time;
        enemyAnimator.SetTrigger("JumpAttack");
        pv.RPC("RPC_JumpAttack", RpcTarget.Others);
        Debug.Log("공격");

        if (navMeshAgent != null && navMeshAgent.enabled)
            navMeshAgent.enabled = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 dir = (targetEntity.transform.position - transform.position).normalized;
            dir.y = 0.5f; // 위쪽 성분 추가
            rb.linearVelocity = Vector3.zero;
            rb.AddForce((dir + Vector3.up) * jumpPower, ForceMode.VelocityChange);
        }
    }
    [PunRPC]
    public void RPC_JumpAttack()
    {
        enemyAnimator.SetTrigger("JumpAttack");
    }

    public void OnJumpAttackHit()
    {
        if (targetEntity != null && !targetEntity.dead && isJumpAttacking)
        {
            float hitDist = Vector3.Distance(transform.position, targetEntity.transform.position);
            if (hitDist <= jumpAttackRange + 0.3f) // 약간 오차 허용
            {
                // 대미지 적용
                float damage = this.damage; // Enemy에서 상속받은 공격력 변수 사용
                Vector3 hitPoint = targetEntity.transform.position;
                Vector3 hitNormal = transform.position - targetEntity.transform.position;

                targetEntity.OnDamage(damage, hitPoint, hitNormal);
            }
        }
        isJumpAttacking = false;
        // 공격 종료 후 NavMeshAgent 다시 켜주기!
        if (navMeshAgent != null && !navMeshAgent.enabled)
            navMeshAgent.enabled = true;

        Debug.Log("공격 끝");
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isJumpAttacking)
        {
            // 플레이어와 충돌했을 때만 작동 (tag 또는 레이어로 식별)
            if (collision.gameObject.CompareTag("Player"))
            {
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.constraints |= RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
                    rb.linearVelocity = Vector3.zero;    // 모든 이동 즉시 멈춤
                    rb.angularVelocity = Vector3.zero;
                }
                isJumpAttacking = false;

                // NavMeshAgent 복구 (돌진 멈춘 후 바로 AI 이동 재개)
                if (navMeshAgent != null && !navMeshAgent.enabled)
                {
                    navMeshAgent.enabled = true;
                    navMeshAgent.Warp(transform.position); // 현재 위치로 Pathfinder 동기화
                }
            }
        }
    }

}
