using UnityEngine;
using System.Collections;
using Photon.Pun;
using UnityEditor.ShaderGraph.Internal;

public class StrawKnight : Enemy
{
    public float stingDamage = 70f;
    public float stingCoolTime = 0f;
    private float stingTime = 10f;
    
    public float attackCoolTime = 0f;
    private float attackTime = 2f;

    public float crosscutDamage = 45f;
    public float crosscutCoolTime = 0f;
    private float crosscutTime = 4f;



    public override void Update()
    {
        base.Update();                        
        if (stingCoolTime < stingTime)
        {
            stingCoolTime += Time.deltaTime;
        }
        if (attackCoolTime < attackTime)
        {
            attackCoolTime += Time.deltaTime;
        }
        if (crosscutCoolTime < crosscutTime)
        {
            crosscutCoolTime += Time.deltaTime;
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
                    // 바인드 중엔 달리기 애니매이션 출력
                    enemyAnimator.SetFloat("Move", 1f); // 걷기/달리기 애니메이션
                    pv.RPC("RPC_BlendRun", RpcTarget.Others, 1f);
                    navMeshAgent.updateRotation = true;
                }

            }

            if (hasTarget)
            {
                float dist = Vector3.Distance(transform.position, targetEntity.transform.position);
                if (dist <= attackRange && !isBinded && !isAttacking)
                {
                    enemyAnimator.SetFloat("Move", 0f); // 공격 전 Idle자세
                    pv.RPC("RPC_BlendIdle", RpcTarget.Others, 0f);
                    if (CanAct() && !isHit)    // (공격 가능한지 자식에게 '질문')
                    {
                        Attack();    // -> Attack도 override 해서 자식 전용
                    }               
                }
                else
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

    public override void Attack()
    {
        if (targetEntity == null || dead || isAttacking) return;

        isRotatingToTarget = true;
        pv.RPC("RPC_SetNavMesh", RpcTarget.All, false);

        if (angleToTarget <= 30f)
        {                  
            angleToTarget = 180f;
            if (stingCoolTime >= stingTime)
            {
                //찌르기
                Debug.Log("지금 실행");
                enemyAnimator.SetTrigger("Skill1");
                pv.RPC("RPC_KnightSkill1", RpcTarget.Others);
                isAttacking = true;
            }
            else if (crosscutCoolTime >= crosscutTime)
            {
                //횡베기                            
                Debug.Log("지금 실행");
                enemyAnimator.SetTrigger("Skill2");
                pv.RPC("RPC_KnightSkill2", RpcTarget.Others);               
                isAttacking = true;
            }
            else if (attackCoolTime >= attackTime)
            {
                //기본공격
                Debug.Log("지금 실행");
                enemyAnimator.SetTrigger("Attack");
                pv.RPC("RPC_KnightAttack", RpcTarget.Others);                
                isAttacking = true;
            }
        }
        else
        {
            pv.RPC("RPC_SetNavMesh", RpcTarget.All, true);
        }
    }

    //찌르기 대미지 주기
    public void Sting()
    {
        // 플레이어 위치와 방향
        Vector3 knightPos = transform.position;
        Vector3 knightForward = transform.forward;
        Vector3 knightRight = transform.right;
        Vector3 knightUp = transform.up;

        // 오프셋 (x: 오른쪽, y: 위, z: 전방)
        Vector3 offset = new Vector3(0f, 1.1f, 2f); // 필요에 따라 값 조정

        // 오프셋을 월드 좌표로 변환
        Vector3 worldOffset = knightRight * offset.x + knightUp * offset.y + knightForward * offset.z;

        // 박스 중심 좌표
        Vector3 swingCenter = knightPos + worldOffset;

        Vector3 halfExtents = new Vector3(0.3f, 1f, 1.5f); // 필요에 따라 값 조정
        Quaternion orientation = transform.rotation;
        int layerMask = LayerMask.GetMask("Player");

        Collider[] hits = Physics.OverlapBox(swingCenter, halfExtents, orientation, layerMask);
        foreach (var hit in hits)
        {
            if (hit.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                Debug.Log("허수아비기사 찌르기 공격 적중: " + hit.gameObject.name);

                LivingEntity player = hit.GetComponent<LivingEntity>();
                if (player != null)
                {
                    // 피격 위치와 방향 계산
                    Vector3 hitPoint = hit.ClosestPoint(transform.position);
                    Vector3 hitNormal = (hitPoint - transform.position).normalized;
                    PhotonView enemyPv = hit.GetComponent<PhotonView>();
                    player.OnDamage(stingDamage, hitPoint, hitNormal); // damage는 원하는 값으로
                }
            }
        }
    }

    public void EndStingAni()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        Debug.Log("찌르기 끝");
        pv.RPC("RPC_SetNavMesh", RpcTarget.All, true);
        stingCoolTime = 0f;
        isAttacking = false;
    }

    //횡베기
    public void CrossCut()
    {
        Vector3 center = transform.position + Vector3.up * 1.1f; // 중심 높이(골렘 키에 맞게 조절)
        float radius = 3.5f;   // 원의 반지름

        // 위에서 아래로 좁은 캡슐을 만들면 거의 원과 비슷하게 동작합니다
        Vector3 point1 = center + Vector3.up;
        Vector3 point2 = center - Vector3.up;

        int layerMask = LayerMask.GetMask("Player");
        Collider[] hits = Physics.OverlapCapsule(point1, point2, radius, layerMask);

        foreach (var hit in hits)
        {
            if (hit.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                Debug.Log("허수아비기사 원형 공격 적중: " + hit.gameObject.name);

                LivingEntity player = hit.GetComponent<LivingEntity>();
                if (player != null)
                {
                    Vector3 hitPoint = hit.ClosestPoint(center);
                    Vector3 hitNormal = (hitPoint - center).normalized;
                    PhotonView pv = hit.GetComponent<PhotonView>();
                    player.OnDamage(crosscutDamage, hitPoint, hitNormal);
                }
            }
        }
    }

    public void EndCrossCutAni()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        Debug.Log("횡베기 끝");
        pv.RPC("RPC_SetNavMesh", RpcTarget.All, true);
        crosscutCoolTime = 0f;
        isAttacking = false;
    }

    //기본공격
    public void BasicAttack()
    {
        // 플레이어 위치와 방향
        Vector3 knightPos = transform.position;
        Vector3 knightForward = transform.forward;
        Vector3 knightRight = transform.right;
        Vector3 knightUp = transform.up;

        // 오프셋 (x: 오른쪽, y: 위, z: 전방)
        Vector3 offset = new Vector3(0f, 1.1f, 2f); // 필요에 따라 값 조정

        // 오프셋을 월드 좌표로 변환
        Vector3 worldOffset = knightRight * offset.x + knightUp * offset.y + knightForward * offset.z;

        // 박스 중심 좌표
        Vector3 swingCenter = knightPos + worldOffset;

        Vector3 halfExtents = new Vector3(1.5f, 1f, 1.5f); // 필요에 따라 값 조정
        Quaternion orientation = transform.rotation;
        int layerMask = LayerMask.GetMask("Player");

        Collider[] hits = Physics.OverlapBox(swingCenter, halfExtents, orientation, layerMask);
        foreach (var hit in hits)
        {
            if (hit.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                Debug.Log("허수아비기사 찌르기 공격 적중: " + hit.gameObject.name);

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
    }

    public void EndAttackAni()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        Debug.Log("기본 끝");
        pv.RPC("RPC_SetNavMesh", RpcTarget.All, true);
        attackCoolTime = 0f;
        isAttacking = false;
    }

    void OnDrawGizmosSelected()
    {
        // 찌르기
        if (gameObject != null)
        {
            // 플레이어 위치와 방향
            Vector3 golemPos = transform.position;
            Vector3 golemForward = transform.forward;
            Vector3 golemRight = transform.right;
            Vector3 golemUp = transform.up;

            // 오프셋 (x: 오른쪽, y: 위, z: 전방)
            Vector3 offset = new Vector3(0f, 1.1f, 2f); // 필요에 따라 값 조정

            // 오프셋을 월드 좌표로 변환
            Vector3 worldOffset = golemRight * offset.x + golemUp * offset.y + golemForward * offset.z;

            // 박스 중심 좌표
            Vector3 swingCenter = golemPos + worldOffset;

            // 박스 크기와 회전
            Vector3 boxHalfExtents = new Vector3(0.3f, 1f, 1.5f);
            Quaternion orientation = transform.rotation;

            // 기즈모 그리기
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.4f);
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(swingCenter, orientation, Vector3.one);
            Gizmos.matrix = rotationMatrix;
            Gizmos.DrawCube(Vector3.zero, boxHalfExtents * 2);
            Gizmos.matrix = Matrix4x4.identity;
        }

        //횡베기
        if (gameObject != null)
        {
            Vector3 center = transform.position + Vector3.up * 1.1f;
            float radius = 3.5f;
            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.2f);
            Gizmos.DrawWireSphere(center, radius);            // 구
        }

        // 기본공격
        if (gameObject != null)
        {
            // 플레이어 위치와 방향
            Vector3 golemPos = transform.position;
            Vector3 golemForward = transform.forward;
            Vector3 golemRight = transform.right;
            Vector3 golemUp = transform.up;

            // 오프셋 (x: 오른쪽, y: 위, z: 전방)
            Vector3 offset = new Vector3(0f, 1.1f, 2f); // 필요에 따라 값 조정

            // 오프셋을 월드 좌표로 변환
            Vector3 worldOffset = golemRight * offset.x + golemUp * offset.y + golemForward * offset.z;

            // 박스 중심 좌표
            Vector3 swingCenter = golemPos + worldOffset;

            // 박스 크기와 회전
            Vector3 boxHalfExtents = new Vector3(1.5f, 1f, 1.5f);
            Quaternion orientation = transform.rotation;

            // 기즈모 그리기
            Gizmos.color = new Color(0f, 0f, 1f, 0.4f);
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(swingCenter, orientation, Vector3.one);
            Gizmos.matrix = rotationMatrix;
            Gizmos.DrawCube(Vector3.zero, boxHalfExtents * 2);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }

    [PunRPC]
    public void RPC_KnightSkill1()
    {
        enemyAnimator.SetTrigger("Skill1");
    }

    [PunRPC]
    public void RPC_KnightSkill2()
    {
        enemyAnimator.SetTrigger("Skill2");
    }

    [PunRPC]
    public void RPC_KnightAttack()
    {
        enemyAnimator.SetTrigger("Attack");
    }

    [PunRPC]
    public void RPC_SetNavMesh(bool active)
    {
        Rigidbody rb = gameObject.GetComponent<Rigidbody>();
        if (active)
        {
            obstacle.enabled = !active;
            navMeshAgent.enabled = active;
            // NavMeshAgent 다시 활성화할 때 위치 동기화 필수!
            //if (navMeshAgent.isOnNavMesh)
                //navMeshAgent.Warp(transform.position);
        }
        else
        {
            navMeshAgent.enabled = active;
            obstacle.enabled = !active;
        }
    }
}
