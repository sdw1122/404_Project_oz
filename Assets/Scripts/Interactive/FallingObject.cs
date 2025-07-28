using UnityEngine;
using Photon.Pun;

// InteractableBase를 상속받아 상호작용 시 떨어지는 객체를 구현합니다.
public class FallingObject : InteractableBase
{
    [Header("물리 설정")]
    [Tooltip("이 객체의 Rigidbody 컴포넌트입니다.")]
    [SerializeField] private Rigidbody rb;
    [Tooltip("낙하 시 충돌한 대상에게 입힐 데미지입니다.")]
    [SerializeField] private float damageOnImpact = 50f;

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
    /// 마스터 클라이언트에게 낙하를 요청합니다.
    /// </summary>
    public override void Interact(PlayerController player)
    {
        // 이미 떨어진 상태라면 아무것도 하지 않습니다.
        if (hasFallen) return;

        // 마스터 클라이언트에게 낙하 요청을 보냅니다.
        pv.RPC("RequestFall", RpcTarget.MasterClient);
    }

    /// <summary>
    /// [마스터 클라이언트에서만 실행됨]
    /// 클라이언트의 요청을 받아 모든 플레이어에게 낙하 명령을 내립니다.
    /// </summary>
    [PunRPC]
    private void RequestFall()
    {
        // 이미 떨어졌는지 다시 한번 확인 (중복 요청 방지)
        if (hasFallen) return;

        // 마스터 클라이언트가 모든 클라이언트에게 낙하 명령을 내립니다.
        pv.RPC("MakeFall", RpcTarget.All);
    }


    /// <summary>
    /// [모든 클라이언트에서 실행됨]
    /// Rigidbody의 상태를 변경하여 객체가 떨어지게 합니다.
    /// </summary>
    [PunRPC]
    private void MakeFall()
    {
        if (hasFallen) return; // 중복 실행 방지
        hasFallen = true;

        if (rb != null)
        {
            // isKinematic을 false로 만들어 물리 엔진의 영향을 받게 합니다.
            rb.isKinematic = false;
        }
    }

    /// <summary>
    /// 충돌을 감지하여 피해를 줍니다.
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        // 아직 떨어지지 않았거나, 땅(Ground)에 부딪힌 경우에는 데미지를 주지 않음
        if (!hasFallen || collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            return;
        }

        // 충돌한 상대방에게 LivingEntity 컴포넌트가 있는지 확인
        LivingEntity targetEntity = collision.gameObject.GetComponent<LivingEntity>();

        // LivingEntity가 있고, 아직 죽지 않았다면 데미지 처리
        if (targetEntity != null && !targetEntity.dead)
        {
            // 데미지 처리는 마스터 클라이언트가 담당하여 일관성을 유지
            if (PhotonNetwork.IsMasterClient)
            {
                // LivingEntity의 OnDamage는 이미 RPC이므로, 해당 PhotonView를 통해 직접 호출합니다.
                targetEntity.OnDamage(damageOnImpact, collision.contacts[0].point, collision.contacts[0].normal);

                // 데미지를 한 번 준 후에는 오브젝트를 네트워크에서 파괴하여 중복 피해를 방지합니다.
                //PhotonNetwork.Destroy(gameObject);
            }
        }
    }
}