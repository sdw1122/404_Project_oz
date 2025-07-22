using UnityEngine;
using Photon.Pun;
using UnityEngine.Rendering;

public class TinyRobot2 : Enemy
{
    public GameObject throwObj;
    public Transform throwPoint;
    public float throwPower = 15f;

    private bool isThrowing = false;

    public override bool CanAct()
    {        
        return !isThrowing;
    }

    public override void Attack()
    {
        if (isThrowing) return;
        isThrowing = true;

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
        isThrowing = false;
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.WakeUp();

        navMeshAgent.enabled = true;        
        rb.isKinematic = true;
    }
}
