using Photon.Pun;
using System.Linq;
using UnityEngine;
using static LobbyManager;

public class GameManager : MonoBehaviourPun
{
    Vector3 spawnPos;
    PlayerSaveData savedData;
    string flagLabel;
   
    void Start()
    {        
        string userId = PhotonNetwork.LocalPlayer.UserId;
        
        // 저장된 데이터 로드
        savedData = SaveSystem.LoadPlayerData(userId);
        
        
        if (savedData != null)
        {
            //spawnPos = savedData.position;
            Debug.Log($"[GameManager] 저장된 위치로 스폰: {spawnPos}");
        }
        else
        {
            Debug.Log("[GameManager] 저장된 데이터 없음. 기본 위치 사용");
        }

        InstantiatePlayer();

        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("나는 마스터 클라이언트입니다.");
        }
        else
        {
            Debug.Log("나는 마스터가 아닙니다.");
        }
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
        // 로비에서 클릭한 깃발의 이름을 가져옴. 없으면 가장 최근 깃발
        string flagToUse = TempMemory.selectedFlagLabel ?? savedData.latestFlag;
        Debug.Log("선택깃발 : "+TempMemory.selectedFlagLabel);
        SaveFlag targetFlag = FindObjectsByType<SaveFlag>(default).FirstOrDefault(flag => flag.label == flagToUse);
        if(targetFlag!= null) 
        {
            spawnPos = targetFlag.SaveFlagGetSpawnPos(job).position; 
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
        PlayerSaveData saved = SaveSystem.LoadPlayerData(data.userId);
        if (saved == null)
        {
            saved = data; // 처음 저장하는 경우
        }
        else
        {
            // 기존 데이터 유지 + 업데이트

            saved.latestFlag = data.latestFlag;
            saved.latestScene = data.latestScene;
            saved.userJob = data.userJob;

            // 깃발 해금
            if (!saved.unlockedFlags.Contains(data.latestFlag))
            {
                saved.unlockedFlags.Add(data.latestFlag);
                Debug.Log($"[GameManager] 새로운 깃발 해금: {data.latestFlag}");
            }
        }

        // 저장
        SaveSystem.SavePlayerData(saved);

        Debug.Log($"[GameManager] Player {data.userId} 데이터 저장 완료");
    }
}
