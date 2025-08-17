using UnityEngine;
using Photon.Pun;

// InteractableBase를 상속받아 대화 트리거 기능을 구현합니다.
public class BossEnd : DialogueManager
<<<<<<< HEAD
{
    PhotonView pv;
=======
{    
>>>>>>> origin/ver_0.92
    [Header("대화 설정")]
    [Tooltip("PJS_GameManager에 설정된 대화의 이름(conversationName)을 입력하세요.")]
    [SerializeField] private string conversationNameToTrigger;

    [Tooltip("체크하면 대화를 한 번만 실행합니다.")]
    [SerializeField] private bool isOneTimeUse = true;

    private bool hasBeenUsed = false;

    private bool isEnd = false;

    public void Start()
    {
<<<<<<< HEAD
        pv = GetComponent<PhotonView>();
=======
        
>>>>>>> origin/ver_0.92
    }

    public void TriggerDialogueIfMaster()
    {
        if (!PhotonNetwork.IsMasterClient) return;        

        pv.RPC("RequestDialogueStart_RPC", RpcTarget.MasterClient);
    }

    [PunRPC]
    private void RequestDialogueStart_RPC()
    {
        // 아직 대화가 시작되지 않았을 경우에만 실행합니다.
        if (isOneTimeUse && hasBeenUsed) return;

        // PJS_GameManager에 있는 대화 트리거 함수를 호출합니다.
        if (PJS_GameManager.Instance != null)
        {
            PJS_GameManager.Instance.TriggerDialogue(conversationNameToTrigger);
        }
        else
        {
            Debug.LogError("PJS_GameManager의 인스턴스를 찾을 수 없습니다!");
        }
    }

    public override void EndDialogue()
    {
        base.EndDialogue();
        if (isEnd)
        {
            PhotonNetwork.LoadLevel("TutorialScene2");
        }
    }

    private void UseEnd()
    {
        isEnd = true;
    }
}