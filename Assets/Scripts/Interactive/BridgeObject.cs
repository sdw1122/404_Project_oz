using UnityEngine;
using Photon.Pun;
using System.Collections;

// Hinge Joint를 사용하여 물리 기반으로 넘어지는 다리 객체
public class BridgeObject : InteractableBase
{
    [Header("물리 설정")]
    [Tooltip("객체의 Rigidbody 컴포넌트입니다.")]
    [SerializeField] private Rigidbody rb;

    [Tooltip("힘을 가할 위치를 지정하는 Transform 입니다. (객체의 위쪽 끝에 배치)")]
    [SerializeField] private Transform forcePoint;

    [Tooltip("넘어뜨릴 때 가하는 힘의 크기입니다.")]
    [SerializeField] private float pushForce = 500f;

    [Tooltip("넘어진 후 다리가 완전히 고정될 때까지의 시간입니다.")]
    [SerializeField] private float timeToSettle = 4f;

    private bool hasFallen = false;

    protected override void Awake()
    {
        base.Awake();
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }


        if (rb != null)
        {
            rb.isKinematic = true;
        }
        else
        {
            Debug.LogError($"'{gameObject.name}' 오브젝트에 Rigidbody 컴포넌트가 없습니다.", this);
        }
    }

    public override void Interact(PlayerController player)
    {
        if (hasFallen || forcePoint == null) return;

        

        // 플레이어의 위치를 기준으로, 객체를 밀어낼 방향을 결정
        // (플레이어 -> 객체 방향에서 수평 방향만 사용)
        Vector3 pushDirection = forcePoint.position - player.transform.position;
        pushDirection.y = 0; // 수평으로만 밀도록 y축 제거

        // 모든 클라이언트에게 계산된 힘의 방향과 위치를 전달하여 객체를 밀도록 함
        pv.RPC("PushOver", RpcTarget.All, pushDirection.normalized * pushForce, forcePoint.position);
    }

    [PunRPC]
    private void PushOver(Vector3 force, Vector3 position)
    {
        // isKinematic 변경과 물리 효과 적용이 같은 RPC 안에서 실행되도록 순서를 보장합니다.
        if (hasFallen) return;
        hasFallen = true;

        if (rb != null)
        {
            // 1. 모든 클라이언트에서 isKinematic을 false로 만들어 물리 엔진의 영향을 받게 합니다.
            rb.isKinematic = false;

            // 2. 지정된 위치에 지정된 힘을 가합니다.
            rb.AddForceAtPosition(force, position);

            // 3. 일정 시간 후 다리를 완전히 고정시키는 코루틴 실행
            StartCoroutine(SettleBridge());
        }
    }

    // 다리가 넘어진 후 안정화시키고 고정하는 코루틴
    private IEnumerator SettleBridge()
    {
        yield return new WaitForSeconds(timeToSettle);

        // 물리 시뮬레이션이 끝난 후에는 움직이지 않도록 Kinematic으로 전환
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        // Hinge Joint가 더는 필요 없으므로 비활성화하거나 파괴
        HingeJoint joint = GetComponent<HingeJoint>();
        if (joint != null)
        {
            joint.breakForce = 0; // 조인트 연결을 약하게 만들어 사실상 끊어지게 함
            // Destroy(joint); // 혹은 바로 파괴
        }
    }

    public override bool CanInteract(PlayerController player)
    {
        return !hasFallen && base.CanInteract(player);
    }
}