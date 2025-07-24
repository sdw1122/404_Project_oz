using Photon.Pun;
using UnityEngine;

public class PenMissile : MonoBehaviour
{
    int layerMask;
    PhotonView pv;
    float m_Damage;
    public int ownerViewID;
    public float lifeTime = 10.0f;
    private void Awake()
    {   layerMask= LayerMask.NameToLayer("Enemy");
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
        if (pv != null && (pv.IsMine || PhotonNetwork.IsMasterClient))
        {
            if (other.gameObject.layer == layerMask)
            {
                Debug.Log($"적에게 데미지 {m_Damage} 입힘");
                Vector3 hitPoint = other.ClosestPoint(transform.position);
                Vector3 hitNormal = transform.position - other.transform.position;

                PhotonView enemyPv=other.GetComponent<PhotonView>();

                enemyPv.RPC("RPC_PlayHitEffect", RpcTarget.All, hitPoint, hitNormal);

                enemyPv.RPC("RPC_ApplyDamage", RpcTarget.MasterClient, m_Damage, hitPoint, hitNormal, ownerViewID);
                PhotonNetwork.Destroy(gameObject);

            }
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        
    }
}
