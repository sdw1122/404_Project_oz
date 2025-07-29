using Photon.Pun;
using UnityEngine;
using System.Collections;

public class HangingCitizen : Enemy
{
    public GameObject throwObj;
    public Transform throwPoint;
    public float throwPower = 15f;
    public bool isThrowing = false;

    public override IEnumerator UpdatePath()
    {
        // 살아 있는 동안 무한 루프
        while (!dead)
        {
            if (navMeshAgent.enabled)
            {
                navMeshAgent.enabled = false;
                obstacle.enabled = true;
            }            

            // 추적 로직은 마스터에서만 실행시켜 둘의 Enemy의 움직임을 동기화함.
            if (!PhotonNetwork.IsMasterClient)
            {
                yield return new WaitForSeconds(0.25f);
                continue;
            }

            if (hasTarget)
            {
                float dist = Vector3.Distance(transform.position, targetEntity.transform.position);
                if (dist <= attackRange && !isBinded)
                {
                    if (CanAct())    // (공격 가능한지 자식에게 '질문')
                    {
                        Attack();    // -> Attack도 override 해서 자식 전용
                    }
                }
            }
            else
            {
                Collider[] colliders = Physics.OverlapSphere(transform.position, 20f, whatIsTarget);
                for (int i = 0; i < colliders.Length; i++)
                {
                    Debug.Log($"Collider 발견: {colliders[i].name}");
                    LivingEntity livingEntity = colliders[i].GetComponent<LivingEntity>();
                    Debug.Log(livingEntity != null ? $"유효 LivingEntity: {livingEntity.name} (dead={livingEntity.dead})": "LivingEntity 컴포넌트 없음");
                    if (livingEntity != null && !livingEntity.dead)
                    {
                        Vector3 toTarget = (livingEntity.transform.position - transform.position).normalized;
                        float angle = Vector3.Angle(transform.forward, toTarget);
                        Debug.Log($"toTarget({livingEntity.name}) 각도: {angle}");

                        if (angle <= 45f) // 전방 90도 (±45)
                        {
                            targetEntity = livingEntity;
                            PhotonView targetPV = targetEntity.GetComponent<PhotonView>();

                            if (targetPV != null && pv != null)
                                pv.RPC("SetTarget", RpcTarget.Others, targetPV.ViewID);
                            break;
                        }
                    }
                }
            }
            Debug.Log("Oh");
            // 0.25초 주기로 처리 반복
            yield return new WaitForSeconds(0.25f);
        }
    }

    public override void Attack()
    {
        if (isThrowing) return;
        isThrowing = true;

        if (targetEntity != null)
        {
            Vector3 lookPos = targetEntity.transform.position - transform.position;
            lookPos.y = 0; // 수평 방향만 고려
            Quaternion lookRotation = Quaternion.LookRotation(lookPos);
            enemyAnimator.SetTrigger("Attack");
            pv.RPC("RPC_HangingAttack", RpcTarget.Others, lookRotation);
        }
    }

    [PunRPC]

    public void RPC_HangingAttack()
    {
        enemyAnimator.SetTrigger("Attack");
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

        pv.RPC("RPC_HangingThrow", RpcTarget.All, targetPos, start);
    }

    [PunRPC]
    public void RPC_HangingThrow(Vector3 targetPos, Vector3 start)
    {
        // 1. 돌맹이 생성
        GameObject saliva = Instantiate(throwObj, throwPoint.position, Quaternion.identity);
        TR2Weapon weaponScript = saliva.GetComponent<TR2Weapon>();
        if (weaponScript != null)
        {
            weaponScript.damage = this.damage; // Enemy에 있는 public damage 사용
        }

        // 타겟의 중앙이나 원하는 높이 조준(예: 허리나 머리 높이)
        targetPos.y += 1.0f; // 원하는 만큼 조정

        Vector3 dir = (targetPos - start).normalized;
        dir.y += 0.2f;

        // 3. 힘 적용
        Rigidbody rb = saliva.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(dir * throwPower, ForceMode.VelocityChange);
        }
    }

    public void EndThrow()
    {
        isThrowing = false;
    }
}
