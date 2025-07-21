using UnityEngine;
using Photon.Pun;

// 냉각수 분사기 로직을 처리하는 스크립트
public class CoolantSprayer : MonoBehaviour
{
    [Header("효과 설정")]
    [Tooltip("냉각수 분사 효과를 위한 파티클 시스템을 연결하세요.")]
    [SerializeField] private ParticleSystem coolantEffect;

    [Tooltip("분사 효과가 지속될 시간(초)입니다.")]
    [SerializeField] private float sprayDuration = 5f;

    private bool isSpraying = false;

    // 레버에 의해 호출되는 메서드
    public void StartSpray()
    {
        if (isSpraying) return;

        // 시각 효과 재생
        if (coolantEffect != null)
        {
            coolantEffect.Play();
        }

        isSpraying = true;
        Invoke(nameof(StopSpray), sprayDuration);

        // 분사 범위 내의 보스를 찾아 약화 상태로 만듦
        //CheckForBoss();
    }

    // 분사 효과를 멈추는 메서드
    private void StopSpray()
    {
        if (coolantEffect != null)
        {
            coolantEffect.Stop();
        }
        isSpraying = false;
    }

    // 분사 범위 내에 보스가 있는지 확인하는 메서드
    /*private void CheckForBoss()
    {
        // 이 스크립트가 붙은 객체에 설정된 Collider를 트리거로 사용
        Collider[] hitColliders = Physics.OverlapBox(transform.position, transform.localScale / 2, transform.rotation);

        foreach (var hitCollider in hitColliders)
        {
            // "Boss" 태그를 가진 객체를 찾음 (보스 객체에 태그 설정 필요)
            if (hitCollider.CompareTag("Boss"))
            {
                // 보스의 PhotonView를 찾아 RPC 호출
                PhotonView bossPv = hitCollider.GetComponent<PhotonView>();
                if (bossPv != null)
                {
                    // 보스 스크립트에 있는 'ApplyWeakness'와 같은 약화 메서드를 RPC로 호출
                    bossPv.RPC("ApplyWeakness", RpcTarget.All, sprayDuration);
                }
                break; // 보스를 한 번만 찾으면 루프 종료
            }
        }
    }*/
}