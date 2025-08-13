using Photon.Pun;
using System.Collections;
using UnityEngine;

public class ShockWaveLever : MonoBehaviour
{
    public Material targetMat;
    public Platform platform;
    public float disableDuration = 20f;
    public float damage = 10f;
    PhotonView pv;
    private void Awake()
    {
        pv = GetComponent<PhotonView>();
    }
    public void StartShockWave()
    {

        pv.RPC(nameof(ShockWave), RpcTarget.MasterClient);

    }
    [PunRPC]
    public void ShockWave()
    {
        pv.RPC(nameof(Shake), RpcTarget.All);
        Collider[] hitColliders=Physics.OverlapSphere(platform.transform.position,40f,1<<LayerMask.NameToLayer("Enemy"));
        foreach(var hit  in hitColliders)
        {
            Vector3 hitPoint = hit.transform.position;
            Vector3 hitNormal = (hit.transform.position - transform.position).normalized;
            PhotonView enemyPv = hit.GetComponent<PhotonView>();
            enemyPv.RPC("RPC_ApplyDamage", RpcTarget.MasterClient, damage, hitPoint, hitNormal, pv.ViewID);

            enemyPv.RPC("RPC_EnemyHit", RpcTarget.All);

            enemyPv.RPC("RPC_PlayHitEffect", RpcTarget.All, hitPoint, hitNormal);
        }
       
    }
    [PunRPC]
    public void Shake()
    {
        StartCoroutine(platform.Shake(1f, 0.2f));
        StartCoroutine(FadeToBlack());
    }
    IEnumerator FadeToBlack()
    {
        gameObject.layer = 0;
        Color startColor = Color.white; // 흰색
        Color endColor = Color.black;   // 검은색

        // Material의 BaseMap 색상이 "_BaseColor"에 저장된 경우(URP 등)
        // Color startColor = targetMat.GetColor("_BaseColor");
        // 만약 표준 쉐이더(Standard)라면 "_Color"를 씁니다

        float time = 0f;
        while (time < disableDuration)
        {
            float t = time / disableDuration;
            Color lerped = Color.Lerp(startColor, endColor, t);
            targetMat.color = lerped; // Standard: "_Color", URP: "_BaseColor"

            // 만약 "_BaseColor"이면 아래와 같이:
            // targetMat.SetColor("_BaseColor", lerped);

            time += Time.deltaTime;
            yield return null;
        }

        // 마지막에 완전히 검은색으로!
        targetMat.color = endColor;
        gameObject.layer = LayerMask.NameToLayer("ShockWave");
        // 또는 targetMat.SetColor("_BaseColor", endColor);
    }

    private void OnDrawGizmosSelected()
    {
        // platform 변수가 Inspector에 할당되지 않았을 경우를 대비한 안전장치
        if (platform == null)
        {
            return;
        }

        // 기즈모의 색상을 반투명한 빨간색으로 설정
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);

        // ShockWave 함수의 OverlapSphere와 동일한 위치와 크기로 와이어 구체를 그립니다.
        Gizmos.DrawWireSphere(platform.transform.position, 40f);
    }

}
