using Photon.Pun;
using UnityEngine;

public class EnemyCannonBall : MonoBehaviour
{
    public float damage;
    float speed;
    public float duration = 5f;
    public float explosionRadius = 3f;
    Rigidbody rb;
    StrawMagician strawMagician;
    public AudioClip clip;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

    }
    public void Initialize(float dmg)
    {
        damage = dmg;

        if (PhotonNetwork.IsMasterClient)
        {
            Invoke(nameof(DestroySelf), duration);
        }
        

    }
    private void DestroySelf()
    {


        PhotonNetwork.Destroy(gameObject);

    }
    private void OnCollisionEnter(Collision collision)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            AudioSource.PlayClipAtPoint(clip, transform.position);
            // 폭발 이펙트 생성
            PhotonNetwork.Instantiate("test/ExplosionEffect", transform.position, Quaternion.identity);

            // 광역 피해
            Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
            foreach (var hit in hits)
            {
                var entity = hit.GetComponent<LivingEntity>();
                if (entity != null && !entity.dead && entity.gameObject.layer == LayerMask.NameToLayer("Player"))
                {
                    entity.OnDamage(damage, transform.position, Vector3.zero);

                }
            }
        }

        
        gameObject.SetActive(false);
        


    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0.3f, 0f, 0.5f); // 주황 반투명
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
