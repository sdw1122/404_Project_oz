using UnityEngine;
using Photon.Pun;
using System.Collections;
using UnityEngine.Rendering;

public class TinyRobot2 : Enemy
{
    public GameObject throwObj;
    public Transform throwPoint;
    public float throwPower = 15f;
    public float fleeDistance = 8f;    

    public override bool CanAct()
    {        
        return !isAttacking;
    }

    public override IEnumerator UpdatePath()
    {
        while (!dead)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                yield return new WaitForSeconds(0.25f);
                continue;
            }

            if (hasTarget)
            {
                float dist = Vector3.Distance(transform.position, targetEntity.transform.position);

                if (dist <= fleeDistance)
                {
                    // 너무 가까워졌을 때 도망감
                    if (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
                    {
                        Vector3 fleeDir = (transform.position - targetEntity.transform.position).normalized;
                        Vector3 fleePos = transform.position + fleeDir * fleeDistance * 4f;
                        navMeshAgent.isStopped = false;
                        navMeshAgent.SetDestination(fleePos);
                        enemyAnimator.SetFloat("Move", 1f);
                        pv.RPC("RPC_BlendRun", RpcTarget.Others, 1f);                        
                    }
                }
                else if (dist <= attackRange)
                {
                    // 원래대로 공격 패턴
                    enemyAnimator.SetFloat("Move", 0f);
                    pv.RPC("RPC_BlendIdle", RpcTarget.Others, 0f);
                    if (CanAct()) { Attack(); }
                }
                else if (dist > 20f)
                {
                    if (chaseTarget < 10f)
                    {
                        chaseTarget += 0.25f;
                        if (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
                        {
                            navMeshAgent.isStopped = false;
                            navMeshAgent.SetDestination(targetEntity.transform.position);
                            //pv.RPC("SyncRigidState", RpcTarget.Others, rb.position, rb.linearVelocity);
                            if (PhotonNetwork.IsMasterClient)
                            {
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
                    if (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
                    {
                        navMeshAgent.isStopped = false;
                        navMeshAgent.SetDestination(targetEntity.transform.position);
                        //pv.RPC("SyncRigidState", RpcTarget.Others, rb.position, rb.linearVelocity);
                        if (PhotonNetwork.IsMasterClient)
                        {
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

                Collider[] colliders = Physics.OverlapSphere(transform.position, 20f, whatIsTarget);
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
            yield return new WaitForSeconds(0.25f);
        }
    }

    public override void Attack()
    {
        if (dead) return;
        if (isAttacking) return;
        isAttacking = true;

        Rigidbody rb = GetComponent<Rigidbody>();

        if (targetEntity == null || dead) return;

        if (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
        {                        
            navMeshAgent.enabled = false;
            rb.isKinematic = false;
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            pv.RPC("RPC_SyncRigidState", RpcTarget.Others, rb.position, Vector3.zero);
        }

        if (targetEntity != null)
        {
            Vector3 lookPos = targetEntity.transform.position - transform.position;
            lookPos.y = 0; // 수평 방향만 고려
            Quaternion lookRotation = Quaternion.LookRotation(lookPos);
            if (lookPos != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookPos);
                rb.Sleep();
                rb.linearVelocity = Vector3.zero;                
                rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation; ;                
            }
            enemyAnimator.SetTrigger("Throw");
            pv.RPC("RPC_ThrowAttackAni", RpcTarget.Others, lookRotation);
        }
    }

    [PunRPC]
    public void RPC_SyncRigidState(Vector3 pos, Vector3 vel)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.position = pos;
        if (!rb.isKinematic)
            rb.linearVelocity = vel;
    }

    [PunRPC]
    public void RPC_ThrowAttackAni(Quaternion look)
    {
        transform.rotation = look;
        enemyAnimator.SetTrigger("Throw");
    }

    public void Throw()
    {
        if (!PhotonNetwork.IsMasterClient) return; // Throw 등은 반드시 마스터에서만

        if (targetEntity == null)
        {
            Debug.LogWarning("Throw: targetEntity가 null, Throw 실행 중단!");
            return;
        }

        // 2. 방향 계산 (플레이어를 정확히 조준)
        Vector3 targetPos = targetEntity.transform.position;
        Vector3 start = throwPoint.position;

        pv.RPC("RPC_Throw", RpcTarget.All, targetPos, start);
    }

    [PunRPC]
    public void RPC_Throw(Vector3 targetPos, Vector3 start)
    {
        // 1. 돌맹이 생성
        GameObject rock = Instantiate(throwObj, throwPoint.position, Quaternion.identity);
        TR2Weapon weaponScript = rock.GetComponent<TR2Weapon>();
        if (weaponScript != null)
        {
            weaponScript.damage = this.damage; // Enemy에 있는 public damage 사용
        }

        // 타겟의 중앙이나 원하는 높이 조준(예: 허리나 머리 높이)
        targetPos.y += 1.0f; // 원하는 만큼 조정

        Vector3 dir = (targetPos - start).normalized;
        dir.y += 0.2f;

        // 3. 힘 적용
        Rigidbody rb = rock.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(dir * throwPower, ForceMode.VelocityChange);
        }
    }


    public void ThrowEnd()
    {
        isAttacking = false;
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.WakeUp();

        navMeshAgent.enabled = true;        
        rb.isKinematic = true;
    }
}
