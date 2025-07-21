using UnityEngine;
using Photon.Pun;

// InteractableBase를 상속받아 상호작용 시 떨어지는 객체를 구현합니다.
public class FallingObject : InteractableBase
{
    [Header("물리 설정")]
    [Tooltip("이 객체의 Rigidbody 컴포넌트입니다.")]
    [SerializeField] private Rigidbody rb;

    private bool hasFallen = false; // 이미 떨어졌는지 확인하는 플래그

    // Awake를 override하여 Rigidbody 컴포넌트를 초기화합니다.
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
    /// 상호작용 시 호출되는 메서드입니다.
    /// </summary>
    public override void Interact(PlayerController player)
    {
        // 이미 떨어진 상태라면 아무것도 하지 않습니다.
        if (hasFallen) return;

        // 모든 클라이언트에게 떨어지라는 명령(RPC)을 보냅니다.
        pv.RPC("MakeFall", RpcTarget.All);
    }

    /// <summary>
    /// 모든 클라이언트에서 실행될 RPC 메서드입니다.
    /// Rigidbody의 상태를 변경하여 객체가 떨어지게 합니다.
    /// </summary>
    [PunRPC]
    private void MakeFall()
    {
        hasFallen = true; // 중복 실행 방지

        if (rb != null)
        {
            // isKinematic을 false로 만들어 물리 엔진의 영향을 받게 합니다.
            rb.isKinematic = false;
        }
    }
}