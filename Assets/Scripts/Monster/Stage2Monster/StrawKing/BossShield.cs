using UnityEngine;
using Photon.Pun;

public class BossShield : MonoBehaviour
{
    public BossGroggy groggy;   

    public int cannonCount = 0;

    public void Start()
    {
        
    }
    
    private void OnCollisionEnter(Collision collision)
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
                Vector3 hitPoint = collision.collider.ClosestPoint(transform.position);
                Vector3 hitNormal = (hitPoint - transform.position).normalized;
                entity.OnDamage(10000f, hitPoint, hitNormal);
            }
        }
        else if (hitLayer == LayerMask.NameToLayer("CannonBall"))
        {
            if (groggy.count < 3)
            {
                groggy.count++;
            }
            else if (groggy.count == 3)
            {
                gameObject.SetActive(false);
            }
        }
        else
        {
            Destroy(collision.gameObject);
        }
    }


}
