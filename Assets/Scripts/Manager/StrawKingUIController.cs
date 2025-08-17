using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro를 사용하기 위해 필요
using System.Collections;

/// <summary>
/// 허수아비 왕 보스전의 UI를 제어하는 스크립트입니다.
/// </summary>
public class StrawKingUIController : MonoBehaviour
{
    [Header("기본 UI 요소")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI targetNameText;

    [Header("지혜(Wisdom) UI")]
    [SerializeField] private Slider wisdomSlider;
    [SerializeField] private TextMeshProUGUI wisdomText;

    [Header("그로기(Groggy) UI")]
    [SerializeField] private GameObject groggyPanel; // 그로기 상태일 때 켤 패널
    [SerializeField] private TextMeshProUGUI groggyTimerText;
    private Coroutine groggyTimerCoroutine;

    [Header("지혜 대포(Wisdom Cannon) UI")]
    [SerializeField] private Image[] cannonStatusIcons; // 3개의 대포 아이콘
    [SerializeField] private Sprite cannonReadySprite; // 발사 가능 상태 스프라이트
    [SerializeField] private Sprite cannonReloadingSprite; // 재장전 중 스프라이트

    // UI가 참조할 보스 스크립트들
    private StrawKing strawKing;
    private BossGroggy bossGroggy;

    /// <summary>
    /// 외부(Manager)에서 호출하여 UI를 초기화하는 함수
    /// </summary>
    public void Setup(StrawKing boss)
    {
        this.strawKing = boss;
        this.bossGroggy = boss.GetComponent<BossGroggy>(); // BossGroggy 컴포넌트 가져오기

        if (strawKing == null || bossGroggy == null)
        {
            Debug.LogError("보스 또는 BossGroggy 컴포넌트를 찾을 수 없습니다. UI를 파괴합니다.");
            Destroy(gameObject);
            return;
        }

        // 초기 UI 설정

        if (groggyPanel != null)
        {
            groggyPanel.SetActive(false);
        }
    }

    void Update()
    {
        // 보스 정보가 없거나 보스가 죽으면 UI 업데이트 중지 및 파괴
        if (strawKing == null || strawKing.dead)
        {
            Destroy(gameObject);
            return;
        }

        // 매 프레임 UI 정보 업데이트
        UpdateHealthUI();
        UpdateTargetUI();
        UpdateWisdomUI();
        UpdateGroggyUI();
        UpdateCannonUI();
    }

    private void UpdateHealthUI()
    {
        if (healthSlider != null)
        {
            healthSlider.value = strawKing.health / strawKing.startingHealth;
        }
    }

    private void UpdateTargetUI()
    {
        if (targetNameText == null) return;

        if (strawKing.targetEntity != null && !strawKing.targetEntity.dead)
        {
            // 포톤뷰를 통해 플레이어의 닉네임을 가져올 수 있습니다.
            var targetPhotonView = strawKing.targetEntity.GetComponent<Photon.Pun.PhotonView>();
            if (targetPhotonView != null && targetPhotonView.Owner != null)
            {
                targetNameText.text = "타겟: " + targetPhotonView.Owner.NickName;
            }
            else
            {
                targetNameText.text = "타겟: " + strawKing.targetEntity.name;
            }
        }
        else
        {
            targetNameText.text = "타겟 없음";
        }
    }

    private void UpdateWisdomUI()
    {
        if (WisdomManager.Instance == null) return;

        int currentWisdom = WisdomManager.Instance.GetCurrentWisdom();
        if (wisdomSlider != null)
        {
            wisdomSlider.value = (float)currentWisdom / WisdomManager.Instance.requiredWisdom;
        }
        if (wisdomText != null)
        {
            wisdomText.text = $"{currentWisdom} / {WisdomManager.Instance.requiredWisdom}";
        }
    }

    private void UpdateGroggyUI()
    {
        if (bossGroggy == null || groggyPanel == null) return;

        // 그로기 상태가 시작되면 타이머 코루틴을 실행
        if (bossGroggy.isGroggy && !groggyPanel.activeSelf)
        {
            groggyPanel.SetActive(true);
            if (groggyTimerCoroutine != null)
            {
                StopCoroutine(groggyTimerCoroutine);
            }
            groggyTimerCoroutine = StartCoroutine(GroggyTimerRoutine());
        }
        // 그로기 상태가 아니면 패널을 비활성화
        else if (!bossGroggy.isGroggy && groggyPanel.activeSelf)
        {
            groggyPanel.SetActive(false);
            if (groggyTimerCoroutine != null)
            {
                StopCoroutine(groggyTimerCoroutine);
                groggyTimerCoroutine = null;
            }
        }
    }

    private IEnumerator GroggyTimerRoutine()
    {
        float timer = 10f; // BossGroggy 스크립트의 groggyTime과 일치시켜야 함
        while (timer > 0)
        {
            if (groggyTimerText != null)
            {
                groggyTimerText.text = $"그로기 남은 시간: {timer:F1}초";
            }
            timer -= Time.deltaTime;
            yield return null;
        }
    }

    private void UpdateCannonUI()
    {
        if (bossGroggy == null || cannonStatusIcons == null || cannonStatusIcons.Length < 3) return;

        for (int i = 0; i < 3; i++)
        {
            if (bossGroggy.cannon[i] != null && cannonStatusIcons[i] != null)
            {
                // isShot 변수를 기준으로 아이콘 스프라이트 변경
                bool isShot = bossGroggy.cannon[i].isShot;
                cannonStatusIcons[i].sprite = isShot ? cannonReloadingSprite : cannonReadySprite;
            }
        }
    }
}