using UnityEngine;

public class ChargedPenMissile : MonoBehaviour
{
    float damage;
    int level;

    public float lifeTime = 7.0f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Destroy(gameObject, lifeTime);
    }
    public void Initialize(float p_damage)
    {
        damage = p_damage;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            Debug.Log($"미사일이 적과 접촉 데미지 : {damage}");
            Destroy(gameObject);
        }
    }
}
