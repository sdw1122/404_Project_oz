using Photon.Pun;
using System.Collections;
using UnityEngine;

public class StoneGolem : Enemy
{
    private bool canAttack;
    public float armAttackRange;
    public float groundAttackRange;
    private float armAttackTime = 3f;
    public float armAttackCoolTime = 3f;
    private float groundAttackTime = 6f;
    public float groundAttackCoolTime = 3f;
    public float hitTime = 0f;

    public bool isAttacking = false;
    public bool isHammer = false;

    public void Update()
    {
        if (armAttackCoolTime < armAttackTime)
        {
            armAttackCoolTime += Time.deltaTime;
        }
        
        if (groundAttackCoolTime < groundAttackTime)
        {
            groundAttackCoolTime += Time.deltaTime;
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
            if (hasTarget && !isAttacking)
            {
                float dist = Vector3.Distance(transform.position, targetEntity.transform.position);
                if (dist <= armAttackRange)
                {
                    enemyAnimator.SetFloat("Move", 0f, 0.5f, Time.deltaTime);
                    pv.RPC("RPC_MoveSet", RpcTarget.Others, enemyAnimator.GetFloat("Move"));
                    if (CanAct() && !isBinded)
                    {
                        Attack();    // -> Attack도 override 해서 자식 전용
                    }
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
                            pv.RPC("RPC_MoveSet", RpcTarget.Others, enemyAnimator.GetFloat("Move"));
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
                        pv.RPC("RPC_MoveSet", RpcTarget.Others, enemyAnimator.GetFloat("Move"));
                    }
                }
            }
            else
            {
                if (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
                {
                    navMeshAgent.isStopped = true;
                    enemyAnimator.SetFloat("Move", 0f, 0.5f, 0.25f);
                    pv.RPC("RPC_MoveSet", RpcTarget.Others, enemyAnimator.GetFloat("Move"));
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
            // 0.25초 주기로 처리 반복
            yield return new WaitForSeconds(0.25f);
        }
    }

    [PunRPC]
    public void RPC_MoveSet(float moveVal)
    {
        enemyAnimator.SetFloat("Move", moveVal); // 실시간 즉시 반영
    }

    public override void Attack()
    {
        if (dead) return;
        if (targetEntity == null || dead) return;
        Debug.Log("Attacking");

        // 공격 시 항상 타겟 바라보기
        Vector3 dir = targetEntity.transform.position - transform.position;
        dir.y = 0;
        if (dir != Vector3.zero)
        {
            float lerpSpeed = 8f;
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * lerpSpeed);
        }

        float dist = Vector3.Distance(transform.position, targetEntity.transform.position);
        if (dist <= groundAttackRange && groundAttackCoolTime >= groundAttackTime)
        {
            // 애니메이션 필요
            pv.RPC("SyncLookRotation", RpcTarget.Others, transform.rotation);
            enemyAnimator.SetTrigger("Skill");
            pv.RPC("RPC_GolemSkill", RpcTarget.Others);
            isAttacking = true;
            pv.RPC("RPC_SetNavMesh", RpcTarget.All, false);
        }
        else if (dist <= armAttackRange && armAttackCoolTime >= armAttackTime)
        {
            // 애니메이션 필요
            pv.RPC("SyncLookRotation", RpcTarget.Others, transform.rotation);
            enemyAnimator.SetTrigger("Attack");
            pv.RPC("RPC_GolemAttack", RpcTarget.Others);
            isAttacking = true;
            pv.RPC("RPC_SetNavMesh", RpcTarget.All, false);
        }

    }

    [PunRPC]
    public void RPC_GolemAttack()
    {        
        enemyAnimator.SetTrigger("Attack");
    }

    [PunRPC]
    public void RPC_GolemSkill()
    {
        enemyAnimator.SetTrigger("Skill");
    }

    public void DamageInSwing()
    {
        // 플레이어 위치와 방향
        Vector3 golemPos = transform.position;
        Vector3 golemForward = transform.forward;
        Vector3 golemRight = transform.right;
        Vector3 golemUp = transform.up;

        // 오프셋 (x: 오른쪽, y: 위, z: 전방)
        Vector3 offset = new Vector3(0f, 1.1f, 1f); // 필요에 따라 값 조정

        // 오프셋을 월드 좌표로 변환
        Vector3 worldOffset = golemRight * offset.x + golemUp * offset.y + golemForward * offset.z;

        // 박스 중심 좌표
        Vector3 swingCenter = golemPos + worldOffset;

        Vector3 halfExtents = new Vector3(1f, 1f, 1f); // 필요에 따라 값 조정
        Quaternion orientation = transform.rotation;
        int layerMask = LayerMask.GetMask("Player");

        Collider[] hits = Physics.OverlapBox(swingCenter, halfExtents, orientation, layerMask);
        foreach (var hit in hits)
        {
            if (hit.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                Debug.Log("골렘 공격 적중: " + hit.gameObject.name);
               
                LivingEntity player = hit.GetComponent<LivingEntity>();
                if (player != null)
                {
                    // 피격 위치와 방향 계산
                    Vector3 hitPoint = hit.ClosestPoint(transform.position);
                    Vector3 hitNormal = (hitPoint - transform.position).normalized;
                    PhotonView enemyPv = hit.GetComponent<PhotonView>();
                    player.OnDamage(damage, hitPoint, hitNormal); // damage는 원하는 값으로
                }
            }
        }
        if (hitTime == 0)
        {
            enemyAnimator.SetTrigger("Attack2");
            pv.RPC("RPC_GolemAttack2", RpcTarget.Others);
            hitTime++;
        }
        else if (hitTime == 1)
        {
            hitTime = 0;
            armAttackCoolTime = 0f;
        }
    }

    [PunRPC]
    public void RPC_GolemAttack2()
    {
        enemyAnimator.SetTrigger("Attack2");
    }

    public void DamageInGround()
    {
        Vector3 center = transform.position + Vector3.up * 1.1f; // 중심 높이(골렘 키에 맞게 조절)
        float radius = 1.5f;   // 원의 반지름
        float height = 1f;   // 원통의 세로(높이). 1.1f 정도로 충분하다면 center==point1==point2 해도 됨

        // 위에서 아래로 좁은 캡슐을 만들면 거의 원과 비슷하게 동작합니다
        Vector3 point1 = center + Vector3.up * (height * 0.5f);
        Vector3 point2 = center - Vector3.up * (height * 0.5f);

        int layerMask = LayerMask.GetMask("Player");
        Collider[] hits = Physics.OverlapCapsule(point1, point2, radius, layerMask);

        foreach (var hit in hits)
        {
            if (hit.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                Debug.Log("골렘 원형 공격 적중: " + hit.gameObject.name);

                LivingEntity player = hit.GetComponent<LivingEntity>();
                if (player != null)
                {
                    Vector3 hitPoint = hit.ClosestPoint(center);
                    Vector3 hitNormal = (hitPoint - center).normalized;
                    PhotonView pv = hit.GetComponent<PhotonView>();
                    player.OnDamage(3 *damage, hitPoint, hitNormal);
                }
            }
        }
    }

    public void EndAni()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        pv.RPC("RPC_SetNavMesh", RpcTarget.All, true);
        groundAttackCoolTime = 0f;
        isAttacking = false;
    }
    public void EndArmAni()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        pv.RPC("RPC_SetNavMesh", RpcTarget.All, true);
        armAttackCoolTime = 0f;
        isAttacking = false;
    }


    void OnDrawGizmosSelected()
    {
        // 일반 휘두르기
        if (gameObject != null)
        {
            // 플레이어 위치와 방향
            Vector3 golemPos = transform.position;
            Vector3 golemForward = transform.forward;
            Vector3 golemRight = transform.right;
            Vector3 golemUp = transform.up;

            // 오프셋 (x: 오른쪽, y: 위, z: 전방)
            Vector3 offset = new Vector3(0f, 1.1f, 1f); // 필요에 따라 값 조정

            // 오프셋을 월드 좌표로 변환
            Vector3 worldOffset = golemRight * offset.x + golemUp * offset.y + golemForward * offset.z;

            // 박스 중심 좌표
            Vector3 swingCenter = golemPos + worldOffset;

            // 박스 크기와 회전
            Vector3 boxHalfExtents = new Vector3(1f, 1f, 1f);
            Quaternion orientation = transform.rotation;

            // 기즈모 그리기
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.4f);
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(swingCenter, orientation, Vector3.one);
            Gizmos.matrix = rotationMatrix;
            Gizmos.DrawCube(Vector3.zero, boxHalfExtents * 2);
            Gizmos.matrix = Matrix4x4.identity;
        }
        // 바닥
        Vector3 center = transform.position + Vector3.up * 1.1f;
        float radius = 1.5f;
        float height = 1f;
        Vector3 point1 = center + Vector3.up * (height * 0.5f);
        Vector3 point2 = center - Vector3.up * (height * 0.5f);
        Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.2f);
        Gizmos.DrawWireSphere(center, radius);            // 구
        Gizmos.DrawWireSphere(point1, radius);            // 캡슐 위
        Gizmos.DrawWireSphere(point2, radius);            // 캡슐 아래
        Gizmos.DrawLine(point1 + Vector3.right * radius, point2 + Vector3.right * radius);
        Gizmos.DrawLine(point1 - Vector3.right * radius, point2 - Vector3.right * radius);
        Gizmos.DrawLine(point1 + Vector3.forward * radius, point2 + Vector3.forward * radius);
        Gizmos.DrawLine(point1 - Vector3.forward * radius, point2 - Vector3.forward * radius);
    }

    [PunRPC]
    public void RPC_SetNavMesh(bool active)
    {
        Rigidbody rb = gameObject.GetComponent<Rigidbody>();
        navMeshAgent.isStopped = !active;
        //rb.isKinematic = active;
    }
}
