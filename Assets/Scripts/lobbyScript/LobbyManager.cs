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
    [SerializeField] GameObject flagButtonPrefab;
    [SerializeField] Transform flagListParent;   // 버튼 생성 위치

    // --- 추가된 변수 ---
    [Header("직업 선택 UI")]
    [SerializeField] Image p1JobImage; // 플레이어 1의 직업 이미지 UI
    [SerializeField] Image p2JobImage; // 플레이어 2의 직업 이미지 UI
    [SerializeField] Sprite penSprite;    // '펜' 직업 스프라이트
    [SerializeField] Sprite eraserSprite; // '지우개' 직업 스프라이트
    // --- 추가된 변수 ---

    string roomName;
    string joinName;
    // 메인 신 가서 직업 정보 저장을 위해 static
    public static class TempMemory
    {
        public static PlayerSaveData MySaveData = new PlayerSaveData();
        public static string selectedFlagLabel = null;  //선택한 깃발 전달용
    }
    private Dictionary<int, GameObject> playerList = new Dictionary<int, GameObject>();
    public void Start()
    {
        AudioManager.instance.PlayBgm("Main Menu");
        lobbyView = GetComponent<PhotonView>();
        Debug.Log($"[LobbyManager] PhotonView ID: {lobbyView.ViewID}, IsMine: {lobbyView.IsMine}");
        // 객체 유지를 위해 PlayerPrefs 에 랜덤한 UserId 저장, 플레이어 UserId에도 저장
        if (string.IsNullOrEmpty(PlayerPrefs.GetString("UserId")))
        {
            Debug.Log("기존 플레이어 없음!");
            PlayerPrefs.SetString("UserId", $"User_{Random.Range(1, 300):000}");

        }
        PhotonNetwork.AuthValues = new AuthenticationValues();
        PhotonNetwork.AuthValues.UserId = PlayerPrefs.GetString("UserId");

        Debug.Log("플레이어 UserId : " + PlayerPrefs.GetString("UserId"));
        Debug.Log(PhotonNetwork.AuthValues.UserId);

        PhotonNetwork.ConnectUsingSettings();
        // 플레이어들의 씬 동기화
        PhotonNetwork.AutomaticallySyncScene = true;
        LoadingPanel.SetActive(true);

        LobbyPanel.SetActive(false);
    }
    [PunRPC]
    public void SyncClickedFlag(string label)
    {
        TempMemory.selectedFlagLabel = label;
    }
    [PunRPC]
    void RPC_ShowUnlockedFlags(string[] unlockedFlagArray)
    {
        Debug.Log("Show깃발호출됨");
        // 기존 버튼 제거
        foreach (Transform child in flagListParent)
        {
            Destroy(child.gameObject);
        }

        // 깃발 데이터 로드 (Resources/flagData.json 사용)
        FlagDataManager.Load();

        // UI 생성
        foreach (string label in unlockedFlagArray)
        {
            GameObject buttonObj = Instantiate(flagButtonPrefab, flagListParent);
            FlagButtonHandler handler = buttonObj.GetComponent<FlagButtonHandler>();
            handler.Initialize(label);
        }

        Debug.Log($"[LobbyManager] 깃발 {unlockedFlagArray.Length}개 UI 생성 완료");
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
        if (PhotonNetwork.IsMasterClient)
        {
            string masterId = PhotonNetwork.LocalPlayer.UserId;
            PlayerSaveData masterData = SaveSystem.LoadPlayerData(masterId);

            if (masterData != null)
            {
                string[] unlocked = masterData.unlockedFlags?.ToArray() ?? new string[0];
                Debug.Log("깃발: " + string.Join(", ", unlocked));
                // 전체 클라이언트에게 RPC 호출
                lobbyView.RPC("RPC_ShowUnlockedFlags", RpcTarget.All, new object[] { unlocked });
            }
            else
            {
                Debug.LogWarning("[LobbyManager] 마스터의 SaveData를 찾을 수 없습니다.");
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
            PhotonNetwork.LoadLevel("StartStory"); // 메인 씬으로 전환
        }
    }
    public void Stage1()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("Stage1"); // 메인 씬으로 전환
        }
    }
    public void Tutorial()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("Tutorial"); // 메인 씬으로 전환
        }
    }

    public void BossRoom()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("Stage1Boss"); // 메인 씬으로 전환
        }
    }
    public void EndRoom()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("EndingScene"); // 메인 씬으로 전환
        }
    }
    public void StageSelectScene()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("StageSelectScene");
        }
    }

    public void ProtoTypeScene()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("MainScene"); // 메인 씬으로 전환
        }
    }

    public void Stage2BossScene()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("Stage2 Boss"); // Stage2으로 전환
        }
    }

    public void Stage2Scene()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("Stage2"); // Stage2으로 전환
        }
    }

    public void CreditScene()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("Credit"); // Stage2으로 전환
        }
    }



    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdatePlayerList();
        if (PhotonNetwork.IsMasterClient)
        {
            string masterId = PhotonNetwork.LocalPlayer.UserId;
            PlayerSaveData masterData = SaveSystem.LoadPlayerData(masterId);

            if (masterData != null)
            {
                string[] unlocked = masterData.unlockedFlags?.ToArray() ?? new string[0];

                // 전체 클라이언트에게 RPC 호출
                lobbyView.RPC("RPC_ShowUnlockedFlags", RpcTarget.All, new object[] { unlocked });
            }
            else
            {
                Debug.LogWarning("[LobbyManager] 마스터의 SaveData를 찾을 수 없습니다.");
            }
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdatePlayerList();
    }

    // --- 수정된 함수 ---
    public void UpdatePlayerList()
    {
        Player[] players = PhotonNetwork.PlayerList;

        // Player 1 UI 업데이트
        UpdatePlayerSlotUI(players.Length > 0 ? players[0] : null, p1Text, p1JobImage);

        // Player 2 UI 업데이트
        UpdatePlayerSlotUI(players.Length > 1 ? players[1] : null, p2Text, p2JobImage);
    }

    // --- 새로 추가된 함수 ---
    void UpdatePlayerSlotUI(Player player, TextMeshProUGUI playerText, Image jobImage)
    {
        // 플레이어가 슬롯에 없으면 UI를 비웁니다.
        if (player == null)
        {
            playerText.text = "Waiting...";
            jobImage.gameObject.SetActive(false);
            return;
        }

        // 플레이어 이름(ID) 표시
        playerText.text = !string.IsNullOrEmpty(player.UserId) ? player.UserId : "Player";

        // 플레이어의 직업 속성을 확인하고 이미지를 설정합니다.
        if (player.CustomProperties.TryGetValue("userJob", out object job))
        {
            jobImage.gameObject.SetActive(true);
            if (job.ToString() == "pen")
            {
                jobImage.sprite = penSprite;
            }
            else if (job.ToString() == "eraser")
            {
                jobImage.sprite = eraserSprite;
            }
        }
        else
        {
            // 직업이 선택되지 않았으면 이미지를 숨깁니다.
            jobImage.gameObject.SetActive(false);
        }
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
        penButton.interactable = false;
    }
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        // 직업 속성이 변경될 때마다 모든 플레이어의 UI를 업데이트합니다.
        if (changedProps.ContainsKey("userJob"))
        {
            UpdatePlayerList();

            // 다른 플레이어가 선택한 직업 버튼을 비활성화합니다.
            string chosenJob = changedProps["userJob"].ToString();
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