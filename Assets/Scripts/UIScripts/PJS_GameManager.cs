using Photon.Pun;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Rendering; // Photon.Pun 네임스페이스 추가
using UnityEngine;
using UnityEngine.SceneManagement;

// 여러 대화 묶음을 관리하기 위한 클래스
[System.Serializable]
public class GameConversation
{
    public string conversationName; // 대화를 구분할 고유 이름 (예: "Dialogue1")
    public DialogueLine[] dialogueLines; // 실제 대화 내용
}

// MonoBehaviour를 MonoBehaviourPunCallbacks로 변경
public class PJS_GameManager : MonoBehaviourPunCallbacks
{
    public static PJS_GameManager Instance;
    public static bool isGameOver = false;

    [Header("UI 및 게임 상태")]
    public SharedLives sharedLives;
    //public CoolDown_UI coolDown_UI;
    public GameObject GameOverUI; // 게임 오버 UI 패널


    [Header("대화 목록")]
    public List<GameConversation> gameConversations;


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        if(GameOverUI != null)
        {
            GameOverUI.SetActive(false); // 게임 오버 UI를 비활성화
        }
    }

    void Update()
    {
        // 게임오버 상태이고, G키가 눌렸을 때
        if (isGameOver && Input.GetKeyDown(KeyCode.G))
        {
            // 방장(마스터 클라이언트)만 씬을 로드할 수 있음
            if (PhotonNetwork.IsMasterClient)
            {
                Debug.Log("StageSelectScene으로 돌아갑니다...");
                PhotonNetwork.LoadLevel("StageSelectScene");
            }
        }
    }

    // 이름을 기반으로 원하는 대화를 시작시키는 함수
    public void TriggerDialogue(string conversationName)
    {
        Debug.Log("TriggerDialogue");
        if (DialogueManager.Instance != null && DialogueManager.Instance.photonView != null)
        {
            // DialogueManager의 RPC를 호출하여 모든 클라이언트에서 대화 시작
            DialogueManager.Instance.photonView.RPC("StartDialogue_RPC", RpcTarget.All, conversationName);
        }
        else
        {
            Debug.LogError("DialogueManager 또는 PhotonView를 찾을 수 없습니다!");
        }
    }

    [PunRPC]
    public void ProcessPlayerDeath()
    {
        photonView.RPC("LoseLife_RPC", RpcTarget.All);
    }

    [PunRPC]
    public void LoseLife_RPC()
    {
        sharedLives.LoseLife(); // SharedLives의 UI 업데이트 함수 호출
        if (sharedLives.score <= 0)
        {
            // 게임 오버는 모든 클라이언트에서 동일하게 처리
            GameOver();
        }
    }

    void GameOver()
    {
        Debug.Log("게임 오버!");
        if (GameOverUI != null)
        {
            GameOverUI.SetActive(true); // 게임 오버 UI 활성화
        }
        isGameOver = true;
    }
}