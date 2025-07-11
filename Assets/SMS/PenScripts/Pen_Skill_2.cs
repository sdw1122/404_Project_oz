using Photon.Pun;
using UnityEngine;

public class Pen_Skill_2 : MonoBehaviour
{
    [Header("��ų ����")]
    public string Skill_ID = "Pen_Skill_2";
    public string Skill_Name = "��ũ ����";
    public string Skill_Description = "������ ��ġ�� ���� �ӹ��ϴ� �������� �����մϴ�.";
    public string Skill_Type = "AreaOfEffect";
    public float Damage = PenAttack.Damage / 2;
    public float Cooldown = 1.0f;
    public float Charge_Levels = 1.0f;
    [Header("���� ����")]
    float lastFireTime;
    float throwForce=15.0f;

    public static bool isThrow=true;
    PhotonView pv;
    private void Awake()
    {
        pv = GetComponent<PhotonView>();
    }
    private void Update()
    {
        if (pv.IsMine && isThrow) 
        {
            if (Input.GetKeyDown(KeyCode.Q) && Time.time - lastFireTime > Cooldown)
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

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        Vector3 throwDir = Camera.main.transform.forward;
        rb.linearVelocity = throwDir * throwForce;
    }
}
