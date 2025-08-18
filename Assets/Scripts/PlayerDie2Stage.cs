using Photon.Pun;
using System.Linq;
using UnityEngine;

public class PlayerDie2Stage : MonoBehaviour
{
    PhotonView pv;
    public void Start()
    {
        pv = GetComponent<PhotonView>();
    }

    public void OnTriggerEnter(Collider other)
    {
        PhotonView playerPv = other.GetComponent<PhotonView>();
        if (playerPv != null)
        {
            // Player의 UserId와 ViewID를 RPC로 전달
            pv.RPC("SendDataClient", RpcTarget.MasterClient, playerPv.Owner.UserId, playerPv.ViewID);
        }
    }

    [PunRPC]
    public void SendDataClient(string userID, int viewID)
    {
        PlayerSaveData data = SaveSystem.LoadPlayerData(userID);
        string job = data.userJob;
        string latestFlag = data.latestFlag;

        PhotonView targetPv = PhotonView.Find(viewID);
        Teleport(targetPv.gameObject, job, latestFlag);
    }

    [PunRPC]
    public void FallDownTeleport(string job, string latestFlag, int viewID)
    {
        if (!pv.IsMine) return;
        PhotonView playerPv = PhotonView.Find(viewID);
        if (playerPv != null)
        {
            Teleport(playerPv.gameObject, job, latestFlag);
        }
    }

    public void Teleport(GameObject playerObj, string job, string latestFlag)
    {
        SaveFlag targetFlag = FindObjectsByType<SaveFlag>(FindObjectsSortMode.None).FirstOrDefault(f => f.label == latestFlag);
        if (targetFlag != null)
        {
            Transform spawn = targetFlag.SaveFlagGetSpawnPos(job);
            CharacterController cc = playerObj.GetComponent<CharacterController>();
            if (pv.IsMine)
            {
                cc.enabled = false;
                playerObj.transform.position = spawn.position;
                playerObj.transform.rotation = spawn.rotation;
                cc.enabled = true;
                Debug.Log($"[Resurrection] 깃발 위치로 이동: {spawn.position}");
            }
        }
    }


    PhotonView FindPlayerPhotonViewByUserId(string userID)
    {
        return FindObjectsByType<PhotonView>(FindObjectsSortMode.None)
               .FirstOrDefault(pv => pv.Owner != null && pv.Owner.UserId == userID);
    }
}