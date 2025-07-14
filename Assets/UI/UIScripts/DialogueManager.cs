using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun; // Photon.Pun 네임스페이스 추가

// 한 줄의 대화에 필요한 데이터 구조체
[System.Serializable]
public struct DialogueLine
{
    public string speakerName;
    [TextArea(3, 10)]
    public string dialogueText;
    public Sprite characterSprite;
}

// MonoBehaviour를 MonoBehaviourPunCallbacks로 변경
public class DialogueManager : MonoBehaviourPunCallbacks
{
    public static DialogueManager Instance;

    [Header("UI 요소")]
    public GameObject dialoguePanel;
    public Image characterImage;
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueText;

    [Header("타이핑 효과 설정")]
    public float typingSpeed = 0.05f;

    private Queue<DialogueLine> dialogueQueue;
    private bool isTyping = false;
    private string currentFullSentence;

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
        dialogueQueue = new Queue<DialogueLine>();
    }

    void Start()
    {
        dialoguePanel.SetActive(false);
    }

    [PunRPC]
    public void StartDialogue_RPC(string conversationName)
    {
        // PJS_GameManager에서 해당 이름의 대화 데이터를 찾음
        GameConversation conversationToStart = PJS_GameManager.Instance.gameConversations.FirstOrDefault(c => c.conversationName == conversationName);

        if (conversationToStart == null)
        {
            Debug.LogWarning($"'{conversationName}' 라는 이름의 대화를 찾을 수 없습니다!");
            return;
        }

        // 모든 플레이어의 게임 시간을 멈춤
        Time.timeScale = 0f;

        dialoguePanel.SetActive(true);
        dialogueQueue.Clear();

        foreach (DialogueLine line in conversationToStart.dialogueLines)
        {
            dialogueQueue.Enqueue(line);
        }

        DisplayNextLine();
    }

    [PunRPC]
    public void EndDialogue_RPC()
    {
        // 모든 플레이어의 게임 시간을 재개
        Time.timeScale = 1f;
        dialoguePanel.SetActive(false);
        Debug.Log("대화가 종료되었습니다.");
    }

    public void DisplayNextLine()
    {
        if (dialogueQueue.Count == 0)
        {
            // 대화가 끝났음을 모든 클라이언트에게 알림
            photonView.RPC("EndDialogue_RPC", RpcTarget.All);
            return;
        }

        DialogueLine currentLine = dialogueQueue.Dequeue();
        speakerNameText.text = currentLine.speakerName;
        currentFullSentence = currentLine.dialogueText;

        if (currentLine.characterSprite != null)
        {
            characterImage.sprite = currentLine.characterSprite;
            characterImage.enabled = true;
        }
        else
        {
            characterImage.enabled = false;
        }

        StartCoroutine(TypeSentence(currentFullSentence));
    }

    IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        dialogueText.text = "";
        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSecondsRealtime(typingSpeed); // Time.timeScale 영향 안 받음
        }
        isTyping = false;
    }

    void Update()
    {
        // 대화창이 활성화된 상태에서 마스터 클라이언트만 입력을 받음
        if (PhotonNetwork.IsMasterClient && dialoguePanel.activeSelf && Input.GetMouseButtonDown(0))
        {
            if (isTyping)
            {
                // 타이핑 중이면 -> 모든 클라이언트에게 타이핑을 스킵하라고 명령
                photonView.RPC("SkipTyping_RPC", RpcTarget.All);
            }
            else
            {
                // 타이핑이 끝났으면 -> 모든 클라이언트에게 다음 줄을 표시하라고 명령
                photonView.RPC("SyncDisplayNextLine", RpcTarget.All);
            }
        }
    }
    [PunRPC]
    void SkipTyping_RPC()
    {
        // 모든 클라이언트에서 타이핑 코루틴을 멈추고 전체 텍스트를 즉시 표시
        isTyping = false;
        StopAllCoroutines();
        dialogueText.text = currentFullSentence;
    }

    [PunRPC]
    void SyncDisplayNextLine()
    {
        // isTyping 상태가 동기화 되었으므로, 이 함수는 항상 타이핑이 끝난 상태에서 호출됨
        DisplayNextLine();
    }
}