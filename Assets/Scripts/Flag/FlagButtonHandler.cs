using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;
using System.Linq;

public class FlagButtonHandler : MonoBehaviour
{
    public TextMeshPro labelText;
    private string flagLabel;
   
    public void Initialize(string label)
    {
      
        flagLabel = label;
        labelText.text = label;
    }

    public void OnClick()
    {   
         
        if (!PhotonNetwork.IsMasterClient) { return; }
        string sceneName = FlagDataManager.GetSceneByFlag(flagLabel);
        if (!string.IsNullOrEmpty(sceneName))
        {
            Debug.Log($"[FlagButton] '{flagLabel}' 선택됨");

            LobbyManager lobbyManager = FindObjectsByType<LobbyManager>(FindObjectsSortMode.None).FirstOrDefault();
            lobbyManager.photonView.RPC("SyncClickedFlag", RpcTarget.All,flagLabel);
            PhotonNetwork.LoadLevel(sceneName);
                 
            

        }
        else
        {
            Debug.LogWarning($"[FlagButton] 라벨 '{flagLabel}' 에 대응하는 씬을 찾을 수 없습니다.");
        }
    }
    
    
}
