using UnityEngine;

public class FallDie : MonoBehaviour
{
    public void OnTriggerEnter(Collider col)
    {
        if (col.gameObject.layer == LayerMask.NameToLayer("Enemy") ||
            col.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            Debug.Log("닿았어");
            LivingEntity livingEntity = col.gameObject.GetComponent<LivingEntity>();
            Vector3 center = transform.position + transform.forward * 1.5f;
            Vector3 hitPoint = col.ClosestPoint(center);            
            Vector3 hitNormal = (hitPoint - center).normalized;
            livingEntity.OnDamage(10000, hitPoint, hitNormal);
        }
    }


}
