using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class Pen_Skill_2 : MonoBehaviour
{
    public GameObject PenPlayer;
    Animator animator;
    [Header("스킬 정보")]
    public string Skill_ID = "Pen_Skill_2";
    public string Skill_Name = "잉크 감옥";
    public string Skill_Description = "지정한 위치에 적을 속박하는 마법진을 생성합니다.";
    public string Skill_Type = "AreaOfEffect";
    public float Damage = PenAttack.Damage / 2;
    public float Cooldown = 1.0f;
    public float Charge_Levels = 1.0f;
    [Header("세부 정보")]
    public float tik = 0.5f;
    float lastFireTime;
    float throwForce=15.0f;

    public static bool isThrow=true;
    PhotonView pv;
    private void Awake()
    {
        pv = GetComponent<PhotonView>();
        animator = PenPlayer.GetComponent<Animator>();
    }
    private void Update()
    {

        
    }

    public void OnSkill2(InputAction.CallbackContext context)
    {
        if (pv.IsMine && isThrow)
        {
            if (context.started && Time.time - lastFireTime > Cooldown)
            {
                lastFireTime = Time.time;
                ThrowProjectile();
            }
        }
    }

    void ThrowProjectile()
    {
        Vector3 spawnPos = Camera.main.transform.position + Camera.main.transform.forward * 0.5f;
        Quaternion rot = Quaternion.identity;
        GameObject obj = PhotonNetwork.Instantiate("Pen_Skill2_Projectile", spawnPos, rot);
        obj.GetComponent<Skill2Projectile>().Initialize(Damage,tik);
        animator.SetTrigger("Attack");
        pv.RPC("RPC_TriggerPenAttack1", RpcTarget.Others);
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        Vector3 throwDir = Camera.main.transform.forward;
        rb.linearVelocity = throwDir * throwForce;
    }
    [PunRPC]
    void RPC_TriggerPenAttack1()
    {
        animator.SetTrigger("Attack");
    }
}
