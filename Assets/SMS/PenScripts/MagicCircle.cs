using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagicCircle : MonoBehaviour
{
    public float duration = 5f;
    public float damage;
    public float tik;

    private HashSet<LivingEntity> targetsInCircle = new HashSet<LivingEntity>();
    private PhotonView pv;
    private int enemyLayer;
    int ownerViewID;
    private void Awake()
    {
        pv = GetComponent<PhotonView>();
        enemyLayer = LayerMask.NameToLayer("Enemy");
    }

    [PunRPC]
    public void RPC_Initialize(float p_damage, float p_tik,int viewID)
    {
        damage = p_damage;
        tik = p_tik;
        ownerViewID = viewID;
        // 데미지 관리는 마스터만!
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(DamageTickRoutine());
            
        }
        // 파괴는 던진 사람이!
        if (pv.IsMine)
        {
            Invoke(nameof(DestroySelf), duration);
        }
    }

    private void DestroySelf()
    {
        if (pv.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        var target = other.GetComponent<LivingEntity>();
        if (target != null)
        {
            Enemy enemy = target as Enemy;
            WoodMan woodMan=enemy as WoodMan;
            if (enemy != null&&woodMan._currentMode!=WoodMan.WoodMan_Mode.Normal)
            {
                enemy.isBinded = true;
            }
            targetsInCircle.Add(target);
        }
    }

    private void OnDestroy()
    {
        if (pv == null) return;

        foreach (var target in targetsInCircle)
        {
            Enemy enemy = target as Enemy;
            if (enemy != null)
            {
                enemy.isBinded = false;
            }
        }

        targetsInCircle.Clear();
    }

    private IEnumerator DamageTickRoutine()
    {
        while (true)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                foreach (var target in targetsInCircle)
                {
                    if (target != null && !target.dead && target.gameObject.layer == enemyLayer)
                    {
                        Vector3 hitPoint = target.transform.position;
                        Vector3 hitNormal = (target.transform.position - transform.position).normalized;

                        PhotonView enemyPv = target.GetComponent<PhotonView>();

                        if (enemyPv != null)
                        {
                            enemyPv.RPC("RPC_PlayHitEffect", RpcTarget.All, hitPoint, hitNormal);
                            enemyPv.RPC("RPC_ApplyDamage", RpcTarget.MasterClient, damage, hitPoint, hitNormal,ownerViewID);
                        }
                    }
                }
            }

            yield return new WaitForSeconds(tik);
        }
    }
}
