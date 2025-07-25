using Photon.Pun;
using UnityEngine;

public class WoodManRoar : MonoBehaviour
{
    [Header("충격파 스킬 설정")]
    public Transform firePos; 
    public float skillDamage = WoodManAttack.meleeAttackDamage*2f; // 충격파 피해량
    public float impactWaveSpeed = 20f; // 충격파 투사체 속도
    public float moveSpeedDecreaseAmount = 0.5f; // 이동 속도 감소율   
    public float moveSpeedDecreaseDuration = 3f; // 이동 속도 감소 지속 시간
    public float skillRadius = 20f; // 스킬 반경
    public float skillRangeYOffset = 1f; // 반경 y축
    [Header("쿨타임")]
    public float skillCooldown = 15f;

    private Animator animator; 
    private WoodMan woodMan;
    private float _lastSkillTime;
    private void Awake()
    {
        animator= GetComponent<Animator>();
        woodMan = GetComponent<WoodMan>();

    }
    public bool IsReady()
    {
        return Time.time >= _lastSkillTime + skillCooldown;
    }
    public void SetDamage(float value)
    {
        skillDamage = value;
    }
    public bool IsInMeleeRange(float targetPos)
    {
        return targetPos <= skillRadius;
    }
    [PunRPC]
    public void RoarRPC()
    {
        if (Time.time < _lastSkillTime + skillCooldown)
        {
            Debug.Log("충격파 쿨타임 입니다.");
            return;
        }

        _lastSkillTime = Time.time;

        animator.SetTrigger("Skill2");
    }
    public void DetectTarget()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
        Vector3 skillCenter = transform.position + Vector3.up * skillRangeYOffset;
        Collider[] hitColliders = Physics.OverlapSphere(skillCenter, skillRadius, woodMan.whatIsTarget);
        Debug.Log("탐지");
        foreach (Collider hitCollider in hitColliders)
        {
            LivingEntity targetLivingEntity = hitCollider.GetComponent<LivingEntity>();
            if (targetLivingEntity == null || targetLivingEntity.dead) continue;
            if (targetLivingEntity == woodMan.targetEntity) 
            {
                RoarToFire(targetLivingEntity);
                Debug.Log("RoarToFire");
            }
               
                
            
            

        }
    }
    public void RoarToFire(LivingEntity target)
    {
        
        
        if (target == null) return;

        Vector3 directionToTarget = (target.transform.position - firePos.position).normalized;
        GameObject impactObj = PhotonNetwork.Instantiate("WoodMan_Impact", firePos.position, Quaternion.LookRotation(directionToTarget));
        ImpactMissile missile= impactObj.GetComponent<ImpactMissile>();
       
        if (missile != null)
        {
            
            missile.Initialize(skillDamage,impactWaveSpeed,moveSpeedDecreaseDuration,moveSpeedDecreaseAmount);
            Debug.Log("미사일 발사");

        }

    }
    private void OnDrawGizmosSelected()
    {

        Vector3 gizmoCenter = transform.position + Vector3.up * skillRangeYOffset;
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(gizmoCenter, skillRadius);
        // ---

        /*// 넉백 방향 시각화 (선택 사항: 디버깅용)
        if (transform.hasChanged)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * knockbackForce * 0.1f);
        }*/
    }


}
