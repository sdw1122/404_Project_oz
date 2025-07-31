using Photon.Pun;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class TinyRobot1 : Enemy
{
    [SerializeField] private PhotonTransformView transformView;
    [SerializeField] private PhotonRigidbodyView rigidbodyView;
    [SerializeField] private PhotonView photonViews;

    public float jumpAttackRange = 1f;
    public float jumpPower = 0.1f;
    public float jumpAttackCooldown = 5f;  

    private float lastJumpAttackTime = -99f;    

    public override bool CanAct()
    {
        // 점프 공격 중에는 부모에서 Attack() 못하도록
        return !isAttacking;
    }

    public override void Attack()
    {
        if (dead) return;
        Debug.Log("Attack 실행");
        if (targetEntity == null || dead) return;
        if (Time.time < lastJumpAttackTime + jumpAttackCooldown) return;
        if (isAttacking) return;

        if (targetEntity != null)
        {            
            Vector3 lookPos = targetEntity.transform.position - transform.position;
            lookPos.y = 0; // 수평 방향만 고려
            if (lookPos != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(lookPos);
        }

        isAttacking = true;
        lastJumpAttackTime = Time.time;
        enemyAnimator.SetTrigger("JumpAttack");
        pv.RPC("RPC_JumpAttack", RpcTarget.Others);
        pv.RPC("RPC_SetNavMesh", RpcTarget.All, false);
        pv.RPC("RPC_RobotAttack", RpcTarget.All, targetEntity.transform.position);

    }

    [PunRPC]
    public void RPC_RobotAttack(Vector3 targetPos)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Debug.Log("View : " + transformView);
        Rigidbody rb = GetComponent<Rigidbody>();        
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
        if (targetEntity != null && !targetEntity.dead && isAttacking)
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
        isAttacking = false;
        // 공격 종료 후 NavMeshAgent 다시 켜주기!
        Rigidbody rb = GetComponent<Rigidbody>();

        pv.RPC("RPC_SetNavMesh", RpcTarget.All, true);

        Debug.Log("공격 끝");
    }

    void OnCollisionStay(Collision col)
    {
        Rigidbody rb = gameObject.GetComponent<Rigidbody>();
        if (isAttacking)
        {
            rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, 1.5f);
        }
    }

    [PunRPC]

    public void RPC_SetNavMesh(bool active)
    {
        Rigidbody rb = gameObject.GetComponent<Rigidbody>();
        navMeshAgent.enabled = active;
        rb.isKinematic = active;
    }
}
