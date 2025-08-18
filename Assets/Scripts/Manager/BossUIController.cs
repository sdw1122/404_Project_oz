// BossUIController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun; // PhotonView 사용을 위해 추가

public class BossUIController : MonoBehaviour
{
    [Header("UI 요소 연결")]
    [SerializeField] private TextMeshProUGUI bossNameText;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image overheatIcon;
    [SerializeField] private Image vulnerableIcon;

    // ▼▼▼ [추가된 UI 요소] ▼▼▼
    [Header("추가 UI 요소")]
    [Tooltip("취약 상태의 남은 시간을 표시할 텍스트 (TMP)")]
    [SerializeField] private TextMeshProUGUI vulnerableTimerText;
    [Tooltip("현재 타겟의 이름을 표시할 텍스트 (TMP)")]
    [SerializeField] private TextMeshProUGUI targetNameText;
    // ▲▲▲ [추가된 UI 요소] ▲▲▲

    private WoodMan targetBoss;

    public void Setup(WoodMan boss)
    {
        this.targetBoss = boss;
        if (targetBoss == null) return;

        if (bossNameText != null)
        {
            bossNameText.text = targetBoss.m_name;
        }

        // 초기 상태 아이콘 및 텍스트 숨기기
        if (overheatIcon != null) overheatIcon.gameObject.SetActive(false);
        if (vulnerableIcon != null) vulnerableIcon.gameObject.SetActive(false);
        if (vulnerableTimerText != null) vulnerableTimerText.gameObject.SetActive(false);
        if (targetNameText != null) targetNameText.gameObject.SetActive(true); // 타겟 이름은 항상 보이게
    }

    void Update()
    {
        if (targetBoss == null) return;

        if (targetBoss.dead)
        {
            Destroy(gameObject);
            return;
        }

        // 매 프레임 체력, 상태, 타겟을 확인하여 UI에 반영
        UpdateHealth(targetBoss.health, targetBoss.startingHealth);
        UpdateStatus(targetBoss._currentMode);
        UpdateTargetUI(); // 타겟 UI 업데이트 함수 호출
    }

    private void UpdateHealth(float currentHealth, float maxHealth)
    {
        if (healthSlider != null)
        {
            // 최대 체력이 0일 경우의 오류를 방지
            healthSlider.value = (maxHealth > 0) ? (currentHealth / maxHealth) : 0;
        }
    }

    private void UpdateStatus(WoodMan.WoodMan_Mode mode)
    {
        // Overheat 아이콘 제어
        if (overheatIcon != null)
        {
            overheatIcon.gameObject.SetActive(mode == WoodMan.WoodMan_Mode.Overheat);
        }

        // Vulnerable 아이콘 및 타이머 제어
        if (vulnerableIcon != null)
        {
            bool isVulnerable = (mode == WoodMan.WoodMan_Mode.Vulnerable);
            vulnerableIcon.gameObject.SetActive(isVulnerable);
            vulnerableTimerText.gameObject.SetActive(isVulnerable);

            if (isVulnerable)
            {
                // WoodMan 스크립트에서 남은 시간을 가져와 표시
                vulnerableTimerText.text = targetBoss.groggyRemainingTime.ToString("F1"); // 소수점 첫째 자리까지
            }
        }
    }

    // ▼▼▼ [추가된 함수] ▼▼▼
    /// <summary>
    /// 현재 타겟의 이름을 UI에 업데이트합니다.
    /// </summary>
    private void UpdateTargetUI()
    {
        if (targetNameText == null) return;

        if (targetBoss.targetEntity != null && !targetBoss.targetEntity.dead)
        {
            PlayerController targetPlayer = targetBoss.targetEntity.GetComponent<PlayerController>();
            if (targetPlayer != null)
            {
                targetNameText.text = "갈망: " + targetPlayer.job;
            }
            else
            {
                // 포톤 정보가 없는 타겟일 경우 (예: NPC)
                targetNameText.text = "갈망: " + targetBoss.targetEntity.name;
            }
        }
        else
        {
            targetNameText.text = "";
        }
    }
    // ▲▲▲ [추가된 함수] ▲▲▲
}