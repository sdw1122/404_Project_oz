using Photon.Pun;
using UnityEngine;

public class SaveFlag : InteractableBase
{
    [Header("라벨 설정.")]
    public string label;
    public string sceneName;
    public Transform penSpawnPos;
    public Transform eraserSpawnPos;
    protected override void Awake()
    {
        base.Awake();

    }
    public override void Interact(PlayerController player)
    {
        
        //RPC호출
        pv.RPC("SaveFlagRPC", RpcTarget.MasterClient);
    }

    [PunRPC]
    public void SaveFlagRPC()
    {
        if (pv != null && pv.IsMine && PhotonNetwork.IsMasterClient)
        {
            Debug.Log("SaveFlag 작동 - 저장 요청 시작");

            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            foreach (GameObject player in players)
            {
                PhotonView targetPV = player.GetComponent<PhotonView>();
                PlayerController play = player.GetComponent<PlayerController>();
                if (targetPV != null)
                {
                    string flag = label;
                    string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                    
                    // 각 플레이어에게 위치 요청 보내기
                    targetPV.RPC("SendMyDataToHost", targetPV.Owner,flag,scene);
                }
            }
        }
    }

    public Transform SaveFlagGetSpawnPos(string job)
    {
        if (job == "pen")
            return penSpawnPos;
        else if(job=="eraser")
            return eraserSpawnPos;
        return penSpawnPos;
    }
}
