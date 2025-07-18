using Photon.Pun;
using UnityEngine;

public class ChargedPenMissile : MonoBehaviour
{
    int layerMask;
    float damage;
    int level;
    PhotonView pv;
    public float lifeTime = 7.0f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        layerMask = LayerMask.NameToLayer("Enemy");
        pv = GetComponent<PhotonView>();
    }
    void Start()
    {   

        Destroy(gameObject, lifeTime);
    }
    public void Initialize(float p_damage)
    {
        damage = p_damage;
    }
    private void OnTriggerEnter(Collider other)
    {   if (!pv.IsMine) return;
        if (other.gameObject.layer == layerMask)
        {
            LivingEntity attackTarget = other.GetComponent<LivingEntity>();
            if (attackTarget != null)
            {
                Debug.Log($"적에게 데미지 {damage} 입힘");
                Vector3 hitPoint = other.ClosestPoint(transform.position);
                Vector3 hitNormal = transform.position - other.transform.position;

                attackTarget.OnDamage(damage, hitPoint, hitNormal);
                

            }
        }
    }
}
