using UnityEngine;
using Photon.Pun;

/// <summary>
/// 플레이어가 상호작용하면 해당 플레이어의 방향으로 힘을 받아 넘어지는 오브젝트입니다.
/// </summary>
public class FallTowardPlayer : InteractableBase
{
    [Header("물리 설정")]
    [Tooltip("이 객체의 Rigidbody 컴포넌트입니다.")]
    [SerializeField] private Rigidbody rb;

    [Tooltip("플레이어 방향으로 밀 때 가할 힘의 크기입니다.")]
    [SerializeField] private float pushForce = 10f;

    [Tooltip("낙하 시 충돌한 대상에게 입힐 데미지입니다.")]
    [SerializeField] private float damageOnImpact = 30f;

    private bool hasFallen = false; // 중복 상호작용을 막기 위한 플래그

    protected override void Awake()
    {
        base.Awake(); // 부모 클래스의 Awake 실행
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        // 시작 시에는 물리 효과를 받지 않도록 Kinematic으로 설정
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
    /// 플레이어와 상호작용 시 호출됩니다.
    /// </summary>
    public override void Interact(PlayerController player)
    {
        if (hasFallen) return;

        // 플레이어의 위치를 RPC로 함께 보내 모든 클라이언트에게 알립니다.
        pv.RPC("FallTowards_RPC", RpcTarget.All, player.transform.position);
    }

    /// <summary>
    /// 모든 클라이언트에서 실행되어 오브젝트를 넘어뜨리는 RPC 메서드입니다.
    /// </summary>
    /// <param name="playerPosition">상호작용한 플레이어의 위치</param>
    [PunRPC]
    private void FallTowards_RPC(Vector3 playerPosition)
    {
        // 이미 넘어졌으면 중복 실행하지 않음
        if (hasFallen) return;
        hasFallen = true;

        if (rb != null)
        {
            // isKinematic을 풀어 물리 효과를 받게 함
            rb.isKinematic = false;

            // [핵심 로직]
            // 오브젝트에서 플레이어 위치로 향하는 방향 벡터를 계산합니다.
            Vector3 direction = playerPosition - transform.position;

            // 오브젝트가 위로 솟구치지 않도록 수평 방향으로만 힘을 가합니다.
            direction.y = 0;

            // 계산된 방향으로 힘을 가합니다. ForceMode.Impulse는 순간적인 힘을 가할 때 적합합니다.
            rb.AddForce(direction.normalized * pushForce, ForceMode.Impulse);
        }
    }

    /// <summary>
    /// 충돌을 감지하여 피해를 줍니다.
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        // 아직 넘어지지 않았거나, 땅(Ground)에 부딪힌 경우에는 데미지를 주지 않음
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
                targetEntity.OnDamage(damageOnImpact, collision.contacts[0].point, collision.contacts[0].normal);

                // 데미지를 한 번 준 후에는 오브젝트를 네트워크에서 파괴하여 중복 피해를 방지합니다.
                PhotonNetwork.Destroy(gameObject);
            }
        }
    }
}