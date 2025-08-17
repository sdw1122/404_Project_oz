using Photon.Pun;
using System.Security.Principal;
using UnityEngine;

public class StrawAttack : MonoBehaviour
{
    public LayerMask whatIsTarget;
    Animator animator;
    PhotonView pv;
    StrawKing_Poison poison;
    Skill1 skill1;

    private int state = 1;
    private float attackRange = 1000f;
    private float attackDamage = 10f;
    public float attackCoolTime = 10f;
    private float attackTime = 5f;
    StrawKing strawKing;
    private float slowAmount = 0.5f;
    private float slowTime = 3f;
    float lastAttackTime;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator = GetComponent<Animator>();
        pv = GetComponent<PhotonView>();
        poison = GetComponent<StrawKing_Poison>();
        skill1 = GetComponent<Skill1>();
    }
    public bool IsReady()
    {
        if (!poison.endAttack || !skill1.endAttack) return false;
        return Time.time >= lastAttackTime + attackCoolTime;
    }
    [PunRPC]
    public void StrawKing_Attack()
    {   if(Time.time >= lastAttackTime + attackCoolTime)
        {
            lastAttackTime = Time.time;
            pv.RPC("RPC_Attack", RpcTarget.All);
        }
        
    }

    public void Attack()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (state == 1)
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange, whatIsTarget);
            Debug.Log("1감지된 콜라이더 수: " + hitColliders.Length);
            Debug.Log(whatIsTarget.value);
            foreach (var hit in hitColliders)
            {
                if (hit.gameObject.layer == LayerMask.NameToLayer("Player"))
                {
                    Debug.Log("공격");
                    LivingEntity targetLivingEntity = hit.gameObject.GetComponent<LivingEntity>();

                    if (targetLivingEntity == null) continue;

                    // hitPoint와 hitNormal 계산 
                    Vector3 damageHitPoint = hit.transform.position;
                    Vector3 damageHitNormal = (damageHitPoint - transform.position).normalized;

                    targetLivingEntity.OnDamage(attackDamage, damageHitPoint, damageHitNormal);
                    strawKing.SetIdle();

                }
            }
        }
        else if (state == 2)
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange, whatIsTarget);
            Debug.Log("감지된 콜라이더 수: " + hitColliders.Length);
            foreach (var hit in hitColliders)
            {
                if (hit.gameObject.layer == LayerMask.NameToLayer("Player"))
                {
                    Debug.Log("슬로우");
                    PhotonView targetPv = hit.GetComponent<PhotonView>();
                    if (targetPv != null)
                    {
                        targetPv.RPC("RPC_ApplyMoveSpeedDecrease", RpcTarget.All, slowAmount, slowTime);
                        lastAttackTime = Time.time;
                        strawKing.SetIdle();
                    }
                }
            }
        }
    }

    [PunRPC]
    public void RPC_Attack()
    {
        animator.SetTrigger("Attack");
    }

    public void AniEnd()
    {

        if (state == 1)
        {
            state = 2;
        }
        else if (state == 2)
        {
            state = 1;
        }
    }
}
