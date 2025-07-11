using Photon.Pun;
using UnityEngine;

public class Pen_Skill_1 : MonoBehaviour
{
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
    bool isCharging = false;
    float lastFireTime;

    PhotonView pv;
    private void Awake()
    {
        pv = GetComponent<PhotonView>();
    }
    private void Update()
    {
        if (!pv.IsMine) return;
        if (Input.GetKeyDown(KeyCode.R)&&Time.time-lastFireTime>Cooldown)
        {
            lastFireTime = Time.time;
            isCharging = true;
            chargeTime = 0.0f;
            PenAttack.isAttack = false;
            PlayerController1.isMove = false;
            Pen_Skill_2.isThrow = false;
            // 이펙트,사운드 시작
        }
        if(isCharging)
        {
            chargeTime += Time.deltaTime;
        }
        if(Input.GetKeyUp(KeyCode.R) && isCharging)
        {
            isCharging = false;
           

            float chargeRatio = Mathf.Clamp01(chargeTime/maxChargeTime);
            
            int chargeLevel = GetChargeLevel(chargeRatio);

            FireChargePen(chargeLevel);
            
        }
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

        Vector3 rayOrigin = Camera.main.transform.position;
        Vector3 rayDir = Camera.main.transform.forward;

        Vector3 spawnPos = rayOrigin + rayDir * 0.5f; // 카메라 앞 0.5m 지점
        Quaternion rotation = Quaternion.LookRotation(rayDir);
        rotation *= Quaternion.Euler(90, 0, 0);
        GameObject missile = PhotonNetwork.Instantiate("Pen_Charged_Missile", spawnPos, rotation);
        missile.transform.localScale = new Vector3(2.0f*chargeLevel, 10.0f, 2.0f * chargeLevel);
        missile.GetComponent<ChargedPenMissile>().Initialize(damage);
        missile.GetComponent<Rigidbody>().linearVelocity = rayDir * speed;

        PenAttack.isAttack = true;
        PlayerController1.isMove = true;
        Pen_Skill_2.isThrow = true;
    }




}
