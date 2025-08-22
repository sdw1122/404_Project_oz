using Photon.Pun;
using System.Collections;
using UnityEngine;

public class HammerEffect : MonoBehaviour
{
    public GameObject skill2Effect;
    private GameObject skill2EffectInstance;
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
        AudioManager.instance.PlaySfxAtLocation("Hammer Skill", pos);
        StartCoroutine(StopSkill2Effect());
    }


    private IEnumerator StopSkill2Effect()
    {
        float maxDuration = 2f;
        yield return new WaitForSeconds(maxDuration);
        Destroy(skill2EffectInstance);
    }
}
