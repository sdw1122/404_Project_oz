using UnityEngine;

public class TR2Weapon : MonoBehaviour
{
    public float damage;
    public float lifeTime = 5f;
    public AudioClip clip;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void OnCollisionEnter(Collision collision)
    {
        // Player 레이어에 닿았는지 확인
        int playerLayer = LayerMask.NameToLayer("Player");
        if (collision.gameObject.layer == playerLayer)
        {
            AudioSource.PlayClipAtPoint(clip, transform.position);
            // LivingEntity 컴포넌트가 있으면 데미지 적용
            LivingEntity entity = collision.gameObject.GetComponent<LivingEntity>();
            if (entity != null)
            {
                // 충돌 지점, 노멀 전달
                ContactPoint contact = collision.contacts[0];
                entity.OnDamage(damage, contact.point, contact.normal);
            }
            Destroy(gameObject); // 맞추면 돌도 즉시 소멸
        }
    }
}
