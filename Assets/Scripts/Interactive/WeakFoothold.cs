using UnityEngine;
using System.Collections;
using Photon.Pun;

// [싱글 오브젝트 버전] 플레이어가 밟으면 잠시 후 부서지는 발판
public class WeakFoothold : InteractableBase
{
    [Header("발판 설정")]
    [Tooltip("이 오브젝트의 Rigidbody 컴포넌트입니다.")]
    [SerializeField] private Rigidbody rb;
    [Tooltip("이 오브젝트의 Collider 컴포넌트입니다.")]
    [SerializeField] private Collider objectCollider;

    [Tooltip("플레이어가 밟고 나서 부서지기까지 걸리는 시간(초)입니다.")]
    [SerializeField] private float timeToBreak = 1.5f;
    [Tooltip("부서진 후 네트워크에서 파괴되기까지 걸리는 시간(초)입니다.")]
    [SerializeField] private float destroyDelay = 3f;

    [Header("보강 설정")]
    [Tooltip("보강되었을 때 변경할 머티리얼입니다. (선택 사항)")]
    [SerializeField] private Material reinforcedMaterial;

    // 발판 상태 변수
    private bool isReinforced = false;
    private bool isBreaking = false;
    private Coroutine breakCoroutine;
    private Renderer objectRenderer;

    public ParticleSystem ps;

    protected override void Awake()
    {
        base.Awake(); // 부모의 Awake 실행 (PhotonView 초기화)
        rb = GetComponent<Rigidbody>();
        objectCollider = GetComponent<Collider>();
        objectRenderer = GetComponent<Renderer>();

        // 시작 시 물리 비활성화, 탐지를 위해 트리거 상태로 설정
        rb.isKinematic = true;
        objectCollider.isTrigger = true;
    }

    // [핵심 로직 1] 플레이어가 닿는 순간 물리 발판으로 변경
    private void OnTriggerEnter(Collider other)
    {
        // 플레이어가 아니거나, 이미 보강/파괴 중이면 무시
        if (!other.CompareTag("Player") || isReinforced || isBreaking) return;

        // 즉시 물리 발판으로 전환하여 플레이어가 밟고 설 수 있게 함
        objectCollider.isTrigger = false;

        // 파괴 코루틴 시작
        if (breakCoroutine == null)
        {
            breakCoroutine = StartCoroutine(BreakAfterDelay());
        }
    }

    // [핵심 로직 2] 발판 위에서 충돌이 유지되는 동안 호출됨
    private void OnCollisionExit(Collision collision)
    {
        // 플레이어가 발판에서 벗어났다면 (점프, 낙하 등)
        if (!collision.gameObject.CompareTag("Player") || isReinforced || isBreaking) return;

        // 파괴 코루틴 중단
        if (breakCoroutine != null)
        {
            StopCoroutine(breakCoroutine);
            breakCoroutine = null;
        }

        // 아무도 밟고 있지 않으면 다시 탐지 모드(트리거)로 전환
        objectCollider.isTrigger = true;
    }

    // '펜' 직업이 상호작용
    public override void Interact(PlayerController player)
    {
        if (!CanInteract(player)) return;
        pv.RPC("Reinforce_RPC", RpcTarget.AllBuffered);
    }

    // '펜'만, 그리고 보강되지 않았을 때만 상호작용 가능
    public override bool CanInteract(PlayerController player)
    {
        interactionType = EInteractionType.PenOnly;
        return !isReinforced && base.CanInteract(player);
    }

    // 지정 시간 후 파괴 RPC 호출
    private IEnumerator BreakAfterDelay()
    {
        yield return new WaitForSeconds(timeToBreak);
        pv.RPC("StartFalling_RPC", RpcTarget.All);
    }

    [PunRPC]
    private void StartFalling_RPC()
    {
        if (isReinforced || isBreaking) return;
        isBreaking = true;

        // 물리 활성화 (isTrigger는 이미 false 상태)
        rb.isKinematic = false;

        // 마스터 클라이언트만 네트워크 파괴 실행
        if (pv.IsMine)
        {
            StartCoroutine(DestroyAfterDelay());
        }
    }

    [PunRPC]
    private void Reinforce_RPC()
    {
        isReinforced = true;

        // 진행 중이던 파괴 코루틴이 있다면 확실히 중단
        if (breakCoroutine != null)
        {
            StopCoroutine(breakCoroutine);
            breakCoroutine = null;
        }

        // 영구적인 물리 발판으로 전환
        objectCollider.isTrigger = false;
        
        if(ps != null) ps.Play();

        // 머티리얼 변경
        if (objectRenderer != null && reinforcedMaterial != null)
        {
            objectRenderer.material = reinforcedMaterial;
        }
    }

    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(destroyDelay);
        PhotonNetwork.Destroy(gameObject);
    }
}