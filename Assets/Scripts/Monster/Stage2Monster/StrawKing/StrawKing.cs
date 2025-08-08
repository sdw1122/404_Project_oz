using Photon.Pun;
using System.Collections;
using UnityEngine;

public class StrawKing : Enemy
{
    StrawKing_Poison poison;
    public override void Awake()
    {
        base.Awake();
        poison=GetComponent<StrawKing_Poison>();
        currentState = StrawKing_State.Idle;
    }
    public enum StrawKing_State
    {

        Dimension, Absorb, Tyrant, Idle
    }
    float distanceToTarget;
    [SerializeField] StrawKing_State currentState;
    public override void Attack()
    {
        switch (currentState)
        {


            case StrawKing_State.Dimension:

                currentState = StrawKing_State.Idle;

                break;
            case StrawKing_State.Absorb:

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
        base.Update();
        /*navMeshAgent.isStopped = true;*/
        distanceToTarget = Vector3.Distance(transform.position, targetEntity.transform.position);
        Vector3 dir = targetEntity.transform.position - transform.position;
        dir.y = 0f;
        dir.Normalize();
        Quaternion lookRotation = Quaternion.LookRotation(dir);
        if (poison.IsReady())
        {
            currentState = StrawKing_State.Tyrant;
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
            if (isBinded)
            {
                if (navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
                {
                    navMeshAgent.isStopped = true;
                    navMeshAgent.updateRotation = false;

                    navMeshAgent.updateRotation = true;
                }

            }

            if (hasTarget)
            {
                float dist = Vector3.Distance(transform.position, targetEntity.transform.position);
                if (dist <= attackRange && !isBinded)
                {
                    enemyAnimator.SetFloat("Move", 0f); // 공격 전 Idle자세
                    pv.RPC("RPC_BlendIdle", RpcTarget.Others, 0f);
                    if (CanAct() && !isHit)    // (공격 가능한지 자식에게 '질문')
                    {
                        Attack();    // -> Attack도 override 해서 자식 전용
                    }
                }
                else if (dist > 20f)
                {
                    if (chaseTarget < 10f)
                    {
                        chaseTarget += 0.25f;
                        if (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh && !isBinded)
                        {
                            navMeshAgent.isStopped = false;
                            navMeshAgent.SetDestination(targetEntity.transform.position);
                            Debug.Log("목적지설정");
                            if (PhotonNetwork.IsMasterClient)
                            {
                                //pv.RPC("SyncRigidState", RpcTarget.Others, rb.position, rb.linearVelocity);
                                pv.RPC("SyncLookRotation", RpcTarget.Others, transform.rotation);
                            }
                            enemyAnimator.SetFloat("Move", 1f); // 걷기/달리기 애니메이션
                            pv.RPC("RPC_BlendRun", RpcTarget.Others, 1f);
                        }
                    }
                    else
                    {
                        targetEntity = null;
                        chaseTarget = 0;
                    }
                }
                else if (dist <= 20f)
                {
                    chaseTarget = 0;
                    if (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh && !isBinded)
                    {
                        navMeshAgent.isStopped = false;
                        navMeshAgent.SetDestination(targetEntity.transform.position);
                        if (PhotonNetwork.IsMasterClient)
                        {
                            //pv.RPC("SyncRigidState", RpcTarget.Others, rb.position, rb.linearVelocity);
                            pv.RPC("SyncLookRotation", RpcTarget.Others, transform.rotation);
                        }
                        enemyAnimator.SetFloat("Move", 1f); // 걷기/달리기 애니메이션
                        pv.RPC("RPC_BlendRun", RpcTarget.Others, 1f);
                    }
                }
            }
            else
            {
                if (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
                {
                    navMeshAgent.isStopped = true;
                    enemyAnimator.SetFloat("Move", 0f);
                    pv.RPC("RPC_BlendIdle", RpcTarget.Others, 0f);
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
                        break;
                    }
                }

            }
            // 0.25초 주기로 처리 반복
            yield return new WaitForSeconds(0.25f);
        }
    }
}
