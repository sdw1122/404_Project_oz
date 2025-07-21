using UnityEngine;
using Photon.Pun;

public class Cannon : InteractableBase
{
    [Header("대포 설정")]
    [Tooltip("발사될 포탄의 프리팹입니다. 'Resources' 폴더 안에 있어야 합니다.")]
    [SerializeField] private GameObject projectilePrefab;

    [Tooltip("포탄이 생성될 위치입니다. (대포의 포구)")]
    [SerializeField] private Transform firePoint;

    [Tooltip("포탄에 가해질 힘의 크기입니다.")]
    [SerializeField] private float fireForce = 20f;

    [Tooltip("발사 후 다음 발사까지의 대기 시간(쿨타임)입니다.")]
    [SerializeField] private float cooldownTime = 3f;

    // 마지막으로 발사한 시간을 기록합니다.
    private float lastFireTime = -999f;

    protected override void Awake()
    {
        base.Awake(); // 부모 클래스의 Awake() 실행
        if (projectilePrefab == null)
        {
            Debug.LogError("'projectilePrefab'이 설정되지 않았습니다!", this);
        }
        if (firePoint == null)
        {
            Debug.LogError("'firePoint'가 설정되지 않았습니다! 포탄 발사 위치를 지정해주세요.", this);
        }
    }

    /// <summary>
    /// 플레이어가 이 대포와 상호작용할 수 있는지 확인합니다.
    /// 쿨타임이 차지 않았다면 상호작용할 수 없습니다.
    /// </summary>
    public override bool CanInteract(PlayerController player)
    {
        // 쿨타임이 아직 안 지났으면 false
        if (Time.time < lastFireTime + cooldownTime)
        {
            // 여기에 "재장전 중..." 같은 UI 메시지를 띄우는 로직을 추가할 수도 있습니다.
            return false;
        }
        // 쿨타임이 다 찼으면, 부모 클래스의 직업 체크 로직을 따릅니다.
        return base.CanInteract(player);
    }

    /// <summary>
    /// 플레이어가 상호작용했을 때 호출됩니다.
    /// 마스터 클라이언트에게 발사를 요청합니다.
    /// </summary>
    public override void Interact(PlayerController player)
    {
        if (!CanInteract(player)) return;

        // 실제 발사 로직은 마스터 클라이언트가 실행하도록 요청을 보냅니다.
        // 이는 여러 명이 동시에 발사하는 것을 막고, 권한을 중앙에서 관리하기 위함입니다.
        pv.RPC("RequestFire_RPC", RpcTarget.MasterClient);
    }

    /// <summary>
    /// [마스터 클라이언트에서만 실행됨]
    /// 클라이언트의 요청을 받아 모든 클라이언트에게 대포를 발사하라고 명령합니다.
    /// </summary>
    [PunRPC]
    private void RequestFire_RPC()
    {
        // 쿨타임 재확인 (네트워크 지연 등으로 인한 중복 실행 방지)
        if (Time.time < lastFireTime + cooldownTime) return;

        // 모든 클라이언트에게 발사 RPC를 호출합니다.
        pv.RPC("FireCannon_RPC", RpcTarget.All);
    }

    /// <summary>
    /// [모든 클라이언트에서 실행됨]
    /// 실제 발사 로직과 쿨타임 초기화가 이루어집니다.
    /// </summary>
    [PunRPC]
    private void FireCannon_RPC()
    {
        // 쿨타임 갱신
        lastFireTime = Time.time;

        // 이 코드는 마스터 클라이언트에서만 실행되어야 네트워크 오브젝트가 정상적으로 생성됩니다.
        if (PhotonNetwork.IsMasterClient)
        {
            // PhotonNetwork.Instantiate를 사용하여 네트워크상의 모든 플레이어에게 포탄을 생성합니다.
            // 프리팹 이름으로 생성하므로, 프리팹은 반드시 'Resources' 폴더 안에 있어야 합니다.
            GameObject projectile = PhotonNetwork.Instantiate(projectilePrefab.name, firePoint.position, firePoint.rotation);

            // 포탄에 힘을 가합니다.
            if (projectile.GetComponent<Rigidbody>() != null)
            {
                projectile.GetComponent<Rigidbody>().AddForce(firePoint.forward * fireForce, ForceMode.Impulse);
            }
        }

        // 여기에 발사 사운드나 파티클 효과를 모든 클라이언트에서 재생하는 코드를 추가할 수 있습니다.
        // ex) audioSource.PlayOneShot(fireSound);
    }
}