using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic; // List를 사용하기 위해 추가
using System.Linq; // Linq를 사용하기 위해 추가

// 여러 대화 묶음을 관리하기 위한 새로운 클래스
[System.Serializable]
public class GameConversation
{
    public string conversationName; // 대화를 구분할 고유 이름 (예: "인트로", "상황2")
    public DialogueLine[] dialogueLines; // 실제 대화 내용
}


public class PJS_GameManager : MonoBehaviour
{
    // --- 기존 변수들 ---
    public static PJS_GameManager Instance;
    public static bool IsGamePaused = false;
    public HealthBar healthBar;
    public SharedLives sharedLives;
    public CoolDown_UI coolDown_UI;

    // --- 대화 관리용 변수 추가 ---
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
    }

    public void Update()
    {
        if (DialogueManager.Instance != null && !DialogueManager.Instance.dialoguePanel.activeSelf)
        {
            // --- 기존 테스트 코드 ---
            if (Input.GetKeyDown(KeyCode.P))
            {
                healthBar.TakeDamage(10);
            }
            if (Input.GetKeyDown(KeyCode.H))
            {
                healthBar.ResetHealth();
            }
            // 'M' 키로 스킬 1 사용 (테스트용)
            if (Input.GetKeyDown(KeyCode.M))
            {
                coolDown_UI.StartCooldown1();
            }

            // 'N' 키로 스킬 2 사용 (테스트용)
            if (Input.GetKeyDown(KeyCode.N))
            {
                coolDown_UI.StartCooldown2();
            }

            // --- 새로운 대화 테스트 코드 ---
            // 숫자 '1' 키를 누르면 "인트로" 대화 시작
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                TriggerDialogue("Dialogue1");
            }

            // 숫자 '2' 키를 누르면 "상황2" 대화 시작
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                TriggerDialogue("Dialogue2");
            }
        }
    }

    // 이름을 기반으로 원하는 대화를 시작시키는 함수
    public void TriggerDialogue(string conversationName)
    {
        // 리스트에서 이름이 일치하는 대화를 찾음
        GameConversation conversationToStart = gameConversations.FirstOrDefault(c => c.conversationName == conversationName);

        if (conversationToStart != null)
        {
            // 찾았다면 DialogueManager에 대화 시작을 요청
            DialogueManager.Instance.StartDialogue(conversationToStart.dialogueLines);
        }
        else
        {
            Debug.LogWarning("'" + conversationName + "' 라는 이름의 대화를 찾을 수 없습니다!");
        }
    }


    // --- 기존 함수들 ---
    public void PlayerDied()
    {
        sharedLives.LoseLife();
        if (sharedLives.score > 0)
        {
            healthBar.ResetHealth();
        }
        else
        {
            GameOver();
        }
    }

    void GameOver()
    {
        Debug.Log("게임 오버!");
    }
}