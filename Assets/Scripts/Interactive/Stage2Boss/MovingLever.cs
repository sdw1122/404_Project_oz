using Photon.Pun;
using UnityEngine;

public class MovingLever : InteractableBase
{
    public GameObject movingObj;
    public Transform objPos;
    public override void Interact(PlayerController player)
    {

        pv.RPC(nameof(SetMovingObj), RpcTarget.All);
    }
    [PunRPC]
    public void SetMovingObj()
    {
        movingObj.SetActive(true);
        movingObj.transform.position = objPos.position;
    }

}
