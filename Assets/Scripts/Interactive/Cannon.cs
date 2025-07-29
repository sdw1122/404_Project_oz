using UnityEngine;
using Photon.Pun;
using UnityEngine.InputSystem; // 새로운 입력 시스템 사용

public class Cannon : InteractableBase, IPunObservable
{
    [Header("탑승 및 조작 설정")]
    [Tooltip("플레이어가 탑승했을 때 카메라가 위치할 지점")]
    [SerializeField] private Transform cameraMountPoint;
    [Tooltip("플레이어가 내릴 위치")]
    [SerializeField] private Transform dismountPoint;
    [Tooltip("포신이 좌우로 회전하는 축 (Y축 회전)")]
    [SerializeField] private Transform turretPivot;
    [Tooltip("포신이 상하로 회전하는 축 (X축 회전)")]
    [SerializeField] private Transform barrelPivot;

    [Header("발사 설정")]
    [Tooltip("발사될 포탄 프리팹 ('Resources' 폴더 안에 있어야 함)")]
    [SerializeField] private GameObject projectilePrefab;
    [Tooltip("포탄이 생성될 위치")]
    [SerializeField] private Transform firePoint;
    [Tooltip("포탄 발사 힘")]
    [SerializeField] private float fireForce = 25f;
    [Tooltip("발사 쿨타임")]
    [SerializeField] private float fireCooldown = 1f;

    [Header("조준 설정")]
    [SerializeField] private float mouseSensitivity = 0.5f;
    [SerializeField] private float minPitch = -15f;
    [SerializeField] private float maxPitch = 45f;
    [SerializeField] private float yawRange = 90f;

    // --- 상태 변수 ---
    private bool isOccupied = false;
    private int occupantViewID = -1;
    private float lastFireTime = -99f;
    private float initialTurretY;

    // --- 탑승자 정보 캐싱 ---
    private Camera occupantCamera;
    private CharacterController occupantControllerComponent;
    private Renderer[] occupantRenderers;
    private Transform originalCameraParent;

    // --- 네트워크 동기화용 변수 ---
    private Quaternion networkTurretRotation;
    private Quaternion networkBarrelRotation;


    protected override void Awake()
    {
        base.Awake();
        initialTurretY = turretPivot.localRotation.eulerAngles.y;
        networkTurretRotation = turretPivot.localRotation;
        networkBarrelRotation = barrelPivot.localRotation;
    }

    private void Update()
    {
        // 탑승한 플레이어만 조작 가능
        if (isOccupied && PhotonView.Find(occupantViewID).IsMine)
        {
            HandleAiming();
            HandleFiring();
        }
        // 다른 클라이언트에서는 동기화된 회전값을 부드럽게 적용
        else
        {
            turretPivot.localRotation = Quaternion.Slerp(turretPivot.localRotation, networkTurretRotation, Time.deltaTime * 10);
            barrelPivot.localRotation = Quaternion.Slerp(barrelPivot.localRotation, networkBarrelRotation, Time.deltaTime * 10);
        }
    }

    public override void Interact(PlayerController player)
    {
        // 아무도 안 탔을 때 -> 탑승
        if (!isOccupied)
        {
            pv.RPC("MountRPC", RpcTarget.AllBuffered, player.GetComponent<PhotonView>().ViewID);
        }
        // 내가 타고 있을 때 -> 내리기
        else if (occupantViewID == player.GetComponent<PhotonView>().ViewID)
        {
            pv.RPC("DismountRPC", RpcTarget.AllBuffered);
        }
    }

    [PunRPC]
    private void MountRPC(int playerViewID)
    {
        isOccupied = true;
        occupantViewID = playerViewID;

        PhotonView playerPV = PhotonView.Find(playerViewID);
        if (playerPV == null) return;

        // 플레이어의 주요 컴포넌트들을 찾아서 저장
        occupantControllerComponent = playerPV.GetComponent<CharacterController>();
        occupantCamera = playerPV.GetComponentInChildren<Camera>();
        occupantRenderers = playerPV.GetComponentsInChildren<Renderer>();
        originalCameraParent = occupantCamera.transform.parent;

        // 플레이어 컨트롤 비활성화 및 숨기기
        occupantControllerComponent.enabled = false;
        foreach (var renderer in occupantRenderers)
        {
            renderer.enabled = false;
        }

        // 카메라를 대포의 조준 위치로 이동
        occupantCamera.transform.SetParent(cameraMountPoint);
        occupantCamera.transform.SetPositionAndRotation(cameraMountPoint.position, cameraMountPoint.rotation);

        // 탑승한 플레이어는 커서 고정
        if (playerPV.IsMine)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    [PunRPC]
    private void DismountRPC()
    {
        // 플레이어 컨트롤 활성화 및 보이기
        if (occupantControllerComponent != null) occupantControllerComponent.enabled = true;
        if (occupantRenderers != null)
        {
            foreach (var renderer in occupantRenderers)
            {
                if (renderer != null) renderer.enabled = true;
            }
        }

        // 카메라를 원래 위치로 복구
        if (occupantCamera != null)
        {
            occupantCamera.transform.SetParent(originalCameraParent);
            occupantCamera.transform.localPosition = Vector3.zero; // 부모 기준 원래 위치로
            occupantCamera.transform.localRotation = Quaternion.identity;
        }

        // 플레이어를 내리는 위치로 이동
        if (occupantControllerComponent != null)
        {
            // CharacterController가 비활성화된 상태에서 위치를 변경해야 순간이동이 가능
            occupantControllerComponent.transform.SetPositionAndRotation(dismountPoint.position, dismountPoint.rotation);
        }

        // 상태 초기화
        isOccupied = false;
        occupantViewID = -1;
    }

    private void HandleAiming()
    {
        // 새로운 입력 시스템에서 마우스 입력을 직접 읽어옴
        Vector2 lookInput = Mouse.current.delta.ReadValue();
        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime * 50; // deltaTime 보정
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime * 50;

        // 좌우 회전 (Turret)
        float currentYaw = turretPivot.localRotation.eulerAngles.y;
        currentYaw += mouseX;
        currentYaw = Mathf.Clamp(Mathf.DeltaAngle(initialTurretY, currentYaw), -yawRange, yawRange) + initialTurretY;
        turretPivot.localRotation = Quaternion.Euler(0, currentYaw, 0);

        // 상하 회전 (Barrel)
        float currentPitch = barrelPivot.localRotation.eulerAngles.x;
        currentPitch -= mouseY;
        currentPitch = Mathf.Clamp(Mathf.DeltaAngle(0, currentPitch), minPitch, maxPitch);
        barrelPivot.localRotation = Quaternion.Euler(currentPitch, 0, 0);
    }

    private void HandleFiring()
    {
        // 마우스 왼쪽 버튼 클릭 감지
        if (Mouse.current.leftButton.wasPressedThisFrame && Time.time > lastFireTime + fireCooldown)
        {
            pv.RPC("FireCannonRPC", RpcTarget.All);
        }
    }

    [PunRPC]
    private void FireCannonRPC()
    {
        lastFireTime = Time.time;
        // 마스터 클라이언트만 포탄 생성
        if (PhotonNetwork.IsMasterClient)
        {
            GameObject projectile = PhotonNetwork.Instantiate(projectilePrefab.name, firePoint.position, firePoint.rotation);
            if (projectile.GetComponent<Rigidbody>() != null)
            {
                projectile.GetComponent<Rigidbody>().AddForce(firePoint.forward * fireForce, ForceMode.Impulse);
            }
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(turretPivot.localRotation);
            stream.SendNext(barrelPivot.localRotation);
        }
        else
        {
            networkTurretRotation = (Quaternion)stream.ReceiveNext();
            networkBarrelRotation = (Quaternion)stream.ReceiveNext();
        }
    }
}