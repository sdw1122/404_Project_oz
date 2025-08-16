using UnityEngine;
using System.Collections;
using Photon.Pun;
using Unity.VisualScripting;
public class BreakableObject : InteractableBase
{
    private bool isDestroyed = false;
    public ParticleSystem DustPuff;

    public override void Interact(PlayerController player)
    {
        pv.RPC("RequestDestroy", RpcTarget.All);
    }
    [PunRPC]
    private void RequestDestroy()
    {
        StartCoroutine(EffectAfterDisable());
    }
    IEnumerator EffectAfterDisable()
    {
        if (DustPuff != null) DustPuff.Play();
        yield return new WaitForSeconds(0.5f);
        gameObject.SetActive(false);
    }
}
