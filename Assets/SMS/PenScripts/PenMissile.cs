using UnityEngine;

public class PenMissile : MonoBehaviour
{
 
    float m_Damage;
    public float lifeTime = 10.0f;
    void Start()
    {
        m_Damage = PenAttack.Damage;
        Debug.Log(m_Damage);
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
            Debug.Log("미사일이 적과 접촉");
            Destroy(gameObject);
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        
    }
}
