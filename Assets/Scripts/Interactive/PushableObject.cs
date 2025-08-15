using UnityEngine;
using Photon.Pun;

// InteractableBase를 상속받아, 상호작용 시 밀려나는 객체를 구현합니다.
public class PushableObject : InteractableBase
{
    [Header("물리 설정")]
    [Tooltip("이 객체의 Rigidbody 컴포넌트입니다.")]
    [SerializeField] private Rigidbody rb;

    [Tooltip("객체를 밀어낼 힘의 크기를 조절합니다.")]
    [SerializeField] private float pushForce = 15f;

    private bool isPushed = false; // 이미 밀렸는지 확인하는 플래그

    protected override void Awake()
    {
        base.Awake(); // 부모 클래스의 Awake 실행 (PhotonView 초기화)
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        // 시작 시에는 물리 효과를 받지 않도록 Rigidbody를 Kinematic으로 설정합니다.
        if (rb != null)
        {
            rb.isKinematic = true;
        }
        else
        {
            Debug.LogError($"'{gameObject.name}' 오브젝트에 Rigidbody 컴포넌트가 없습니다.", this);
        }
    }

    /// <summary>
    /// 상호작용 시 호출되는 메서드입니다. (PlayerController에서 호출)
    /// </summary>
    public override void Interact(PlayerController player)
    {
        if (isPushed) return;// 이미 밀려난 상태라면 아무것도 하지 않습니다.
        

        // 모든 클라이언트에게 밀어내라는 명령(RPC)을 보냅니다.
        // 이때, 어느 방향으로 밀어낼지 계산하기 위해 상호작용한 플레이어의 위치를 함께 보냅니다.
        pv.RPC("PushAway", RpcTarget.All, player.transform.position);
    }

    /// <summary>
    /// 모든 클라이언트에서 실행될 RPC 메서드입니다.
    /// 객체를 지정된 위치의 반대 방향으로 밀어냅니다.
    /// </summary>
    [PunRPC]
    private void PushAway(Vector3 playerPosition)
    {
        isPushed = true; // 중복 실행 방지

        if (rb != null)
        {
            // isKinematic을 false로 만들어 물리 엔진의 영향을 받게 합니다.
            rb.isKinematic = false;

            // 밀어낼 방향을 계산합니다 (객체 위치 - 플레이어 위치 = 플레이어로부터 객체를 향하는 방향).
            Vector3 pushDirection = (transform.position - playerPosition).normalized;

            // 계산된 방향으로 순간적인 힘(Impulse)을 가합니다.
            rb.AddForce(pushDirection * pushForce, ForceMode.Impulse);
            isPushed = false; // 밀려난 후 다시 초기화
        }
    }
}