using Photon.Pun;
using UnityEngine;
using static LobbyManager;

public class GameManager : MonoBehaviourPun
{
    Vector3 spawnPos = new Vector3(0, 1, 0);
    PlayerSaveData savedData;
    void Start()
    {
        string userId = PhotonNetwork.LocalPlayer.UserId;

        // 저장된 데이터 로드
        savedData = SaveSystem.LoadPlayerData(userId);

        if (savedData != null)
        {
            spawnPos = savedData.position;
            Debug.Log($"[GameManager] 저장된 위치로 스폰: {spawnPos}");
        }
        else
        {
            Debug.Log("[GameManager] 저장된 데이터 없음. 기본 위치 사용");
        }

        InstantiatePlayer();
    }

    // 플레이어 프리팹 생성
    void InstantiatePlayer()
    {
        string userId = PhotonNetwork.LocalPlayer.UserId;

        if (GameObject.Find(userId) != null)
        {
            Debug.Log("[GameManager] 이미 플레이어가 존재합니다!!");
            return;
        }
        // job에 따라 프리팹 선택
        string job = TempMemory.MySaveData != null ? TempMemory.MySaveData.userJob : "pen"; // 기본값은 pen
        /*if (savedData != null)
        {
            job = savedData.userJob;
        }*/
        string prefabName = "";

        switch (job)
        {
            case "pen":
                prefabName = "PenPlayer"; 
                break;
            case "eraser":
                prefabName = "EraserPlayer"; 
                break;
            default:
                prefabName = "Player"; // 예비 프리팹
                break;
        }

        GameObject player = PhotonNetwork.Instantiate(prefabName, spawnPos, Quaternion.identity);
        player.name = userId;
        // 본인 것일 때만 job 세팅
        if (player.GetComponent<PhotonView>().IsMine)
        {
            player.GetComponent<PhotonView>().RPC("SetJob", RpcTarget.AllBuffered, job);
        }
        Debug.Log($"[GameManager] 직업 프리팹 생성 완료: {prefabName}");
    }


    // 데이터 받음
    [PunRPC]
    public void ReceivePlayerData(string json)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        PlayerSaveData data = JsonUtility.FromJson<PlayerSaveData>(json);
        SaveSystem.SavePlayerData(data);

        Debug.Log($"[GameManager] Player {data.userId} 데이터 저장 완료: pos={data.position}");
    }
}
