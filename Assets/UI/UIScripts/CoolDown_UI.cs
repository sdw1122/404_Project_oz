using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro를 사용하려면 추가

public class CoolDown_UI : MonoBehaviour
{
    [Header("UI Elements")]
    public Image skillIcon;         // 스킬 아이콘 (투명도 조절용)
    public Image cooldownOverlay;   // 쿨타임이 찰 때 돌아가는 이미지
    public TextMeshProUGUI cooldownText;    // 남은 시간 텍스트

    [Header("Cooldown Settings")]
    public float cooldownTime = 5.0f; // 스킬 쿨타임 (초)

    private float remainingCooldown;  // 남은 쿨타임을 추적
    private bool isCooldown = false;  // 쿨타임 상태 플래그

    void Start()
    {
        // 처음에는 쿨타임이 아니므로 텍스트를 비활성화
        if (cooldownText != null)
        {
            cooldownText.gameObject.SetActive(false);
        }
        // 쿨타임 오버레이도 비활성화
        cooldownOverlay.fillAmount = 0;
    }

    void Update()
    {
        // Q키를 누르면 쿨타임 시작 (테스트용)
        if (Input.GetKeyDown(KeyCode.M) && !isCooldown)
        {
            StartCooldown();
        }

        // 쿨타임이 진행 중일 때만 로직 실행
        if (isCooldown)
        {
            remainingCooldown -= Time.deltaTime;

            if (remainingCooldown > 0)
            {
                // 쿨타임 UI 업데이트
                cooldownOverlay.fillAmount = 1.0f - (remainingCooldown / cooldownTime);
                if (cooldownText != null)
                {
                    cooldownText.text = remainingCooldown.ToString("F1"); // 소수점 한 자리까지 표시
                }
            }
            else
            {
                // 쿨타임 종료
                EndCooldown();
            }
        }
    }

    public void StartCooldown()
    {
        isCooldown = true;
        remainingCooldown = cooldownTime;

        // 쿨타임 텍스트 활성화
        if (cooldownText != null)
        {
            cooldownText.gameObject.SetActive(true);
        }

        // 쿨타임 오버레이 이미지의 투명도를 126으로 설정
        Color overlayColor = cooldownOverlay.color;
        overlayColor.a = 126f / 255f; // 알파값은 0~1 사이이므로 255로 나눔
        cooldownOverlay.color = overlayColor;

        cooldownOverlay.fillAmount = 0; // 채우기를 0부터 시작
    }

    private void EndCooldown()
    {
        isCooldown = false;
        remainingCooldown = 0;

        // 쿨타임 관련 UI 비활성화
        cooldownOverlay.fillAmount = 0; // 오버레이 숨기기
        if (cooldownText != null)
        {
            cooldownText.gameObject.SetActive(false);
        }

        // 스킬 아이콘을 완전히 선명하게 (투명도 255)
        Color iconColor = skillIcon.color;
        iconColor.a = 1f; // 알파값 1 = 255
        skillIcon.color = iconColor;

        Debug.Log("스킬 사용 가능!");
    }
}
