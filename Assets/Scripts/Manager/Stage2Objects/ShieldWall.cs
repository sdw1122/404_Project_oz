using UnityEngine;
using Photon.Pun;

/// <summary>
/// [활성화/비활성화, 데미지 연동 버전] EnemyCannon의 공격을 막는 방어벽입니다.
/// </summary>
[RequireComponent(typeof(PhotonView))]
public class ShieldWall : MonoBehaviour, IPunObservable
{
    [Header("방어벽 설정")]
    [Tooltip("방어벽의 최대 체력입니다.")]
    [SerializeField] private float maxHealth = 100f;
    [Tooltip("방어벽이 활성화된 후 자동으로 사라지기까지의 시간(초)입니다.")]
    [SerializeField] private float lifetime = 30f;

    private float currentHealth;
    private bool isDeactivating = false;
    private PhotonView pv;

    void Awake()
    {
        pv = GetComponent<PhotonView>();
    }

    void OnEnable()
    {
        currentHealth = maxHealth;
        isDeactivating = false;
        if (PhotonNetwork.IsMasterClient)
        {
            CancelInvoke(nameof(RequestDeactivate));
            Invoke(nameof(RequestDeactivate), lifetime);
        }
    }

    void OnDisable()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            CancelInvoke(nameof(RequestDeactivate));
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!PhotonNetwork.IsMasterClient || isDeactivating) return;

        EnemyCannonBall cannonBall = collision.gameObject.GetComponent<EnemyCannonBall>();
        if (cannonBall != null)
        {
            // ▼▼▼ [수정] 포탄의 damage 값을 가져와서 체력 감소 ▼▼▼
            TakeDamage(cannonBall.damage);
            // ▲▲▲▲▲

            PhotonNetwork.Destroy(collision.gameObject);
        }
    }

    private void TakeDamage(float damage)
    {
        Debug.Log("ShieldWall TakeDamage: " + damage);
        currentHealth -= damage;
        Debug.Log("ShieldWall currentHealth: " + currentHealth);
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            RequestDeactivate();
        }
    }

    private void RequestDeactivate()
    {
        if (isDeactivating) return;
        isDeactivating = true;
        pv.RPC("RPC_DeactivateWall", RpcTarget.All);
    }

    [PunRPC]
    private void RPC_DeactivateWall()
    {
        gameObject.SetActive(false);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(currentHealth);
        }
        else
        {
            this.currentHealth = (float)stream.ReceiveNext();
        }
    }
}