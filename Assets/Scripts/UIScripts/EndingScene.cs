using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class EndingScene : MonoBehaviourPunCallbacks
{
    public RectTransform targetRect;
    public float moveSpeed = 100f; // 초당 이동 픽셀 수    
    private bool goMain = false;

    void Update()
    {
        if (targetRect == null) return;

        // 현재 anchoredPosition
        Vector2 pos = targetRect.anchoredPosition;

        // 위로 moveSpeed * Time.deltaTime 만큼 증가
        pos.y += moveSpeed * Time.deltaTime;

        // 새로운 위치 적용
        targetRect.anchoredPosition = pos;
        
        if (targetRect.anchoredPosition.y >= 700f && !goMain)
        {
            Debug.Log("targetRect : " + targetRect.anchoredPosition.y);
            goMain = true;
            PhotonNetwork.Disconnect();            
        }
    }
    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log("Photon 완전 Disconnect됨, 이제 씬 이동!");
        PhotonNetwork.LoadLevel("mainmenu");
    }
}
