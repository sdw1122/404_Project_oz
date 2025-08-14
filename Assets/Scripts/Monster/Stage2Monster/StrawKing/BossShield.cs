using UnityEngine;
using Photon.Pun;

public class BossShield : MonoBehaviour
{
    public BossGroggy groggy;
    public GameObject Shield;

    public int cannonCount = 0;

    public void Start()
    {
        
    }
    public void AddCount()
    {
        groggy.count++;
        if (groggy.count == 3)
        {
            gameObject.SetActive(false);
        }
    }
    
    private void OnTriggerEnter(Collider collision)
    {        
        // 충돌한 오브젝트의 레이어 번호
        int hitLayer = collision.gameObject.layer;

        // 예: "Player" 레이어와 "Shield" 레이어만 검사하고 싶다면
        if (hitLayer == LayerMask.NameToLayer("Player"))
        {
            LivingEntity entity = collision.gameObject.GetComponent<LivingEntity>();
            if (entity != null)
            {
                // 충돌 위치 계산: 내 위치와 가장 가까운 상대 표면
                Vector3 hitPoint = collision.ClosestPoint(transform.position);
                Vector3 hitNormal = (hitPoint - transform.position).normalized;
                entity.OnDamage(10000f, hitPoint, hitNormal);
            }
        }
        if (collision.gameObject.layer == LayerMask.NameToLayer("CannonBall"))
        {
            return;
        }
        else
            Destroy(collision.gameObject);
        
    }


}
