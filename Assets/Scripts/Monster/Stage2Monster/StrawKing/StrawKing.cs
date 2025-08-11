using Photon.Pun;
using System.Collections;
using UnityEngine;

public class StrawKing : Enemy
{
    StrawKing_Poison poison;
    Skill1 skill1;
    StrawAttack strawAttack;
    public override void Awake()
    {
        base.Awake();
        poison=GetComponent<StrawKing_Poison>();
        skill1 = GetComponent<Skill1>();
        currentState = StrawKing_State.Idle;
        strawAttack= GetComponent<StrawAttack>();
    }
    public enum StrawKing_State
    {

        Attack, Absorb, Tyrant, Idle
    }
    float distanceToTarget;
    [SerializeField] StrawKing_State currentState;
    public override void Attack()
    {
        switch (currentState)
        {
            case StrawKing_State.Attack:
                pv.RPC("StrawKing_Attack", RpcTarget.All);
                currentState = StrawKing_State.Idle;
                break;
            case StrawKing_State.Absorb:
                pv.RPC("StartSkill", RpcTarget.MasterClient);
                currentState = StrawKing_State.Idle;
                break;
            case StrawKing_State.Tyrant:
                pv.RPC("TyrantRPC", RpcTarget.All);
                currentState = StrawKing_State.Idle;
                break;
            case StrawKing_State.Idle:
                /*enemyAnimator.SetFloat("Move", 1f); // 걷기/달리기 애니메이션
                pv.RPC("RPC_BlendRun", RpcTarget.Others, 1f);*/
                break;

        }
    }
    public override void Update()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        navMeshAgent.isStopped = true;  // 허수아비왕은 움직이지 않는다.
        base.Update();
        /*navMeshAgent.isStopped = true;*/
        distanceToTarget = Vector3.Distance(transform.position, targetEntity.transform.position);
        Vector3 dir = targetEntity.transform.position - transform.position;
        dir.y = 0f;
        dir.Normalize();
        Quaternion lookRotation = Quaternion.LookRotation(dir);
        if(skill1.IsReady())
        {
            currentState= StrawKing_State.Absorb;
            Attack();
            return;
        }else if(poison.IsReady())
        {
            currentState = StrawKing_State.Tyrant;
            Attack();
            return;
        }else if (strawAttack.IsReady())
        {
            currentState = StrawKing_State.Attack;
            Attack();
            return;
        }
    }
    public override IEnumerator UpdatePath()
    {
        // 살아 있는 동안 무한 루프
        while (!dead)
        {

            // 추적 로직은 마스터에서만 실행시켜 둘의 Enemy의 움직임을 동기화함.
            if (!PhotonNetwork.IsMasterClient)
            {
                yield return new WaitForSeconds(0.25f);
                continue;
            }
            Collider[] colliders = Physics.OverlapSphere(transform.position, 200f, whatIsTarget);
            for (int i = 0; i < colliders.Length; i++)
            {
                LivingEntity livingEntity = colliders[i].GetComponent<LivingEntity>();
                if (livingEntity != null && !livingEntity.dead)
                {
                    targetEntity = livingEntity;
                    PhotonView targetPV = targetEntity.GetComponent<PhotonView>();

                    if (targetPV != null && pv != null)
                        pv.RPC("SetTarget", RpcTarget.Others, targetPV.ViewID);
                    navMeshAgent.SetDestination(targetEntity.transform.position);
                    if (PhotonNetwork.IsMasterClient)
                    {
                        //pv.RPC("SyncRigidState", RpcTarget.Others, rb.position, rb.linearVelocity);
                        /*pv.RPC("SyncLookRotation", RpcTarget.Others, transform.rotation);*/
                    }
                    break;
                }
            }
            // 0.25초 주기로 처리 반복
            yield return new WaitForSeconds(0.25f);
        }
    }
}
