using Photon.Pun;
using UnityEngine;

public class WisdomCannonBall : MonoBehaviour
{
    public float damage;
    float speed;
    public float duration = 5f;
    public float explosionRadius = 3f;
    Rigidbody rb;
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
    private void OnTriggerEnter(Collider other)
    {
        
        if (PhotonNetwork.IsMasterClient)
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("Shield"))
            {
                Debug.Log("대포알이 벽과 충돌");

                other.GetComponent<BossShield>().GetComponent<PhotonView>().RPC("AddCount", RpcTarget.All);
            }
            else if (other.CompareTag("StrawKing"))
            {
                // 폭발 이펙트 생성
                PhotonNetwork.Instantiate("test/ExplosionEffect", transform.position, Quaternion.identity);

                // 광역 피해

                Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);

                foreach (var hit in hits)
                {
                    var entity = hit.GetComponent<LivingEntity>();
                    Enemy enemy = hit.GetComponent<Enemy>();

                    if (enemy != null && !entity.dead)
                    {
                        PhotonView enemyPv = enemy.GetComponent<PhotonView>();
                        Vector3 hitPoint = hit.ClosestPoint(transform.position);
                        Vector3 hitNormal = transform.position - hit.transform.position;


                        enemyPv.RPC("RPC_ApplyDamage", RpcTarget.MasterClient, damage, hitPoint, hitNormal, 9998);
                        enemyPv.RPC("RPC_EnemyHit", RpcTarget.All);
                        enemyPv.RPC("RPC_PlayHitEffect", RpcTarget.All, hitPoint, hitNormal);
                        if (hit.GetComponent<Skill1>() != null)
                        {
                            hit.GetComponent<Skill1>().SetHit();
                        }
                        gameObject.SetActive(false);
                    }
                }
            }
           
        }


        



    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0.3f, 0f, 0.5f); // 주황 반투명
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
