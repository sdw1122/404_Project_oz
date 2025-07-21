using Photon.Pun;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class TinyRobot1 : Enemy
{    
    public float jumpAttackRange = 1f;
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
        Rigidbody rb = GetComponent<Rigidbody>();
        pv.RPC("RPC_SetNavMesh", RpcTarget.All, false);
        pv.RPC("RPC_RobotAttack", RpcTarget.All, targetEntity.transform.position);

    }

    [PunRPC]
    public void RPC_RobotAttack(Vector3 targetPos)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
        if (rb != null)
        {
            Vector3 dir = (targetPos - transform.position).normalized;
            dir.y = 0.5f; // 위쪽 성분 추가
            rb.linearVelocity = Vector3.zero;

            rb.AddForce(dir * jumpPower, ForceMode.VelocityChange);
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
        Rigidbody rb = GetComponent<Rigidbody>();
        pv.RPC("RPC_SetNavMesh", RpcTarget.All, true);

        Debug.Log("공격 끝");
    }

    void OnCollisionStay(Collision col)
    {
        if (col.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            Rigidbody playerRb = col.gameObject.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                // 플레이어가 몬스터를 뚫으려 움직일 때, 그 움직임을 상쇄
                playerRb.linearVelocity = Vector3.ProjectOnPlane(playerRb.linearVelocity, col.GetContact(0).normal);
            }
        }

        // 반대로 플레이어 스크립트에도 몬스터 만나면 같은 로직 적용
    }

    [PunRPC]

    public void RPC_SetNavMesh(bool active)
    {
        Rigidbody rb = gameObject.GetComponent<Rigidbody>();
        navMeshAgent.enabled = active;
        rb.isKinematic = active;
    }
}
