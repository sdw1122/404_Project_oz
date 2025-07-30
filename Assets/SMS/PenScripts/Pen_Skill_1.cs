using Photon.Pun;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class Pen_Skill_1 : MonoBehaviour
{
    public GameObject PenPlayer;
    public GameObject bow;
    public GameObject missile1;
    public GameObject missile2;
    public GameObject missile3;
    public ParticleSystem ChargeEffect;
    private bool didChargeLevel1, didChargeLevel2, didChargeLevel3;
    Rigidbody rb;
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
    private Color[] chargeColors = { new Color(1f, 1f, 1f, 0.5f), new Color(0f, 1f, 0.79f, 0.5f), new Color(0f, 0.46f, 1f, 1f) };
    private void Awake()
    {
        pv = GetComponent<PhotonView>();
        animator = PenPlayer.GetComponent<Animator>();
        playerController = GetComponent<PlayerController>();
        rb = PenPlayer.GetComponent<Rigidbody>();
    }
    public void CancelCharging()
    {
        if (!isCharging) return;
        BowDisable();
        ArrowDisable();

        isCharging = false;
        chargeTime = 0f;
        isSkill1Pressed = false;
        playerController.isCharge = false;

        animator.SetBool("Charge", false);
        animator.ResetTrigger("ChargeAttack");

        playerController.canMove = true;
        PenAttack.isAttack = true;
        Pen_Skill_2.isThrow = true;
    }
    private void Update()
    {
        if (!pv.IsMine) return;

        if (isSkill1Pressed && !isCharging && Time.time - lastFireTime > Cooldown)
        {
            didChargeLevel1 = false;
            didChargeLevel2 = false;
            didChargeLevel3 = false;
            StartCharging();
        }

        if (isCharging)
        {
            float nomalized = chargeTime / maxChargeTime;
            chargeTime += Time.deltaTime;

            if (!didChargeLevel1 && nomalized >= 0)
            {
                PlayChargeLevelEffect(1);
                didChargeLevel1 = true;
            }
            if (!didChargeLevel2 && nomalized >= 1f / 3f)
            {
                PlayChargeLevelEffect(2);
                didChargeLevel2 = true;
            }
            if (!didChargeLevel3 && nomalized >= 2f / 3f)
            {
                PlayChargeLevelEffect(3);
                didChargeLevel3 = true;
            }
        }
    }

    public void OnSkill1(InputAction.CallbackContext context)
    {
        if (!pv.IsMine) return;

        if (context.started)
        {
            playerController.isCharge = true;
            isSkill1Pressed = true;
            BowEnable();
        }
        else if (context.canceled)
        {
            isSkill1Pressed = false;
            playerController.isCharge = false;

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
        playerController.ResetSpeed();
        playerController.canMove = false;
        PenAttack.isAttack = false;
        Pen_Skill_2.isThrow = false;

        animator.ResetTrigger("ChargeAttack");
        animator.SetBool("Charge", true);
        pv.RPC("RPC_TriggerChargeStart", RpcTarget.Others);
    }
    private void FinishChargingAndFire()
    {
        isCharging = false;
        BowDisable();
        animator.SetBool("Charge", false);
        animator.ResetTrigger("ChargeAttack");
        animator.SetTrigger("ChargeAttack");   // 발사 애니메이션
        
        pv.RPC("RPC_TriggerChargeFinish", RpcTarget.Others);
        pv.RPC("RPC_TriggerChargeAttack", RpcTarget.Others);
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
        string missilePath = chargeLevel switch
        {
            1 => "Pen_1Charged_missile",
            2 => "Pen_2Charged_missile",
            3 => "Pen_3Charged_missile",
            _ => "Pen_1Charged_missile"
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
        //rotation *= Quaternion.Euler(90, 0, 0);
        ArrowDisable();
        GameObject missile = PhotonNetwork.Instantiate("test/"+missilePath, spawnPos, rotation);
        missile.transform.localScale = new Vector3(100.0f, 100.0f, 76.0f);
        missile.GetComponent<ChargedPenMissile>().Initialize(damage);
        missile.GetComponent<Rigidbody>().linearVelocity = rayDir * speed;
        missile.GetComponent<ChargedPenMissile>().ownerViewID = PhotonView.Get(this).ViewID;
        animator.SetTrigger("ChargeAttack");
        pv.RPC("RPC_TriggerChargeAttack", RpcTarget.Others);
        PenAttack.isAttack = true;
        PlayerController1.isMove = true;
        Pen_Skill_2.isThrow = true;
    }

    void PlayChargeLevelEffect(int chargeLevel)
    {
        var main = ChargeEffect.main;
        ArrowEnable(chargeLevel);
        if (chargeLevel == 1)
        {
            main.startColor = chargeColors[0];
            ChargeEffect.Play();
            pv.RPC("RPC_PlayChargeLevelEffect",RpcTarget.Others);
        }
        else if (chargeLevel == 2)
        {
            main.startColor = chargeColors[1];
            ChargeEffect.Play();
            pv.RPC("RPC_PlayChargeLevelEffect", RpcTarget.Others);
        }
        else if (chargeLevel == 3)
        {
            main.startColor= chargeColors[2];
            ChargeEffect.Play();
            pv.RPC("RPC_PlayChargeLevelEffect", RpcTarget.Others);
        }
    }
    void BowDisable()
    {
        bow.SetActive(false);
        pv.RPC("RPC_BowDisable",RpcTarget.Others);
    }
    void BowEnable()
    {
        bow.SetActive(true);
        pv.RPC("RPC_BowEnable", RpcTarget.Others);
    }
    void ArrowEnable(int level)
    {
        if (level == 1)
        {
            missile1.SetActive(true);
            pv.RPC("RPC_ArrowEnable", RpcTarget.Others, level);
        }
        else if (level == 2)
        {
            missile1.SetActive(false);
            missile2.SetActive(true);
            pv.RPC("RPC_ArrowEnable", RpcTarget.Others, level);
        }
        else if(level == 3)
        {
            missile2.SetActive(false);
            missile3.SetActive(true);
            pv.RPC("RPC_ArrowEnable", RpcTarget.Others, level);
        }
    }
    void ArrowDisable()
    {
        missile1.SetActive(false);
        missile2.SetActive(false);
        missile3.SetActive(false);
        pv.RPC("RPC_ArrowDisable", RpcTarget.Others);
    }
    [PunRPC]
    void RPC_ArrowDisable()
    {
        missile1.SetActive(false);
        missile2.SetActive(false);
        missile3.SetActive(false);
    }
    [PunRPC]
    void RPC_PlayChargeLevelEffect()
    {
        ChargeEffect.Play();
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
    [PunRPC]
    void RPC_BowEnable()
    {
        bow.SetActive(true);
    }
    [PunRPC]
    void RPC_BowDisable()
    {
        bow.SetActive(false);
    }
    [PunRPC]
    void RPC_ArrowEnable(int level)
    {
        if (level == 1)
        {
            missile1.SetActive(true);
        }
        else if (level == 2)
        {
            missile1.SetActive(false);
            missile2.SetActive(true);
        }
        else if (level == 3)
        {
            missile2.SetActive(false);
            missile3.SetActive(true);
        }
    }
}