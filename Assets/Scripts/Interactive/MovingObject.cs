using UnityEngine;
using System.Collections;
using Photon.Pun;

// InteractableBase를 상속받아 움직이는 객체를 구현합니다.
public class MovingObject : InteractableBase
{
    [Header("이동 설정")]
    [Tooltip("오브젝트가 이동할 목표 위치를 지정합니다.")]
    [SerializeField] private Transform targetTransform;

    [Tooltip("오브젝트의 이동 속도를 조절합니다.")]
    [SerializeField] private float moveSpeed = 2f;
    [Header("용암인가?")]
    [SerializeField] private bool isLava = false; // 용암인지 여부를 나타내는 변수입니다.

    private Vector3 startPosition;
    private Quaternion startRotation;
    private Vector3 targetPosition;
    private Quaternion targetRotation;

    private bool isMoved = false; // 오브젝트가 목표 위치에 있는지 여부를 추적합니다.
    public bool IsMoving { get; private set; }

    // Awake를 override하여 초기 위치를 저장합니다.
    protected override void Awake()
    {
        base.Awake(); // 부모 클래스의 Awake 실행 (PhotonView 초기화)

        // 초기 위치와 회전값 저장
        startPosition = transform.position;
        startRotation = transform.rotation;

        if (targetTransform != null)
        {
            // 목표 위치와 회전값 저장
            targetPosition = targetTransform.position;
            targetRotation = targetTransform.rotation;
        }
        else
        {
            Debug.LogError($"'{gameObject.name}' 오브젝트에 targetTransform이 할당되지 않았습니다.", this);
        }
        if (isLava)
        {
            gameObject.layer = LayerMask.NameToLayer("Lava");
            if (PhotonNetwork.IsMasterClient)
            {
                pv.RPC("ToggleMoveState", RpcTarget.All);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 이 오브젝트가 용암이 아니거나, 부딪힌 대상이 플레이어가 아니면 무시
        if (!isLava || !other.CompareTag("Player")) return;

        // 부딪힌 플레이어의 PlayerHealth 컴포넌트를 가져옴
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            // 플레이어의 최대 체력만큼의 데미지를 주어 즉사시킴
            playerHealth.OnDamage(playerHealth.startingHealth, other.transform.position, Vector3.zero);
        }
    }

    /// <summary>
    /// 상호작용 시 호출되는 메서드입니다. (PlayerController에서 호출)
    /// </summary>
    public override void Interact(PlayerController player)
    {
        // 직접 움직이지 않고, 모든 클라이언트에게 움직이라는 명령(RPC)을 보냅니다.
        // 이렇게 하면 상호작용한 플레이어뿐만 아니라 다른 모든 플레이어 화면에서도 오브젝트가 움직입니다.
        pv.RPC("ToggleMoveState", RpcTarget.All);
    }

    /// <summary>
    /// 모든 클라이언트에서 실행될 RPC 메서드입니다.
    /// 오브젝트의 상태를 바꾸고 이동 코루틴을 시작합니다.
    /// </summary>
    [PunRPC]
    public void ToggleMoveState()
    {
        // 상태 변경 (원래 위치 -> 목표 위치, 목표 위치 -> 원래 위치)
        isMoved = !isMoved;

        // 기존에 실행 중인 코루틴이 있다면 중지
        StopAllCoroutines();
        // 새 위치로 이동하는 코루틴 시작
        StartCoroutine(MoveToTarget());
    }

    /// <summary>
    /// 지정된 위치로 오브젝트를 부드럽게 이동시키는 코루틴입니다.
    /// </summary>
    private IEnumerator MoveToTarget()
    {
        // isMoved 상태에 따라 목적지(destination)와 목표 회전값(rotation)을 설정합니다.
        Vector3 destination = isMoved ? targetPosition : startPosition;
        Quaternion rotation = isMoved ? targetRotation : startRotation;
        IsMoving = true; // 이동 시작 상태로 설정

        // 목표 위치에 도달할 때까지 반복합니다.
        while (Vector3.Distance(transform.position, destination) > 0.01f)
        {
            // Lerp를 사용하여 현재 위치에서 목적지까지 부드럽게 이동
            transform.position = Vector3.Lerp(transform.position, destination, Time.deltaTime * moveSpeed);
            // Slerp를 사용하여 현재 회전에서 목표 회전까지 부드럽게 회전
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * moveSpeed);

            // 다음 프레임까지 대기
            yield return null;
        }

        // 루프 종료 후 정확한 위치와 회전값으로 설정
        transform.position = destination;
        transform.rotation = rotation;
        IsMoving = false; // 이동 완료 상태로 설정
    }
    public void TriggerMovement()
    {
        // 기존에 만들어둔 RPC를 그대로 호출하여 모든 클라이언트에서 움직이게 합니다.
        pv.RPC("ToggleMoveState", RpcTarget.All);
    }
}