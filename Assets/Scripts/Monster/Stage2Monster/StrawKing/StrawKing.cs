using Photon.Pun;
using System.Collections;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;
public class StrawKing : Enemy
{
    StrawKing_Poison poison;
    Skill1 skill1;
    StrawAttack strawAttack;
    private float targetSwitchTimer = 0f;
    public override void Awake()
    {
        base.Awake();
        poison = GetComponent<StrawKing_Poison>();
        skill1 = GetComponent<Skill1>();
        currentState = StrawKing_State.Idle;
        strawAttack = GetComponent<StrawAttack>();
    }
    public void SetIdle()
    {
        currentState = StrawKing_State.Idle;
    }
    public void GoEnd()
    {
        PhotonNetwork.LoadLevel("EndingScene");
    }
    public void SetAbsorb()
    {
        currentState = StrawKing_State.Absorb;
    }
    public enum StrawKing_State
    {

        Attack, Absorb, Tyrant, Idle
    }
    float distanceToTarget;
    [SerializeField] StrawKing_State currentState;
    public override void Attack()
    {
        if (targetEntity != null)
        {
            switch (currentState)
            {
                case StrawKing_State.Attack:
                    pv.RPC("StrawKing_Attack", RpcTarget.MasterClient);

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
    }
    public override void Update()
    {
        if (!PhotonNetwork.IsMasterClient || targetEntity == null) return;
        //navMeshAgent.isStopped = true;  // 허수아비왕은 움직이지 않는다.
        base.Update();


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
        if (currentState != StrawKing_State.Absorb)
        {
            Vector3 dir = targetEntity.transform.position - transform.position;
            dir.y = 0f;
            if (dir != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
            }
        }

    }
    public override IEnumerator UpdatePath()
    {
        // 살아 있는 동안 무한 루프
        while (!dead)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                targetSwitchTimer += 0.25f;

                //10초마다 다른 플레이어로 타겟 변경
                if (targetSwitchTimer >= 10f && targetEntity != null)
                {
                    targetSwitchTimer = 0f;
                    PlayerHealth[] allPlayers = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
                    PlayerHealth otherPlayer = allPlayers.FirstOrDefault(p => p.gameObject != targetEntity.gameObject && !p.dead);

                    if (otherPlayer != null)
                    {
                        targetEntity = otherPlayer;
                        pv.RPC("SetTarget", RpcTarget.Others, otherPlayer.GetComponent<PhotonView>().ViewID);
                    }
                    break;
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
            // 0.25초 주기로 처리 반복
            yield return new WaitForSeconds(0.25f);
        }
    }
    public void PlayCanonClip()
    {
        AudioManager.instance.PlaySfxAtLocation("King Hit",transform.position);
    }
    public void PlayAttackClip()
    {
        AudioManager.instance.PlaySfxAtLocation("King Attack", transform.position);
    }
    public void PlayLaughClip()
    {
        AudioManager.instance.PlaySfxAtLocation("King Laugh", transform.position);
    }
    public void PlayGroggyClip()
    {
        AudioManager.instance.PlaySfxAtLocation("King Groggy", transform.position);
    }
    public void PlayChargeClip()
    {
        AudioManager.instance.PlaySfxAtLocation("King Charge", transform.position);
    }
    public void PlayRazorClip()
    {
        AudioManager.instance.PlaySfxAtLocation("King Razor", transform.position);
    }
    public void PlayShoutClip()
    {
        AudioManager.instance.PlaySfxAtLocation("King Shout", transform.position);
    }
}

