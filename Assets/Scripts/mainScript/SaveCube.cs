using UnityEngine;
using Photon.Pun;

public class SaveCube : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        PhotonView pv = other.GetComponent<PhotonView>();
        if (pv != null && pv.IsMine && PhotonNetwork.IsMasterClient)
        {
            Debug.Log("SaveCube 충돌 - 저장 요청 시작");

            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            foreach (GameObject player in players)
            {
                PhotonView targetPV = player.GetComponent<PhotonView>();
                if (targetPV != null)
                {
                    // 각 플레이어에게 위치 요청 보내기
                    targetPV.RPC("SendMyDataToHost", targetPV.Owner);
                }
            }
        }
    }
}
