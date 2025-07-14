using UnityEngine;

public class PenMissile : MonoBehaviour
{
 
    float m_Damage;
    public float lifeTime = 10.0f;
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
    {
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
