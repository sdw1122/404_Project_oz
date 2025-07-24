using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class PenAttack : MonoBehaviour
{
    public GameObject penPlayer;

    Animator animator;
    [Header("스킬 정보")]
    public string Skill_ID = "Pen_Attack";
    public string Skill_Name="펜 공격";
    public string Skill_Description="평타";
    public string Skill_Type = "Pyeongta";
    public static float Damage = 20.0f;
    public float Cooldown = 1.0f;
    public float Charge_Levels = 1.0f;
    [Header("세부 정보")]
    public Transform firePoint;
    public float fireRate = 1.0f;
    public float MissileSpeed = 10.0f;
    public Camera penCamera;
    public static bool isAttack = true;
    private float lastFireTime;
    PhotonView pv;
    private void Awake()
    {   

        pv = GetComponent<PhotonView>();
        animator = penPlayer.GetComponent<Animator>();
        if (!pv.IsMine) return;
        Debug.Log("firePoint: " + firePoint);
        
        
    }
    private void Update()
    {

    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (!pv.IsMine) return;
        if (Time.time-lastFireTime>fireRate && isAttack)
        {
            lastFireTime = Time.time;
            Fire();
        }
    }

    void Fire()
    {
        if (!pv.IsMine) return;
        // 카메라 기준 마우스 방향 계산
        Ray ray = penCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        Vector3 targetPoint;

        if (Physics.Raycast(ray, out hit, 100f))
            targetPoint = hit.point;
        else
            targetPoint = ray.GetPoint(100f);

        Vector3 rayOrigin = new Vector3(firePoint.position.x,firePoint.position.y,firePoint.position.z);
        Vector3 rayDir = penCamera.transform.forward;
        
        Vector3 spawnPos = rayOrigin + rayDir * 0.5f; // 카메라 앞 0.5m 지점
        Quaternion rotation = Quaternion.LookRotation(rayDir);
        /*rotation *= Quaternion.Euler(90, 0, 0);*/
        GameObject missile = PhotonNetwork.Instantiate("Pen_Attack_Missile", spawnPos, rotation);
        missile.GetComponent<Rigidbody>().linearVelocity = rayDir * MissileSpeed;
        missile.GetComponent<PenMissile>().ownerViewID = PhotonView.Get(this).ViewID;

        pv.RPC("RPC_TriggerPenAttack", RpcTarget.All);
    }
    [PunRPC]
    void RPC_TriggerPenAttack()
    {
      
        animator.SetTrigger("Attack");
    }

}
