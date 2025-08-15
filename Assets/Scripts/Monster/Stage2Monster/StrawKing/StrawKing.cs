using Photon.Pun;
using System.Collections;
using System.Linq;
using UnityEngine;

public class StrawKing : Enemy
{
    StrawKing_Poison poison;
    Skill1 skill1;
    StrawAttack strawAttack;
    private float targetSwitchTimer = 0f;
   
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
                
                break;
            case StrawKing_State.Absorb:
                pv.RPC("StartSkill", RpcTarget.MasterClient);
                
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
    public void setIdle()
    {
        currentState = StrawKing_State.Idle;
    }
    public override void Update()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        navMeshAgent.isStopped = true;  // 허수아비왕은 움직이지 않는다.
        base.Update();

        if (currentState != StrawKing_State.Idle)
        {
            return;
        }
        distanceToTarget = Vector3.Distance(transform.position, targetEntity.transform.position);
        Vector3 dir = targetEntity.transform.position - transform.position;
        dir.y = 0f;
        dir.Normalize();
        Quaternion lookRotation = Quaternion.LookRotation(dir);
        if (skill1.IsReady())
        {
            currentState = StrawKing_State.Absorb;
            Attack();
            return;
        }
        else if (poison.IsReady())
        {
            currentState = StrawKing_State.Tyrant;
            Attack();
            return;
        }
        else if (strawAttack.IsReady())
        {
            currentState = StrawKing_State.Attack;
            Attack();
            return;
        }
        if (currentState == StrawKing_State.Idle)
        {
            // 타겟향해 회전
            if (targetEntity != null)
            {
                dir = targetEntity.transform.position - transform.position;
                dir.y = 0f;
                if (dir != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(dir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
                }
            }
        }

    }
    public override IEnumerator UpdatePath()
    {
        while (!dead)
        {
            // 마스터 클라이언트만
            if (PhotonNetwork.IsMasterClient)
            {

                targetSwitchTimer += 0.25f;


                if (targetSwitchTimer >= 10f)
                {
                    targetSwitchTimer = 0f; // 타이머 리셋
                    PlayerHealth[] allPlayers = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);

                    // 현재 타겟이 아닌 다른 플레이어를 찾음
                    PlayerHealth otherPlayer = allPlayers.FirstOrDefault(p => p.gameObject != targetEntity.gameObject && !p.dead);

                    if (otherPlayer != null)
                    {

                        targetEntity = otherPlayer;

                        pv.RPC("SetTarget", RpcTarget.Others, otherPlayer.GetComponent<PhotonView>().ViewID);
                        
                    }
                }

                else if (targetEntity == null || targetEntity.dead)
                {

                    Collider[] colliders = Physics.OverlapSphere(transform.position, 200f, whatIsTarget);
                    LivingEntity closestEntity = null;
                    float closestDist = float.MaxValue;

                    foreach (var col in colliders)
                    {
                        LivingEntity entity = col.GetComponent<LivingEntity>();
                        if (entity != null && !entity.dead)
                        {
                            float dist = Vector3.Distance(transform.position, entity.transform.position);
                            if (dist < closestDist)
                            {
                                closestDist = dist;
                                closestEntity = entity;
                            }
                        }
                    }

                    if (closestEntity != null)
                    {
                        targetEntity = closestEntity;
                        pv.RPC("SetTarget", RpcTarget.Others, closestEntity.GetComponent<PhotonView>().ViewID);
                    }
                }
            }

            yield return new WaitForSeconds(0.25f);
        }
    }
}
