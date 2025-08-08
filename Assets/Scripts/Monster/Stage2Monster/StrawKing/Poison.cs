using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Poison : MonoBehaviour
{
    public float duration;
    public float damage;
    public float tik;
    public Transform pos;

    private HashSet<LivingEntity> targetsInCircle = new HashSet<LivingEntity>();
    private PhotonView pv;
    private Coroutine damageCoroutine;
    private void Awake()
    {
        pv = GetComponent<PhotonView>();
        gameObject.SetActive(false);
    }
    private void OnEnable()
    {
        
        targetsInCircle.Clear();
    }

    [PunRPC]
    public void RPC_Activate(float p_damage, float p_tik, float p_duration)
    {
        damage = p_damage;
        tik = p_tik;
        duration = p_duration;
        gameObject.SetActive(true);
        gameObject.transform.position = pos.position;
        // 마스터 클라이언트
        if (PhotonNetwork.IsMasterClient)
        {
            if (damageCoroutine != null) StopCoroutine(damageCoroutine);
            damageCoroutine = StartCoroutine(DamageTickRoutine());
        }

        // 소유자만 비활성화
        if (pv.IsMine)
        {
            
            CancelInvoke(nameof(DeactivateSelf));
            Invoke(nameof(DeactivateSelf), duration);
        }
    }
    private void DeactivateSelf()
    {
       
        pv.RPC(nameof(RPC_Deactivate), RpcTarget.All);
    }
    [PunRPC]
    private void RPC_Deactivate()
    {
        
        if (PhotonNetwork.IsMasterClient && damageCoroutine != null)
        {
            StopCoroutine(damageCoroutine);
            damageCoroutine = null;
        }

        
        targetsInCircle.Clear();
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        var target = other.GetComponent<LivingEntity>();
        if (target != null)
        {
                

                targetsInCircle.Add(target);
            
        }
    }
    private void OnTriggerExit(Collider other)
    {
        var target = other.GetComponent<LivingEntity>();
        if (target != null)
        {
            targetsInCircle.Remove(target);
        }
    }

    private void OnDestroy()
    {
        if (pv == null) return;

        

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
                    if (target != null && !target.dead)
                    {
                        Vector3 hitPoint = target.transform.position;
                        Vector3 hitNormal = (target.transform.position - transform.position).normalized;

                        PhotonView enemyPv = target.GetComponent<PhotonView>();
                        Enemy enemy = target.GetComponent<Enemy>();
                        PlayerHealth player=target.GetComponent<PlayerHealth>();
                        if (enemy != null && !enemy.dead)
                        {

                            enemyPv.RPC("RPC_ApplyDamage", RpcTarget.MasterClient, damage, hitPoint, hitNormal,pv.ViewID);

                            enemyPv.RPC("RPC_EnemyHit", RpcTarget.All);

                            enemyPv.RPC("RPC_PlayHitEffect", RpcTarget.All, hitPoint, hitNormal);

                        }
                        else if (player != null && !player.dead)
                        {
                            target.OnDamage(damage, hitPoint, hitNormal);
                            Debug.Log("플레이어에게 데미지"+damage);
                        }
                    }
                }
            }

            yield return new WaitForSeconds(tik);
        }
    }
}
