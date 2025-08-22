using Photon.Pun;
using UnityEngine;

public class ZombieCitizen : Enemy
{
    public override void Attack()
    {
        if (targetEntity == null || dead) return;
        if (isAttacking) return;
        isAttacking = true;

        isRotatingToTarget = true;

        Debug.Log("공격");
        pv.RPC("RPC_SetNavMesh", RpcTarget.All, false);
        enemyAnimator.SetTrigger("Attack");
        pv.RPC("RPC_ZombieAttack", RpcTarget.Others);
    }

    [PunRPC]
    public void RPC_ZombieAttack()
    {
        enemyAnimator.SetTrigger("Attack");
    }

    public void Damaging()
    {
        // 플레이어 위치와 방향
        Vector3 citizenPos = transform.position;
        Vector3 citizenForward = transform.forward;
        Vector3 citizenRight = transform.right;
        Vector3 citizenUp = transform.up;

        // 오프셋 (x: 오른쪽, y: 위, z: 전방)
        Vector3 offset = new Vector3(0f, 1f, 0.9f); // 필요에 따라 값 조정

        // 오프셋을 월드 좌표로 변환
        Vector3 worldOffset = citizenRight * offset.x + citizenUp * offset.y + citizenForward * offset.z;

        // 박스 중심 좌표
        Vector3 swingCenter = citizenPos + worldOffset;

        Vector3 halfExtents = new Vector3(0.4f, 0.8f, 0.7f); // 필요에 따라 값 조정
        Quaternion orientation = transform.rotation;
        int layerMask = LayerMask.GetMask("Player");

        Collider[] hits = Physics.OverlapBox(swingCenter, halfExtents, orientation, layerMask);
        foreach (var hit in hits)
        {
            if (hit.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                Debug.Log("좀비시민 공격 적중: " + hit.gameObject.name);

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

    public void EndAttack()
    {
        pv.RPC("RPC_SetNavMesh", RpcTarget.All, true);
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
            Vector3 offset = new Vector3(0f, 1f, 0.9f); // 필요에 따라 값 조정

            // 오프셋을 월드 좌표로 변환
            Vector3 worldOffset = golemRight * offset.x + golemUp * offset.y + golemForward * offset.z;

            // 박스 중심 좌표
            Vector3 swingCenter = golemPos + worldOffset;

            // 박스 크기와 회전
            Vector3 boxHalfExtents = new Vector3(0.4f, 0.8f, 0.7f);
            Quaternion orientation = transform.rotation;

            // 기즈모 그리기
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.4f);
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(swingCenter, orientation, Vector3.one);
            Gizmos.matrix = rotationMatrix;
            Gizmos.DrawCube(Vector3.zero, boxHalfExtents * 2);
            Gizmos.matrix = Matrix4x4.identity;
        }
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
            if (navMeshAgent.isOnNavMesh)
                navMeshAgent.Warp(transform.position);
        }
        else
        {
            navMeshAgent.enabled = active;
            obstacle.enabled = !active;
        }
        //rb.isKinematic = active;
    }
    public void PlayAttackAClip()
    {
        AudioManager.instance.PlaySfxAtLocation("Citizen AttackA", transform.position);
    }
    public void PlayAttackBClip()
    {
        AudioManager.instance.PlaySfxAtLocation("Citizen AttackB", transform.position);
    }
}
