using Photon.Pun;
using UnityEngine;

public class PenMissile : MonoBehaviour
{
    PhotonView pv;
    float m_Damage;
    public float lifeTime = 10.0f;
    private void Awake()
    {
        pv = GetComponent<PhotonView>();
    }
    void Start()
    {
        m_Damage = PenAttack.Damage;
        
        Destroy(gameObject,lifeTime);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void OnTriggerEnter(Collider other)
    {   if (!pv.IsMine) return;
        if (other.CompareTag("Enemy"))
        {
            

            LivingEntity attackTarget = other.GetComponent<LivingEntity>();
            if (attackTarget != null)
            {
                Debug.Log($"적에게 데미지 {m_Damage} 입힘");
                Vector3 hitPoint = other.ClosestPoint(transform.position);
                Vector3 hitNormal = transform.position - other.transform.position;
                
                attackTarget.OnDamage(m_Damage, hitPoint, hitNormal);
                Destroy(gameObject);

            }
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        
    }
}
