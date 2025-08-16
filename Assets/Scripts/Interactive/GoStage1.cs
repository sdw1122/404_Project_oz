using Photon.Pun;
using UnityEngine;

public class GoStage1 : InteractableBase
{
    public string setStage;
    protected override void Awake()
    {
        base.Awake();
        
    }

    public override void Interact(PlayerController player)
    {
        // 마스터라면 바로, 클라이언트라면 마스터에게 RPC 요청
        if (PhotonNetwork.IsMasterClient)
            LoadStage1();
        else
            pv.RPC(nameof(RequestStageLoad), RpcTarget.MasterClient);
    }

    [PunRPC]
    public void RequestStageLoad()
    {
        // 마스터만 이 코드 실행
        if (PhotonNetwork.IsMasterClient)
            LoadStage1();
    }

    public void LoadStage1()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel(setStage); // 메인 씬으로 전환
        }
    }
}
