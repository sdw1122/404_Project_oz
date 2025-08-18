using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

// 포톤 콜백을 사용하기 위해 MonoBehaviourPunCallbacks 상속
public class StageSelectManager : MonoBehaviourPunCallbacks
{
    [Header("UI 요소")]
    [Tooltip("Stage 1 선택 버튼")]
    [SerializeField] private Button stage1Button;

    [Tooltip("Stage 2 선택 버튼")]
    [SerializeField] private Button stage2Button;

    [Tooltip("tutorial 버튼")]
    [SerializeField] private Button tutorialButton;

    void Start()
    {
        // 씬에 들어왔을 때 UI 상태를 업데이트합니다.
        UpdateButtonsForMasterClient();
    }

    /// <summary>
    /// 마스터 클라이언트가 바뀌었을 때 자동으로 호출되는 콜백 함수입니다.
    /// </summary>
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        // 마스터 클라이언트가 변경되면 UI를 다시 업데이트하여 새 마스터가 버튼을 누를 수 있게 합니다.
        UpdateButtonsForMasterClient();
    }

    private void UpdateButtonsForMasterClient()
    {
        bool isMaster = PhotonNetwork.IsMasterClient;

        // 마스터 클라이언트만 버튼을 누를 수 있도록 설정합니다.
        if (stage1Button != null) stage1Button.interactable = isMaster;
        if (stage2Button != null) stage2Button.interactable = isMaster;
        if (tutorialButton != null) tutorialButton.interactable = isMaster;
    }

    // --- 버튼 OnClick() 이벤트에 연결할 함수들 ---

    public void LoadStage1()
    {
        // 마스터 클라이언트만 스테이지를 로드할 수 있습니다.
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("Stage 1을 로드합니다...");
            PhotonNetwork.LoadLevel("Stage1"); // Stage1 씬의 이름
        }
    }

    public void LoadStage2()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("Stage 2를 로드합니다...");
            PhotonNetwork.LoadLevel("Stage2"); // Stage2 씬의 이름
        }
    }

    public void LoadStage1Bosss()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("Stage 1 보스전을 로드합니다...");
            PhotonNetwork.LoadLevel("Stage1Boss"); // Stage1Boss 씬의 이름
        }
    }

    public void LoadStage2Bosss()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("Stage 2 보스전을 로드합니다...");
            PhotonNetwork.LoadLevel("Stage2 Boss"); // Stage2Boss 씬의 이름
        }
    }

    public void LoadTutorial()
    {
        if(PhotonNetwork.IsMasterClient)
        {
            Debug.Log("튜토리얼을 로드합니다...");
            PhotonNetwork.LoadLevel("TutorialScene"); // Tutorial 씬의 이름
        }
    }

    public void BackToLobby()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("로비로 돌아갑니다...");
            PhotonNetwork.LoadLevel("Lobby"); // 로비 씬의 이름
        }
    }

    public void StartStory()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("스토리를 시작합니다...");
            PhotonNetwork.LoadLevel("StartStory"); // 스토리 씬의 이름
        }
    }
}