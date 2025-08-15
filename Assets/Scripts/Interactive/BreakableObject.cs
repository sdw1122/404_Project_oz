using UnityEngine;
using System.Collections;
using Photon.Pun;
public class BreakableObject : InteractableBase
{
    private bool isDestroyed = false;

    public override void Interact(PlayerController player)
    {
        pv.RPC("RequestDestroy", RpcTarget.All);
    }
    [PunRPC]
    private void RequestDestroy()
    {
        gameObject.SetActive(false);
    }
}
