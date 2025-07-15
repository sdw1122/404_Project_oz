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
    PhotonView pv;
    private void Awake()
    {
      pv=GetComponent<PhotonView>();  
    }

    public void Initialize(float p_damage,float p_tik)
    {
        damage = p_damage;
        tik= p_tik;
        if (!pv.IsMine) return;
        StartCoroutine(DamageTickRoutine());
        Invoke(nameof(DestroySelf), duration);
       
    }
    void DestroySelf()
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
            if (enemy != null)
            {
                enemy.isBinded = true;
            }
            targetsInCircle.Add(target);

        }
    }

    /*private void OnTriggerExit(Collider other)
    {
        
        var target = other.GetComponent<LivingEntity>();
        if (target != null && targetsInCircle.Contains(target))
        {
            Enemy enemy = target as Enemy;
            if (enemy != null)
            {
                enemy.isBinded = false;
            }
            targetsInCircle.Remove(target);
        }
    }*/
    private void OnDestroy()
    {
        if (pv==null||!pv.IsMine) return;
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
            if (PhotonNetwork.IsMasterClient) // isMine을 해도 OnTrigger가 각자 실행되니 마스터만 데미지 관리.
            {
                foreach (var target in targetsInCircle)
                {
                    if (target != null && !target.dead && target.CompareTag("Enemy"))
                    {
                        Debug.Log($"틱 데미지 {damage} 입힘: {target.name}");
                        Vector3 hitPoint = target.transform.position;
                        Vector3 hitNormal = (target.transform.position - transform.position).normalized;

                        target.OnDamage(damage, hitPoint, hitNormal);
                    }
                }
            }
           

            yield return new WaitForSeconds(tik);
        }
    }
}
