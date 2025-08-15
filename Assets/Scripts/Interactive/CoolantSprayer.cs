using UnityEngine;
using Photon.Pun;

// 냉각수 분사기 로직을 처리하는 스크립트
public class CoolantSprayer : MonoBehaviour
{
    [Header("효과 설정")]
    [Tooltip("냉각수 분사 효과를 위한 파티클 시스템을 연결하세요.")]
    [SerializeField] private ParticleSystem coolantEffect;

    // ----- [수정됨] 분사 지속 시간 설정 -----
    [Tooltip("분사 효과가 지속될 시간(초)입니다. 이 값을 직접 설정할 수 있습니다.")]
    [SerializeField] private float sprayDuration = 5f; // 기본값 5초로 설정

    [Header("충돌 설정")]
    [Tooltip("활성화/비활성화할 콜라이더를 연결하세요. (보통 Box Collider)")]
    [SerializeField] private Collider sprayCollider;

    private bool isSpraying = false;

    void Awake()
    {
        if (sprayCollider != null)
        {
            sprayCollider.enabled = false;
        }
        else
        {
            Debug.LogError($"'{gameObject.name}' 오브젝트에 sprayCollider가 연결되지 않았습니다!", this);
        }
    }

    // 레버에 의해 호출되는 메서드
    public void StartSpray()
    {
        if (isSpraying) return;

        if (coolantEffect != null)
        {
            coolantEffect.Play();
        }

        if (sprayCollider != null)
        {
            sprayCollider.enabled = true;
        }

        isSpraying = true;

        // ----- [수정됨] 이제 파티클 시스템이 아닌, 직접 설정한 sprayDuration 값을 사용합니다. -----
        if (sprayDuration > 0)
        {
            Invoke(nameof(StopSpray), sprayDuration);
        }
        else
        {
            StopSpray();
        }
    }

    // 분사 효과를 멈추는 메서드
    private void StopSpray()
    {
        if (coolantEffect != null)
        {
            coolantEffect.Stop();
        }

        if (sprayCollider != null)
        {
            sprayCollider.enabled = false;
        }

        isSpraying = false;
    }
}