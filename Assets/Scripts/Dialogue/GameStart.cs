using UnityEngine;
using Photon.Pun;
using System.Collections;

// InteractableBase를 상속받아 대화 트리거 기능을 구현합니다.
public class GameStart : MonoBehaviour
{
    PhotonView pv;
    [Header("대화 설정")]
    [Tooltip("PJS_GameManager에 설정된 대화의 이름(conversationName)을 입력하세요.")]
    [SerializeField] private string conversationNameToTrigger;

    [Tooltip("체크하면 대화를 한 번만 실행합니다.")]
    [SerializeField] private bool isOneTimeUse = true;

    // 대화가 이미 실행되었는지 여부를 모든 클라이언트가 공유하기 위한 변수입니다.
    private bool hasBeenUsed = false;    

    private void Start()
    {
        pv = GetComponent<PhotonView>();
        // 자동 실행 조건(예: 자동실행 옵션 여부 등)을 필요에 따라 추가
        StartCoroutine(StartDialogueWithDelay(0.1f));
    }
    IEnumerator StartDialogueWithDelay(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        TriggerDialogueIfMaster();
    }

    private void TriggerDialogueIfMaster()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (isOneTimeUse && hasBeenUsed) return;        

        pv.RPC("RequestDialogueStart_RPC", RpcTarget.MasterClient);
    }

    /// [마스터 클라이언트에서만 실행됨]
    /// 클라이언트의 요청을 받아 PJS_GameManager를 통해 모든 플레이어에게 대화를 시작
    [PunRPC]
    private void RequestDialogueStart_RPC()
    {        
        // 아직 대화가 시작되지 않았을 경우에만 실행합니다.
        if (isOneTimeUse && hasBeenUsed) return;        

        // 한 번만 실행되는 대화일 경우, 모든 클라이언트에게 '사용됨' 상태를 전파합니다.
        if (isOneTimeUse)
        {            
            // AllBuffered를 사용하여, 나중에 접속한 플레이어도 이 상태를 받도록 합니다.
            pv.RPC("MarkAsUsed_RPC", RpcTarget.AllBuffered);
        }

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

    /// 이 오브젝트를 '사용됨' 상태로 만듭니다.
    [PunRPC]
    private void MarkAsUsed_RPC()
    {
        hasBeenUsed = true;
    }
}