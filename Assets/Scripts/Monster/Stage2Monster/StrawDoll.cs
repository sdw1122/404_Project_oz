using UnityEngine;
using System.Collections;
using Photon.Pun;

public class StrawDoll : Enemy
{
    public float jumpPower = 10f;
    public float jumpUpPower = 5f;
    
    public bool Atk = true;
    public override void Attack()
    {
        if (Atk)
        {
            pv.RPC("RPC_SetNavMesh", RpcTarget.All, false);
            StartCoroutine(JumpAttackRoutine());
            Atk = false;
        }
    }

    IEnumerator JumpAttackRoutine() 
    {
        if (isAttacking || dead) yield break;
        int repeatCount = 3;
        float interval = 1f;

        for (int i = 0; i < repeatCount; i++)
        {
            if (dead) yield break;
            // -- 1초마다 피격 이펙트 실행 (부모/RPC이벤트 호출)
            pv.RPC("RPC_PlayHitEffect", RpcTarget.All, transform.position, transform.forward);
            yield return new WaitForSeconds(interval); // 1초 대기
        }

        if (!isAttacking)
        {
            Debug.Log("dead : " + dead);
            Attacking();
            isAttacking = true;
        }
    }

    public IEnumerator DieBoom()
    {
        if (!dead) yield break;
        int repeatCount = 3;
        float interval = 1f;

        for (int i = 0; i < repeatCount; i++)
        {
            // -- 1초마다 피격 이펙트 실행 (부모/RPC이벤트 호출)
            pv.RPC("RPC_PlayHitEffect", RpcTarget.All, transform.position, transform.forward);
            yield return new WaitForSeconds(interval); // 1초 대기
        }

        Boom();
    }

    public void Attacking()
    {
        if (dead) return;
        if (targetEntity != null)
        {
            Vector3 lookPos = targetEntity.transform.position - transform.position;
            lookPos.y = 0; // 수평 방향만 고려
            if (lookPos != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(lookPos);
        }
        // 점프 애니메이션 등 실행
        pv.RPC("RPC_PlayJumpAttack", RpcTarget.All);
        pv.RPC("RPC_StrawDollAttack", RpcTarget.All, targetEntity.transform.position);
    }

    [PunRPC]
    public void RPC_PlayJumpAttack()
    {
        enemyAnimator.SetTrigger("Attack");
    }

    [PunRPC]
    public void RPC_StrawDollAttack(Vector3 targetPos)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // 1. 목표 위치까지의 방향(수평) 벡터
            Vector3 dir = (targetPos - transform.position);
            dir.y = 0; // 수평 방향

            // 2. 노멀라이즈
            Vector3 forwardDir = dir.normalized;

            // 3. 수직 힘과 결합 (y축 힘은 별도)
            Vector3 jumpVec = forwardDir * jumpPower + Vector3.up * jumpUpPower;

            // 4. 기존 속도 제거(있으면)
            rb.linearVelocity = Vector3.zero;

            // 5. 힘 가하기 (VelocityChange: 곧장 속도값 변경)
            rb.AddForce(jumpVec, ForceMode.VelocityChange);
        }
    }

    public void Boom()
    {
        // 1. 중심(보통 transform.position)과 반경(공격 범위)을 정합니다.
        float aoeRadius = 5f; // 예시: 반지름 5
        Vector3 center = transform.position;

        // 2. Physics.OverlapSphere로 범위 내 콜라이더를 모두 찾기
        Collider[] hitColliders = Physics.OverlapSphere(center, aoeRadius, whatIsTarget);

        for (int i = 0; i < hitColliders.Length; i++)
        {
            // 3. 자신을 공격 대상에서 제외(예: 본인 자신 체크)
            if (hitColliders[i].gameObject == gameObject)
                continue;

            // 4. LivingEntity 등 원하는 컴포넌트만 판정
            LivingEntity entity = hitColliders[i].GetComponent<LivingEntity>();
            if (entity != null && !entity.dead)
            {
                // 5. 대미지/상태 효과 등 적용
                entity.OnDamage(damage, hitColliders[i].ClosestPoint(center), center - hitColliders[i].transform.position);
            }
        }
        DieMotion();
    }

    [PunRPC]

    public void RPC_SetNavMesh(bool active)
    {
        Rigidbody rb = gameObject.GetComponent<Rigidbody>();
        navMeshAgent.enabled = active;
        rb.isKinematic = active;
    }
}
