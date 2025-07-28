using Photon.Pun;
using System.Collections;
using UnityEngine;

public class HammerEffect : MonoBehaviour
{
    public GameObject skill2Effect;
    public GameObject skill2EffectInstance;
    public float effectDistance = 1.5f;
    private PhotonView pv;

    private void Awake()
    {
        pv = GetComponent<PhotonView>();
        if (skill2Effect == null) return;
    }
    public void PlaySkill2()
    {
        Vector3 pos = transform.position + transform.forward * effectDistance;
        Quaternion rot = Quaternion.identity;

        if (skill2Effect == null) return;
        skill2EffectInstance = Instantiate(skill2Effect, pos, rot);
        StartCoroutine(StopSkill2Effect());
        pv.RPC(nameof(RPC_PlaySkill2Effect),RpcTarget.Others,pos,rot);
    }

    [PunRPC]
    void RPC_PlaySkill2Effect(Vector3 pos, Quaternion rot)
    {
        skill2EffectInstance = Instantiate(skill2Effect, pos, rot);
        StartCoroutine(StopSkill2Effect());
    }

    private IEnumerator StopSkill2Effect()
    {
        float maxDuration = 2f;
        yield return new WaitForSeconds(maxDuration);
        Destroy(skill2EffectInstance);
    }
}
