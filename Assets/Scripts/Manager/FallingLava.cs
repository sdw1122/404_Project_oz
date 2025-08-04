using UnityEngine;
using System.Collections;
using Photon.Pun;

/// <summary>
/// 외부 신호에 의해 두 단계에 걸쳐 하강하고, 원래 위치로 복귀하는 용암 오브젝트입니다.
/// 각 단계의 하강 속도를 다르게 설정할 수 있습니다.
/// </summary>
[RequireComponent(typeof(PhotonView))]
public class FallingLava : MonoBehaviour
{
    [Header("이동 설정")]
    [Tooltip("1단계로 하강할 목표 위치를 지정합니다.")]
    [SerializeField] private Transform fallPosition;

    [Tooltip("2단계로 추가 하강할 목표 위치를 지정합니다.")]
    [SerializeField] private Transform deepFallPosition;

    [Tooltip("1단계 하강 속도를 조절합니다.")]
    [SerializeField] private float moveSpeed = 2f;

    // ----- [추가됨] 2단계 하강 속도 -----
    [Tooltip("2단계 추가 하강 속도를 조절합니다.")]
    [SerializeField] private float deepFallSpeed = 1f;
    // ----- [추가됨] -----


    private PhotonView pv;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private Coroutine moveCoroutine;

    void Awake()
    {
        pv = GetComponent<PhotonView>();

        // 초기 위치와 회전값 저장
        startPosition = transform.position;
        startRotation = transform.rotation;

        // 용암에 "Lava" 레이어 설정
        gameObject.layer = LayerMask.NameToLayer("Lava");
        // 시작 시 비활성화
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.OnDamage(playerHealth.startingHealth, other.transform.position, Vector3.zero);
        }
    }

    /// <summary>
    /// [외부 호출] 용암을 활성화하고 1단계 하강을 시작합니다.
    /// </summary>
    public void ActivateAndFall()
    {
        pv.RPC("SetState_RPC", RpcTarget.All, true);
        // 1단계 하강 시에는 기본 moveSpeed를 사용합니다.
        StartMoveTo(fallPosition.position, moveSpeed);
    }

    /// <summary>
    /// [외부 호출] 용암을 2단계로 추가 하강시킵니다.
    /// </summary>
    public void FallDeeper()
    {
        // 2단계 하강 시에는 deepFallSpeed를 사용합니다.
        StartMoveTo(deepFallPosition.position, deepFallSpeed);
    }

    /// <summary>
    /// [외부 호출] 용암을 원래 위치로 복귀시키고 비활성화합니다.
    /// </summary>
    public void ResetAndDeactivate()
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }
        pv.RPC("SetState_RPC", RpcTarget.All, false);
    }

    // ----- [수정됨] 목표 위치와 속도를 받아 이동을 시작하는 내부 함수 -----
    private void StartMoveTo(Vector3 destination, float speed)
    {
        pv.RPC("StartMovement_RPC", RpcTarget.All, destination, speed);
    }

    [PunRPC]
    private void StartMovement_RPC(Vector3 destination, float speed)
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }
        moveCoroutine = StartCoroutine(MoveToTarget(destination, speed));
    }

    [PunRPC]
    private void SetState_RPC(bool isActive)
    {
        if (isActive)
        {
            gameObject.SetActive(true);
        }
        else
        {
            transform.SetPositionAndRotation(startPosition, startRotation);
            gameObject.SetActive(false);
        }
    }

    // ----- [수정됨] 이동 코루틴에 속도 매개변수 추가 -----
    private IEnumerator MoveToTarget(Vector3 destination, float speed)
    {
        while (Vector3.Distance(transform.position, destination) > 0.01f)
        {
            transform.position = Vector3.Lerp(transform.position, destination, Time.deltaTime * speed);
            yield return null;
        }
        transform.position = destination;
    }
}