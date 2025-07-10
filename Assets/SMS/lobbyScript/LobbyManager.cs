using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [SerializeField] GameObject mainPanel;
    [SerializeField] GameObject LobbyPanel;
    [SerializeField] GameObject LoadingPanel;
    [SerializeField] TextMeshProUGUI p1Text;
    [SerializeField] TextMeshProUGUI p2Text;
    [SerializeField] GameObject startButton;
    [SerializeField] Button penButton;
    [SerializeField] Button eraserButton;
    [SerializeField] TMP_InputField roomInput;
    [SerializeField] TMP_InputField joinInput;
    PhotonView lobbyView;

    string roomName;
    string joinName;
    // 메인 신 가서 직업 정보 저장을 위해 static
    public static class TempMemory
    {
        public static PlayerSaveData MySaveData = new PlayerSaveData();
    }
    private Dictionary<int, GameObject> playerList = new Dictionary<int, GameObject>();
    public void Start()
    {   lobbyView= GetComponent<PhotonView>();
        // 객체 유지를 위해 PlayerPrefs 에 랜덤한 UserId 저장, 플레이어 UserId에도 저장
        if (string.IsNullOrEmpty(PlayerPrefs.GetString("UserId")))
        {
            Debug.Log("기존 플레이어 없음!");
            PlayerPrefs.SetString("UserId", $"User_{Random.Range(1, 300):000}");

        }
        PhotonNetwork.AuthValues = new AuthenticationValues();
        PhotonNetwork.AuthValues.UserId = PlayerPrefs.GetString("UserId");

        Debug.Log("플레이어 UserId : "+PlayerPrefs.GetString("UserId"));
        Debug.Log(PhotonNetwork.AuthValues.UserId);

        PhotonNetwork.ConnectUsingSettings();
        // 플레이어들의 씬 동기화
        PhotonNetwork.AutomaticallySyncScene = true;
        LoadingPanel.SetActive(true);
        
        LobbyPanel.SetActive(false);
    }
    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Master");
        LoadingPanel.SetActive(false);
        mainPanel.SetActive(true);
        PhotonNetwork.JoinLobby();
    }
    public override void OnJoinedRoom()
    {
        UpdatePlayerList();
        startButton.SetActive(PhotonNetwork.IsMasterClient);

        string userId = PhotonNetwork.LocalPlayer.UserId;
        PlayerSaveData savedData = SaveSystem.LoadPlayerData(userId);

        // 직업 정보를 TempMemory에 저장
        if (savedData != null && !string.IsNullOrEmpty(savedData.userJob))
        {
            TempMemory.MySaveData = savedData;

            Debug.Log($"[LobbyManager] 이전에 선택한 직업: {savedData.userJob}");

            // CustomProperties에 복원
            ExitGames.Client.Photon.Hashtable jobProp = new ExitGames.Client.Photon.Hashtable();
            jobProp["userJob"] = savedData.userJob;
            PhotonNetwork.LocalPlayer.SetCustomProperties(jobProp);
        }
        else
        {
            Debug.Log("[LobbyManager] 저장된 직업 없음. 선택 필요");
        }

        // 버튼 초기화
        penButton.gameObject.SetActive(true);
        eraserButton.gameObject.SetActive(true);

        // 나와 상대방의 직업 상태를 바탕으로 버튼 비활성화
        Player[] players = PhotonNetwork.PlayerList;
        foreach (Player p in players)
        {
            if (p.CustomProperties.TryGetValue("userJob", out object jobObj))
            {
                string job = jobObj.ToString();

                if (job == "pen")
                    penButton.gameObject.SetActive(false);
                else if (job == "eraser")
                    eraserButton.gameObject.SetActive(false);
            }
        }
    }
    public void Room_Create()
    {
        roomName = roomInput.text;
    }
    public void Room_Join()
    {
        joinName = joinInput.text;
    }
    public void Lobby_Create()
    {
        RoomOptions rm = new RoomOptions();
        rm.MaxPlayers = 2;
        rm.PublishUserId = true;
       
        PhotonNetwork.CreateRoom(roomName, rm, TypedLobby.Default);
        Debug.Log($"[LobbyManager] {roomName} 생성 완료");
        mainPanel.SetActive(false);
        LobbyPanel.SetActive(true);
    }
    public void Lobby_Join()
    {   
        
        PhotonNetwork.JoinRoom(joinName);
        mainPanel.SetActive(false);
        LobbyPanel.SetActive(true);
    }
    public void Lobby_Left()
    {
        PhotonNetwork.LeaveRoom();
    }
    
    public void StartGame()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("MainScene"); // 메인 씬으로 전환
        }

    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdatePlayerList();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdatePlayerList();
    }
    public void UpdatePlayerList()
    {
        Player[] players = PhotonNetwork.PlayerList;
        
        // 두 개 슬롯 초기화
        p1Text.text = "";
        p2Text.text = "";

        if (players.Length > 0)
            p1Text.text = !string.IsNullOrEmpty(players[0].UserId) ? players[0].UserId : "Player 1";

        if (players.Length > 1)
            p2Text.text = !string.IsNullOrEmpty(players[1].UserId) ? players[1].UserId : "Player 2";
    }
    
    public void ChooseJob_Pen()
    {
        Hashtable props = new Hashtable();
        props["userJob"] = "pen";
        TempMemory.MySaveData.userJob = "pen";
        TempMemory.MySaveData.userId = PhotonNetwork.LocalPlayer.UserId;
        SaveSystem.SavePlayerData(TempMemory.MySaveData);
        PhotonNetwork.LocalPlayer.SetCustomProperties(props); // 서버에 저장
        penButton.gameObject.SetActive(false); // 본인 버튼 비활성화
        eraserButton.interactable = false;
       
    }

    public void ChooseJob_Eraser()
    {
        Hashtable props = new Hashtable();
        props["userJob"] = "eraser";
        TempMemory.MySaveData.userJob = "eraser";
        TempMemory.MySaveData.userId = PhotonNetwork.LocalPlayer.UserId;
        SaveSystem.SavePlayerData(TempMemory.MySaveData);
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        eraserButton.gameObject.SetActive(false);
        penButton.interactable= false;
    }
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps.ContainsKey("userJob"))
        {
            string chosenJob = changedProps["userJob"].ToString();
            Debug.Log($"{targetPlayer.UserId} chose {chosenJob}");

            // 그 직업 버튼을 비활성화
            if (chosenJob == "pen")
            {
                penButton.interactable = false;
            }
            else if (chosenJob == "eraser")
            {
                eraserButton.interactable = false;
            }
        }
    }
}
