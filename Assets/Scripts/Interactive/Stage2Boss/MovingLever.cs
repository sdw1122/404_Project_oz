using Photon.Pun;
using System.Collections;
using UnityEngine;

public class MovingLever : InteractableBase
{
    public GameObject movingObj;
    public Transform objPos;
    public float disableDuration = 10f;
    public override void Interact(PlayerController player)
    {

        pv.RPC(nameof(SetMovingObj), RpcTarget.All);
        
    }
    [PunRPC]
    public void SetMovingObj()
    {
        movingObj.SetActive(true);
        movingObj.transform.position = objPos.position;
        StartCoroutine(DisableInteract());
    }
    public IEnumerator DisableInteract()
    {
        gameObject.layer = 0;
        yield return new WaitForSeconds(disableDuration);
        gameObject.layer = LayerMask.NameToLayer("Interact");
    }

}
