<<<<<<< HEAD
using UnityEngine;

public class Boss2Dialogue : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
=======
using Unity.VisualScripting;
using UnityEngine;

public class Boss2Dialogue : DialogueManager
{
    [Tooltip("PJS_GameManager에 설정된 대화의 이름(conversationName)을 입력하세요.")]
    [SerializeField] private string conversationNameToTrigger;

    public override void EndDialogue()
    {
        base.EndDialogue();
        if (currentConversationName == "BossEnd")
        {

        }
    }

    public void RequestDialogueStart_RPC()
    {
        // PJS_GameManager에 있는 대화 트리거 함수를 호출합니다.
        if (PJS_GameManager.Instance != null)
        {
            PJS_GameManager.Instance.TriggerDialogue(conversationNameToTrigger);
        }
        else
        {
            Debug.LogError("PJS_GameManager의 인스턴스를 찾을 수 없습니다!");
        }
>>>>>>> origin/ver_0.92
    }
}
