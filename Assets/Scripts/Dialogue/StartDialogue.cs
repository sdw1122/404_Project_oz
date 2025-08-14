using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class StartDialogue : DialogueManager
{
    // 인스펙터에서 지정할 다음 씬 이름
    [Header("인트로 다이얼로그 종료 후 이동할 씬")]
    public string nextSceneName;

    // 마스터 클라이언트만 씬 이동 명령
    public override void EndDialogue()
    {
        base.EndDialogue(); // 기존 UI 닫기 등 처리
        // 마스터만 씬 이동 RPC 호출
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC(nameof(LoadNextScene_RPC), RpcTarget.All);
        }
    }

    [PunRPC]
    private void LoadNextScene_RPC()
    {
        // PhotonNetwork.AutomaticallySyncScene = true; 설정 필요!
        PhotonNetwork.LoadLevel(nextSceneName);
    }
}
