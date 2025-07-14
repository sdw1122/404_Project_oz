using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 한 줄의 대화에 필요한 데이터 (이름, 대사, 캐릭터 이미지)
[System.Serializable]
public struct DialogueLine
{
    public string speakerName;
    [TextArea(3, 10)]
    public string dialogueText;
    public Sprite characterSprite;
}

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("UI 요소")]
    public GameObject dialoguePanel;
    public Image characterImage;
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueText;

    [Header("타이핑 효과 설정")]
    public float typingSpeed = 0.20f;

    private Queue<DialogueLine> dialogueQueue;
    private bool isTyping = false;
    private string currentFullSentence; // ★ 현재 출력 중인 전체 문장을 저장할 변수

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

    public void StartDialogue(DialogueLine[] conversation)
    {
        //게임 일시정지
        Time.timeScale = 0f;

        dialoguePanel.SetActive(true);
        dialogueQueue.Clear();

        foreach (DialogueLine line in conversation)
        {
            dialogueQueue.Enqueue(line);
        }

        DisplayNextLine();
    }

    public void DisplayNextLine()
    {
        // 1. 타이핑 효과가 진행 중일 때 클릭한 경우
        if (isTyping)
        {
            StopAllCoroutines(); // 진행 중인 타이핑 코루틴을 즉시 중지
            dialogueText.text = currentFullSentence; // 전체 문장을 한 번에 표시
            isTyping = false; // 타이핑 상태 해제
            return; // 이번 클릭은 문장 완성으로 소모, 다음 대사로 넘어가지 않음
        }

        // 2. 남은 대사가 없는 경우
        if (dialogueQueue.Count == 0)
        {
            EndDialogue();
            return;
        }

        // 3. 다음 대사로 넘어가는 경우
        DialogueLine currentLine = dialogueQueue.Dequeue();

        speakerNameText.text = currentLine.speakerName;
        currentFullSentence = currentLine.dialogueText; // ★ 전체 문장 저장

        // 캐릭터 이미지가 할당되었을 때만 표시
        if (currentLine.characterSprite != null)
        {
            characterImage.sprite = currentLine.characterSprite;
            characterImage.enabled = true;
        }
        else
        {
            characterImage.enabled = false; // 이미지가 없으면 비활성화
        }

        // 새로운 문장 타이핑 시작
        StartCoroutine(TypeSentence(currentFullSentence));
    }

    //한 글자씩 타이핑
    IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        dialogueText.text = "";
        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter;
            //Time.scale의 영향 받지 않도록 WaitForSecondsRealtime 사용
            yield return new WaitForSecondsRealtime(typingSpeed);
        }
        isTyping = false;
    }

    void EndDialogue()
    {
        Time.timeScale = 1f; // 게임 재개
        dialoguePanel.SetActive(false);
        Debug.Log("대화가 종료되었습니다.");
    }

    void Update()
    {
        if (dialoguePanel.activeSelf && Input.GetMouseButtonDown(0))
        {
            DisplayNextLine();
        }
    }
}