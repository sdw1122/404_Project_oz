using UnityEngine;
using UnityEngine.UI; // Image를 사용하기 위해 필요
using TMPro;
using Photon.Pun;
using System.Collections;

public class BossUI_ImageFill : MonoBehaviour
{
    [Header("UI 요소 연결")]
    public GameObject bossUIPanel;
    public TMP_Text bossNameText;

    [Tooltip("Image Type이 Filled로 설정되어 있어야 합니다.")]
    public Image healthBarImage; // Slider 대신 Image로 변경

    public TMP_Text modeText;
    public TMP_Text aggroTargetText;

    [Header("그로기 상태 UI")]
    public GameObject vulnerablePanel;
    public TMP_Text vulnerableTimerText;

    private WoodMan woodMan;
    private Coroutine vulnerableCoroutine;

    private void Awake()
    {
        // 처음에는 UI를 비활성화
        if (bossUIPanel != null)
        {
            bossUIPanel.SetActive(false);
        }
        if (vulnerablePanel != null)
        {
            vulnerablePanel.SetActive(false);
        }
    }

    private void Update()
    {
        // woodMan 참조가 없거나 죽었다면 업데이트 중지
        if (woodMan == null || woodMan.dead)
        {
            if (bossUIPanel.activeSelf)
            {
                bossUIPanel.SetActive(false);
            }
            return;
        }

        // UI 업데이트
        UpdateHealthUI();
        UpdateModeUI();
        UpdateAggroTargetUI();
    }

    // 외부에서 호출하여 UI 초기화 (보스 생성 시)
    public void Initialize(WoodMan boss)
    {
        woodMan = boss;

        if (woodMan == null)
        {
            Debug.LogError("BossUI: WoodMan 참조가 null입니다.");
            bossUIPanel.SetActive(false);
            return;
        }

        // Image Type 검사
        if (healthBarImage.type != Image.Type.Filled)
        {
            Debug.LogWarning("HealthBar Image의 Image Type이 'Filled'로 설정되어 있지 않습니다. 기능이 올바르게 동작하지 않을 수 있습니다.");
        }

        // UI 기본 정보 설정
        bossNameText.text = woodMan.m_name;
        bossUIPanel.SetActive(true);
        UpdateHealthUI(); // 초기 체력 설정
    }

    // 체력 UI 업데이트
    private void UpdateHealthUI()
    {
        if (woodMan.startingHealth > 0)
        {
            // 현재 체력 비율을 계산하여 fillAmount에 적용
            healthBarImage.fillAmount = woodMan.health / woodMan.startingHealth;
        }
    }

    // 모드 UI 업데이트
    private void UpdateModeUI()
    {
        switch (woodMan._currentMode)
        {
            case WoodMan.WoodMan_Mode.Normal:
                modeText.text = "상태: <color=white>일반</color>";
                StopVulnerableTimer();
                break;
            case WoodMan.WoodMan_Mode.Overheat:
                modeText.text = "상태: <color=red>과열 (방어력 증가)</color>";
                StopVulnerableTimer();
                break;
            case WoodMan.WoodMan_Mode.Vulnerable:
                modeText.text = "상태: <color=cyan>냉각 (그로기!)</color>";
                StartVulnerableTimer();
                break;
        }
    }

    // 어그로 대상 UI 업데이트
    private void UpdateAggroTargetUI()
    {
        if (woodMan.targetEntity != null)
        {
            PhotonView targetPV = woodMan.targetEntity.GetComponent<PhotonView>();
            if (targetPV != null && targetPV.Owner != null)
            {
                aggroTargetText.text = $"타겟: {targetPV.Owner.NickName}";
            }
            else
            {
                aggroTargetText.text = "타겟: -";
            }
        }
        else
        {
            aggroTargetText.text = "타겟: -";
        }
    }

    // 그로기 타이머 시작
    private void StartVulnerableTimer()
    {
        if (vulnerableCoroutine == null)
        {
            vulnerableCoroutine = StartCoroutine(VulnerableTimerRoutine());
        }
    }

    // 그로기 타이머 정지 및 숨기기
    private void StopVulnerableTimer()
    {
        if (vulnerableCoroutine != null)
        {
            StopCoroutine(vulnerableCoroutine);
            vulnerableCoroutine = null;
            vulnerablePanel.SetActive(false);
        }
    }

    // 그로기 타이머 코루틴
    private IEnumerator VulnerableTimerRoutine()
    {
        vulnerablePanel.SetActive(true);
        float timer = woodMan.groggyTime;

        while (timer > 0)
        {
            vulnerableTimerText.text = $"그로기 남은 시간: {timer:F1}초";
            timer -= Time.deltaTime;
            yield return null;
        }

        vulnerablePanel.SetActive(false);
        vulnerableCoroutine = null; // 코루틴 완료 후 참조 초기화
    }

    // 보스가 죽었을 때 호출
    public void HideUI()
    {
        bossUIPanel.SetActive(false);
        StopVulnerableTimer();
    }
}