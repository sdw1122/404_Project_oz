using Photon.Pun;
using System.Security.Principal;
using UnityEngine;

public class StrawAttack : MonoBehaviour
{
    public LayerMask whatIsTarget;
    Animator animator;

    private int state = 1;
    private float attackRange = 1000f;
    private float attackDamage = 30f;
    public float attackCoolTime = 5f;
    private float attackTime = 5f;

    private float slowAmount = 0.5f;
    private float slowTime = 3f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {        
        animator =  GetComponent<Animator>();
    }
    private void Update()
    {
        if (attackTime < attackCoolTime)
        {
            attackTime += Time.deltaTime;
            if (attackTime >= attackCoolTime)
            {
                Debug.Log("state: " + state);
                RPC_Attack();
            }
        }
    }

    public void Attack()
    {
        if (state == 1)
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange, whatIsTarget);
            Debug.Log("1감지된 콜라이더 수: " + hitColliders.Length);
            Debug.Log(whatIsTarget.value);
            foreach (var hit in hitColliders)
            {
                Debug.Log("아");
                if (hit.gameObject.layer == LayerMask.NameToLayer("Player"))
                {
                    Debug.Log("공격");
                    LivingEntity targetLivingEntity = hit.gameObject.GetComponent<LivingEntity>();

                    if (targetLivingEntity == null) continue;

                    // hitPoint와 hitNormal 계산 
                    Vector3 damageHitPoint = hit.transform.position;
                    Vector3 damageHitNormal = (damageHitPoint - transform.position).normalized;

                    targetLivingEntity.OnDamage(attackDamage, damageHitPoint, damageHitNormal);

                }
            }
        }
        else if (state == 2)
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange, whatIsTarget);
            Debug.Log("감지된 콜라이더 수: " + hitColliders.Length);
            foreach (var hit in hitColliders)
            {
                Debug.Log("어");
                if (hit.gameObject.layer == LayerMask.NameToLayer("Player"))
                {
                    Debug.Log("슬로우");                    
                    PhotonView targetPv = hit.GetComponent<PhotonView>();
                    if (targetPv != null)
                    {
                        targetPv.RPC("RPC_ApplyMoveSpeedDecrease", RpcTarget.All, slowAmount, slowTime);
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
        attackTime = 0;
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
