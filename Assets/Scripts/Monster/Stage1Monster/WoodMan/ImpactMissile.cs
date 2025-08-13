using Photon.Pun;
using UnityEngine;

public class ImpactMissile : MonoBehaviour
{
    float damage;
    float speed;
    float slowDuration;
    float slowAmount;
    public float duration=5f;
    Rigidbody rb;

    public AudioSource impact;
    private AudioClip clip;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        clip = impact.clip;
    }
    public void Initialize(float dmg,float spd,float sDu,float sAm)
    {
        damage = dmg;
        speed = spd;
        slowDuration = sDu;
        slowAmount = sAm;
        rb.linearVelocity = transform.forward * speed;
        Debug.Log($"지속시간{duration}");
        Invoke(nameof(DestroySelf), duration);
        
    }
    private void DestroySelf()
    {
       
        
            PhotonNetwork.Destroy(gameObject);
        
    }
    private void OnTriggerEnter(Collider other)
    {
        AudioSource.PlayClipAtPoint(clip, transform.position);
        LivingEntity hitEntity=other.GetComponent<LivingEntity>();
        if(hitEntity != null&&hitEntity.gameObject.layer== LayerMask.NameToLayer("Player"))
        {
            hitEntity.OnDamage(damage,transform.position,Vector3.zero);
            Debug.Log($"[ImpactWave] {other.name}에 충격파 명중! 데미지: {damage}");
            PhotonView targetPv=hitEntity.GetComponent<PhotonView>();
            if(targetPv != null)
            {
                targetPv.RPC("RPC_ApplyMoveSpeedDecrease", RpcTarget.All, slowAmount, slowDuration);
                if (PhotonNetwork.IsMasterClient)
                {
                    PhotonNetwork.Destroy(this.gameObject);
                }
            }

        }
        
    }



}
