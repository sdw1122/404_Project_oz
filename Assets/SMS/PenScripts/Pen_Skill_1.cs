using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class Pen_Skill_1 : MonoBehaviour
{
    public GameObject PenPlayer;
    Animator animator;
    [Header("스킬 정보")]
    public string Skill_ID = "Pen_Skill_1";
    public string Skill_Name = "대궁";
    public string Skill_Description = "힘을 모아 강력한 마법 화살을 발사합니다.";
    public string Skill_Type = "Charge";
    public static float ChargeDamage_1 = PenAttack.Damage * 2;
    public static float ChargeDamage_2 = PenAttack.Damage * 4;
    public static float ChargeDamage_3 = PenAttack.Damage * 8;
    public float Cooldown = 1.0f;
    public float Charge_Levels = 3.0f;
    [Header("세부 정보")]
    public float charged_Pen_Speed = 10.0f;
    public float chargeTime = 0.0f;
    public float maxChargeTime = 3.0f;
    public float minDistance = 5f;
    bool isCharging = false;
    bool isSkill1Pressed = false;
    float lastFireTime;
    public Transform firePoint;
    PlayerController playerController;
    PhotonView pv;
    private void Awake()
    {
        pv = GetComponent<PhotonView>();
        animator = PenPlayer.GetComponent<Animator>();
        playerController = GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (!pv.IsMine) return;

        if (isSkill1Pressed && !isCharging && Time.time - lastFireTime > Cooldown)
        {
            StartCharging();
        }

        if (isCharging)
        {
            chargeTime += Time.deltaTime;
        }
    }

    public void OnSkill1(InputAction.CallbackContext context)
    {
        if (!pv.IsMine) return;

        if (context.started)
        {
            isSkill1Pressed = true;
        }
        else if (context.canceled)
        {
            isSkill1Pressed = false;

            if (isCharging)
            {
                FinishChargingAndFire();
            }
        }
    }
    private void StartCharging()
    {
        isCharging = true;
        chargeTime = 0f;
        lastFireTime = Time.time;

        playerController.canMove = false;
        PenAttack.isAttack = false;
        PlayerController1.isMove = false;
        Pen_Skill_2.isThrow = false;

        animator.SetBool("Charge", true);
        pv.RPC("RPC_TriggerChargeStart", RpcTarget.Others);
    }
    private void FinishChargingAndFire()
    {
        isCharging = false;

        animator.SetBool("Charge", false);
        pv.RPC("RPC_TriggerChargeFinish", RpcTarget.Others);
/*
        if (chargeTime < 0.1f)
        {
            // 너무 짧은 클릭이면 무효
            playerController.canMove = true;
            return;
        }*/

        int chargeLevel = GetChargeLevel(chargeTime / maxChargeTime);
        FireChargePen(chargeLevel);

        // 상태 초기화
        playerController.canMove = true;
        PenAttack.isAttack = true;
        PlayerController1.isMove = true;
        Pen_Skill_2.isThrow = true;
    }
    int GetChargeLevel(float ratio)
    {
        if (ratio < 0.33f) return 1;
        else if (ratio < 0.66f) return 2;
        else return 3;
    }
    void FireChargePen(int chargeLevel)
    {
        float damage = chargeLevel switch
        {
            1 => ChargeDamage_1,
            2 => ChargeDamage_2,
            3 => ChargeDamage_3,
            _ => ChargeDamage_1
        };
        float speed = charged_Pen_Speed + 5f * chargeLevel;
        // 카메라 기준 마우스 방향 계산
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        Vector3 targetPoint;

        if (Physics.Raycast(ray, out hit, 100f))
            targetPoint = hit.point;
        else
            targetPoint = ray.GetPoint(100f);

        Vector3 rayOrigin = new Vector3(firePoint.position.x, firePoint.position.y, firePoint.position.z);
        Vector3 rayDir = Camera.main.transform.forward;

        Vector3 spawnPos = rayOrigin + rayDir * minDistance; // 카메라 앞 0.5m 지점
        Quaternion rotation = Quaternion.LookRotation(rayDir);
        rotation *= Quaternion.Euler(90, 0, 0);
        GameObject missile = PhotonNetwork.Instantiate("Pen_Charged_Missile", spawnPos, rotation);
        missile.transform.localScale = new Vector3(2.0f*chargeLevel, 10.0f, 2.0f * chargeLevel);
        missile.GetComponent<ChargedPenMissile>().Initialize(damage);
        missile.GetComponent<Rigidbody>().linearVelocity = rayDir * speed;

        animator.SetTrigger("ChargeAttack");
        pv.RPC("RPC_TriggerChargeAttack", RpcTarget.Others);
        PenAttack.isAttack = true;
        PlayerController1.isMove = true;
        Pen_Skill_2.isThrow = true;
    }
    [PunRPC]
    void RPC_TriggerChargeAttack()
    {
        animator.SetTrigger("ChargeAttack");
    }
    [PunRPC]
    void RPC_TriggerChargeStart()
    {
        animator.SetBool("Charge", true);
    }
    [PunRPC]
    void RPC_TriggerChargeFinish()
    {
        animator.SetBool("Charge", false);
    }



}
