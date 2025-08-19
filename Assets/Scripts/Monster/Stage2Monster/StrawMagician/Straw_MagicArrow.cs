using Photon.Pun;
using UnityEngine;

public class Straw_MagicArrow : MonoBehaviour
{
    public float damage = 20.0f;
    public float cooldown = 3.0f;
    public float range = 15.0f;
    public float arrowSpeed = 60f;
    public float skillRangeYOffset = 1f; // 반경 y축
    float lastAttackTime;
    public Transform firePos;
    Animator animator;
    StrawMagician strawMagician;
    public AudioSource attackSource;
    public AudioSource castSource;
    public AudioSource strumbleSource;
    public AudioSource castASource;
    Vector3 directionToTarget;
    private void Awake()
    {
        animator = GetComponent<Animator>();
        strawMagician = GetComponent<StrawMagician>();
    }
    public bool IsReady()
    {
        return Time.time >= lastAttackTime +cooldown;
    }
    public bool IsInRange(float targetPos)
    {
        return targetPos <= range;
    }
    [PunRPC]
    public void StrawMagician_AttackRPC()
    {



        /*if (Time.time < lastAttackTime + cooldown)
        {

            return;
        }*/
        lastAttackTime = Time.time;

        // 공격 애니메이션 트리거 
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        // 데미지를 주는건 마스터만

    }
    public void DetectTarget()
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
    public void Fire()
    {


        if (!PhotonNetwork.IsMasterClient) return;
        Debug.Log("발사준비");
        
        GameObject magicArrow = PhotonNetwork.Instantiate("test/"+"Straw_MagicArrow", firePos.position, Quaternion.LookRotation(directionToTarget));
        MagicArrow arrow=magicArrow.GetComponent<MagicArrow>();
        Debug.Log("미사일준비"+arrow);
        if (arrow != null)
        {

            arrow.Initialize(damage,arrowSpeed);
            Debug.Log("미사일 발사");

        }

    }
    private void OnDrawGizmosSelected()
    {

        Vector3 gizmoCenter = transform.position + Vector3.up * skillRangeYOffset;
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(gizmoCenter, range);
        // ---

        /*// 넉백 방향 시각화 (선택 사항: 디버깅용)
        if (transform.hasChanged)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * knockbackForce * 0.1f);
        }*/
    }
    public void PlayAttackClip()
    {
        if (attackSource == null) return;
        AudioClip clip = attackSource.clip;
        attackSource.PlayOneShot(clip);
    }
    public void PlayCastClip()
    {
        if(castSource == null) return;
        AudioClip clip = castSource.clip;
        castSource.PlayOneShot(clip);
    }
    public void PlayCastAClip()
    {
        if (castASource == null) return;
        AudioClip clip = castASource.clip;
        castASource.PlayOneShot(clip);
    }
    public void PlayStrumbleClip()
    {
        if(strumbleSource == null) return;
        AudioClip clip = strumbleSource.clip;
        strumbleSource.PlayOneShot(clip);
    }
}
