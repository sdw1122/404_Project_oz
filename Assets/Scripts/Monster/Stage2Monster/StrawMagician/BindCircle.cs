using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BindCircle : MonoBehaviour
{
   float duration = 1.5f;
   float damage;
    float bindDuration;

    private HashSet<PlayerController> targetsInCircle = new HashSet<PlayerController>();
    private PhotonView pv;
    private int playerLayer;
    int ownerViewID;
    Straw_BindCircle straw_BindCircle;
    Straw_FireBall fireBall;
    float time;
    private void Awake()
    {
        pv = GetComponent<PhotonView>();
        playerLayer = LayerMask.NameToLayer("Player");
        
        
    }

    [PunRPC]
    public void RPC_Initialize(float p_damage,float p_duration,int Id,float reduceTime)
    {
        damage = p_damage;
        duration = p_duration;
        ownerViewID = Id;
        time = reduceTime;
        /*bindDuration = p_bindDuration;*/
        PhotonView magicianPV = PhotonView.Find(ownerViewID);
        if (magicianPV != null)
        {
            fireBall = magicianPV.GetComponent<Straw_FireBall>();
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
        
        var target = other.GetComponent<PlayerController>();
        if (target != null)
        {

            var targetEntity=other.GetComponent<LivingEntity>();
            target.setBind();
            if(targetEntity.gameObject.layer==playerLayer) targetEntity.OnDamage(damage, transform.position, Vector3.zero);
            Pen_Skill_1 pen = other.GetComponent<Pen_Skill_1>();
            if (pen != null)
            {
                pen.setBind();
                
            }
            Hammer hammer= other.GetComponent<Hammer>();
            if(hammer != null)
            {
                hammer.setBind();
            }
            if (fireBall != null && PhotonNetwork.IsMasterClient)
            {
                fireBall.Straw_ReduceFireBallCooldown(time);
            }
            targetsInCircle.Add(target);


        }
    }

    private void OnDestroy()
    {
        if (pv == null) return;

        foreach (var target in targetsInCircle)
        {

            target.clearBind();
            Pen_Skill_1 pen = target.GetComponent<Pen_Skill_1>();
            if (pen != null)
            {
                pen.freeBind();

            }
            Hammer hammer = target.GetComponent<Hammer>();
            if (hammer != null)
            {
                hammer.freeBind();
            }
        }

        targetsInCircle.Clear();
    }

}
