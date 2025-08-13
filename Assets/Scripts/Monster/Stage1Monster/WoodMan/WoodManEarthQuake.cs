using UnityEngine;
using Photon.Pun;
public class WoodManEarthQuake : MonoBehaviour
{
    [Header("대지 파쇄 스킬 설정")]
    public float skillRadius = 5f; // 스킬 반경
    public float skillDamage = WoodManAttack.meleeAttackDamage*1.5f; // 스킬 피해량
    public float knockbackForce = 10f; // 넉백 힘 
    public float knockbackUpwardForce = 10f;
    public float knockbackDuration = 2f; // 넉백 시간
    public float skillRangeYOffset = 1f;

    [Header("쿨타임")]
    public float skillCooldown = 8f;
    
    private Animator animator;
    public ParticleSystem skillEffect;
    private WoodMan woodMan; // WoodMan 스크립트 참조
    private float lastSkillTime; // 마지막 스킬 사용 시간 (네트워크 동기화 필요)

    public AudioSource landing;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        woodMan = GetComponent<WoodMan>();
    }
    public bool isReady()
    {
        return Time.time >= lastSkillTime + skillCooldown;
    }
    public void SetDamage(float value)
    {
        skillDamage = value;
    }
    public bool IsTargetInRange()
    {
        Vector3 skillCenter = transform.position + Vector3.up * skillRangeYOffset;
        Collider[] hitColliders = Physics.OverlapSphere(skillCenter, skillRadius, woodMan.whatIsTarget);
        foreach (var col in hitColliders)
        {
            LivingEntity entity = col.GetComponent<LivingEntity>();
            if (entity != null && !entity.dead)
            {
                // 타겟이 맞는 LivingEntity가 있다면 true
                return true;
            }
        }

        return false;
    }
    [PunRPC]
    public void EarthQuakeRPC()
    {
        if (Time.time<lastSkillTime+skillCooldown)
        {
            Debug.Log("대지 파쇄 쿨타임 입니다.");
            return;
        }
        
            lastSkillTime=Time.time;
        
        animator.SetTrigger("Skill1");
        
    }
    public void ApplyEarthQuake()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        Vector3 skillCenter = transform.position + Vector3.up * skillRangeYOffset;
        Collider[] hitColliders = Physics.OverlapSphere(skillCenter, skillRadius,woodMan.whatIsTarget);

        foreach(Collider hitCollider in hitColliders) 
        {   
            LivingEntity targetLivingEntity=hitCollider.GetComponent<LivingEntity>();
            if(targetLivingEntity==null||targetLivingEntity.dead) continue;
            targetLivingEntity.OnDamage(skillDamage, targetLivingEntity.transform.position, Vector3.zero);
            PhotonView targetPv=targetLivingEntity.GetComponent<PhotonView>();

            ApplyKnockback(targetLivingEntity, transform.position);
            Debug.Log(targetLivingEntity);

        }
    }

    void PlaySkill2Effect()
    {
        skillEffect.Play();
    }
    
    void ApplyKnockback(LivingEntity target,Vector3 pos)
    {
        Debug.Log("Apply넉백 호출됨");
        Rigidbody targetRb = target.GetComponent<Rigidbody>();
      
        Vector3 knockbackDirection = (target.transform.position - pos).normalized;

        // 넉백 방향의 Y축을 무시하고 수평 방향으로만 계산(위로 치솟지 않게)
        knockbackDirection.y = 0f;

       
        // 너무 가까우면 보스의 앞 방향으로 강제 지정
        if (knockbackDirection.magnitude < 0.5f)
        {
            knockbackDirection = transform.forward;
        }
        else
        {
            knockbackDirection.Normalize();
        }
        // 띄우는 힘

        Vector3 finalKnockbackForce = knockbackDirection * knockbackForce + Vector3.up * knockbackUpwardForce;

     
        PhotonView targetPv = target.GetComponent<PhotonView>();
        Debug.Log("타겟pv : "+targetPv);
       
        if (targetPv != null/* && targetPv.IsMine*/)
        {
            /*PlayerController pc = target.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.StartKnockback(finalKnockbackForce,knockbackDuration);
            }*/
            targetPv.RPC("StartKnockback", RpcTarget.All, finalKnockbackForce, knockbackDuration);
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        
        Vector3 gizmoCenter = transform.position + Vector3.up * skillRangeYOffset;
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(gizmoCenter, skillRadius);
        // ---

        /*// 넉백 방향 시각화 (선택 사항: 디버깅용)
        if (transform.hasChanged)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * knockbackForce * 0.1f);
        }*/
    }

    public void PlayLandingClip()
    {
        AudioClip clip = landing.clip;
        landing.PlayOneShot(clip);
    }
}
