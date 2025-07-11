using Photon.Pun;
using UnityEngine;

public class PenAttack : MonoBehaviour
{
    [Header("��ų ����")]
    public string Skill_ID = "Pen_Attack";
    public string Skill_Name="�� ����";
    public string Skill_Description="��Ÿ";
    public string Skill_Type = "Pyeongta";
    public static float Damage = 20.0f;
    public float Cooldown = 1.0f;
    public float Charge_Levels = 1.0f;
    [Header("���� ����")]
    public Transform firePoint;
    public float fireRate = 1.0f;
    public float MissileSpeed = 10.0f;

    public static bool isAttack = true;
    private float lastFireTime;
    PhotonView pv;
    private void Awake()
    {
        pv = GetComponent<PhotonView>();
        Debug.Log("firePoint: " + firePoint);
    }
    private void Update()
    {
        if (!pv.IsMine) return;

        if(Input.GetMouseButton(0)&&Time.time-lastFireTime>fireRate&&isAttack)
        {   

            lastFireTime=Time.time;
            Fire();    
        }
    }
    void Fire()
    {
        // ī�޶� ���� ���콺 ���� ���
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        Vector3 targetPoint;

        if (Physics.Raycast(ray, out hit, 100f))
            targetPoint = hit.point;
        else
            targetPoint = ray.GetPoint(100f);
            
        Vector3 rayOrigin = Camera.main.transform.position;
        Vector3 rayDir = Camera.main.transform.forward;
        
        Vector3 spawnPos = rayOrigin + rayDir * 0.5f; // ī�޶� �� 0.5m ����
        Quaternion rotation = Quaternion.LookRotation(rayDir);
        rotation *= Quaternion.Euler(90, 0, 0);
        GameObject missile = PhotonNetwork.Instantiate("Pen_Attack_Missile", spawnPos, rotation);
        missile.GetComponent<Rigidbody>().linearVelocity = rayDir * MissileSpeed;
    }


}
