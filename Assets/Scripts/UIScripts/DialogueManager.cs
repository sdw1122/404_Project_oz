using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;

// 한 줄의 대화에 필요한 데이터 구조체
[System.Serializable]
public struct DialogueLine
{
    public string speakerName;
    [TextArea(3, 10)]
    public string dialogueText;
    public Sprite characterSprite;
}

public class DialogueManager : MonoBehaviourPunCallbacks
{
    public static DialogueManager Instance;
    PhotonView pv;

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

    public static bool IsDialogueActive { get; private set; } = false;

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
        pv = GetComponent<PhotonView>();                
    }

    void Start()
    {
        dialoguePanel.SetActive(false);
    }

    void Update()
    {
        if (PauseMenu.IsPaused) return;
        // 이제 마스터 클라이언트가 아니어도, 모든 클라이언트가 각자 자신의 클릭 입력을 처리합니다.
        if (dialoguePanel.activeSelf && Input.GetMouseButtonDown(0))
        {
            pv.RPC("AdvanceDialogue", RpcTarget.All);
        }
    }

    // [PunRPC] 속성을 유지하여 마스터 클라이언트가 모두의 대화를 시작시킬 수 있도록 합니다.
    [PunRPC]
    public void StartDialogue_RPC(string conversationName)
    {
        GameConversation conversationToStart = PJS_GameManager.Instance.gameConversations.FirstOrDefault(c => c.conversationName == conversationName);

        if (conversationToStart == null)
        {
            Debug.LogWarning($"'{conversationName}' 라는 이름의 대화를 찾을 수 없습니다!");
            return;
        }

        // Time.timeScale = 0f; // 게임 시간을 멈추는 코드를 제거합니다.

        dialoguePanel.SetActive(true);        
        IsDialogueActive = true;
        dialogueQueue.Clear();

        foreach (DialogueLine line in conversationToStart.dialogueLines)
        {
            dialogueQueue.Enqueue(line);
        }

        DisplayNextLine();
    }

    [PunRPC]
    public void NextDialogue()
    {

    }

    // RPC가 아닌 일반 로컬 함수로 변경합니다.
    public virtual void EndDialogue()
    {
        // Time.timeScale = 1f; // 게임 시간을 되돌리는 코드를 제거합니다.
        dialoguePanel.SetActive(false);
        IsDialogueActive = false;        
        Debug.Log("대화가 종료되었습니다.");
    }

    public void DisplayNextLine()
    {
        if (dialogueQueue.Count == 0)
        {
            // 모든 클라이언트에게 종료 신호를 보내는 대신, 로컬에서 대화를 종료 처리합니다.
            EndDialogue();
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

        // 게임 시간이 멈추지 않으므로 WaitForSeconds를 사용해도 괜찮습니다.
        StartCoroutine(TypeSentence(currentFullSentence));
    }

    IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        dialogueText.text = "";
        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
        isTyping = false;
    }

    [PunRPC]
    // RPC가 아닌, 각 클라이언트가 로컬에서 호출하는 일반 함수로 변경합니다.
    public void AdvanceDialogue()
    {
        // 기존 AdvanceDialogue_RPC의 로직을 그대로 가져옵니다.
        if (isTyping)
        {
            isTyping = false;
            StopAllCoroutines();
            dialogueText.text = currentFullSentence;
        }
        else
        {
            DisplayNextLine();
        }
    }
}