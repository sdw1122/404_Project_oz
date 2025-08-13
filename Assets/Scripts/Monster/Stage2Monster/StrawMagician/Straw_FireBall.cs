using Photon.Pun;
using UnityEngine;

public class Straw_FireBall : MonoBehaviour
{
    public float damage = 60.0f;
    public float cooldown = 6.0f;
    public float range = 15.0f;
    public float arrowSpeed = 60f;
    public float skillRangeYOffset = 1f; // 반경 y축
    public float lastAttackTime;
    public Transform firePos;
    public GameObject fireEffect;
    Animator animator;
    StrawMagician strawMagician;
    Vector3 directionToTarget;
    FireBall fireBall;
    GameObject fireIns;
    private void Awake()
    {
        animator = GetComponent<Animator>();
        strawMagician = GetComponent<StrawMagician>();
    }
    public bool IsReady()
    {
        return Time.time >= lastAttackTime + cooldown;
    }
    public bool IsInRange(float targetPos)
    {
        return targetPos <= range;
    }
   
    public void Straw_ReduceFireBallCooldown(float amount)
    {
        lastAttackTime -= amount;
        Debug.Log("쿨타임 감소됨 " + lastAttackTime);
    }
    [PunRPC]
    public void StrawMagician_FireBallRPC()
    {


        Debug.Log("🔥 FireBall RPC 호출됨");

        lastAttackTime = Time.time;

        // 공격 애니메이션 트리거 
        if (animator != null)
        {
            fireIns = Instantiate(fireEffect, fireEffect.transform);
            ParticleSystem ps = fireIns.GetComponent<ParticleSystem>();
            ps.Play();
            animator.SetTrigger("FireBall");
        }

        // 데미지를 주는건 마스터만

    }
    public void DetectFireBallTarget()
    {

        if (!PhotonNetwork.IsMasterClient) return;
        if (strawMagician.targetEntity == null) return;

        Vector3 targetPos = strawMagician.targetEntity.transform.position;
        directionToTarget = (targetPos - firePos.position);
        if (directionToTarget.sqrMagnitude < 0.001f)
        {
            directionToTarget = transform.forward;
            Debug.LogWarning("directionToTarget이 0에 가까워 forward로 대체됨");
        }
        directionToTarget.Normalize();
        Debug.Log("directionToTarget = " + directionToTarget);
    }
    public void FireFireBall()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        GameObject magicArrow = PhotonNetwork.Instantiate("test/" + "Straw_FireBall", firePos.position, Quaternion.LookRotation(directionToTarget));
        fireBall = magicArrow.GetComponent<FireBall>();
        if (fireBall != null)
        {
            fireIns.transform.SetParent(fireBall.transform, false);
            fireBall.Initialize(damage, arrowSpeed);
        }
    }
}
