using Photon.Pun;
using UnityEngine;

public class Respawn : InteractableBase
{
    BoxCollider col;
   public PlayerHealth playerHealth;
    protected override void Awake()
    {
        base.Awake();
        col = GetComponent<BoxCollider>();
    }
    public override void Interact(PlayerController player)
    {
        playerHealth.pv.RPC("Resurrection", playerHealth.pv.Owner);

    }
    public void ActiveCol()
    {
 
        pv.RPC(nameof(ActiveColRPC), RpcTarget.All, true);
    }

    public void DeactiveCol()
    {

        pv.RPC(nameof(ActiveColRPC), RpcTarget.All, false);
    }

    [PunRPC]
    public void ActiveColRPC(bool isActive)
    {

        if (col != null)
        {
            col.enabled = isActive;
        }
    }
}
