using UnityEngine;
using Photon.Pun;

public class CreateObject : InteractableBase
{
    [Header("생성 설정")]
    [Tooltip("생성할 오브젝트의 프리팹을 지정합니다. 반드시 'Resources' 폴더 안에 있어야 합니다.")]
    [SerializeField] private GameObject objectToSpawn;

    [Tooltip("오브젝트가 생성될 위치를 지정합니다. 지정하지 않으면 이 오브젝트의 위치에 생성됩니다.")]
    [SerializeField] private Transform spawnPoint;
    private string resourcePath = "test";
    private bool isUsed = false; // 중복 생성을 막기 위한 플래그

    protected override void Awake()
    {
        base.Awake();
        if (spawnPoint == null)
        {
            spawnPoint = this.transform;
        }
    }

    public override void Interact(PlayerController player)
    {
        // 이미 사용되었다면 아무것도 하지 않음
        if (isUsed || objectToSpawn == null) return;

        // 상호작용한 플레이어만 네트워크 오브젝트를 생성
        PhotonNetwork.Instantiate(resourcePath + objectToSpawn.name, spawnPoint.position, spawnPoint.rotation);

        // 모든 클라이언트에게 이 생성기 오브젝트를 파괴하라고 요청
        pv.RPC("DestroyGenerator", RpcTarget.All);
    }

    [PunRPC]
    private void DestroyGenerator()
    {
        isUsed = true; // 다른 클라이언트에서 RPC가 약간 늦게 도착하더라도 중복 실행 방지

        // 마스터 클라이언트만 이 오브젝트를 파괴하도록 하여 충돌을 방지
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }
}