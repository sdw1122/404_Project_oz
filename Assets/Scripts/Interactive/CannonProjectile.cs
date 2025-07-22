using UnityEngine;
using Photon.Pun;

public class CannonProjectile : MonoBehaviour
{
    [Header("포탄 설정")]
    [Tooltip("포탄의 수명입니다. 이 시간이 지나면 자동으로 파괴됩니다.")]
    [SerializeField] private float lifetime = 5.0f;


    private PhotonView pv;

    void Awake()
    {
        pv = GetComponent<PhotonView>();
    }

    void Start()
    {
        // 이 포탄의 주체(MasterClient)만 파괴 타이머를 실행합니다.
        if (pv.IsMine)
        {
            Invoke(nameof(DestroyProjectile), lifetime);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
       
        // 이 포탄의 주체(MasterClient)만 포탄을 파괴할 권한을 가집니다.
        if (pv.IsMine)
        {
            DestroyProjectile();
        }
    }

    /// <summary>
    /// 네트워크상의 모든 클라이언트에서 이 포탄을 안전하게 파괴합니다.
    /// </summary>
    private void DestroyProjectile()
    {
        // 이미 파괴된 오브젝트에 대해 다시 호출하는 것을 방지
        if (gameObject.activeInHierarchy)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }
}