using Photon.Pun;
using System.Security.Principal;
using UnityEngine;

public class MagicArrow : MonoBehaviour
{
    float damage;
    float speed;
    public float duration = 5f;
    Rigidbody rb;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

    }
    public void Initialize(float dmg, float spd)
    {
        damage = dmg;
        speed = spd;
        rb.linearVelocity = transform.forward * speed;
 
        Invoke(nameof(DestroySelf), duration);

    }
    private void DestroySelf()
    {


        PhotonNetwork.Destroy(gameObject);

    }
    private void OnTriggerEnter(Collider other)
    {
        LivingEntity player = other.GetComponent<LivingEntity>();
        if (player!= null&& player.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            player.OnDamage(damage, transform.position, Vector3.zero);
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.Destroy(this.gameObject);
            }

        }
    }
}
