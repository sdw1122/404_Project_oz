using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun; // Photon.Pun 네임스페이스 추가

public class CoolDown_UI : MonoBehaviour
{
    // ... (기존 변수들은 그대로 유지) ...
    [Header("--- 스킬 1 ---")]
    public Image skillIcon1;
    public Image cooldownOverlay1;
    public TextMeshProUGUI cooldownText1;
    public float cooldownTime1 = 5.0f;

    private float remainingCooldown1;
    public bool isCooldown1 = false;

    [Header("--- 스킬 2 ---")]
    public Image skillIcon2;
    public Image cooldownOverlay2;
    public TextMeshProUGUI cooldownText2;
    public float cooldownTime2 = 8.0f;

    private float remainingCooldown2;
    private bool isCooldown2 = false;

    private PhotonView photonView;


    void Awake()
    {
        // 프리팹의 최상위 부모에 있는 PhotonView를 찾아옴
        photonView = GetComponentInParent<PhotonView>();
    }

    void Start()
    {
        // 이 UI가 내 소유가 아니라면 즉시 비활성화하고 종료
        if (photonView == null || !photonView.IsMine)
        {
            gameObject.SetActive(false);
            return;
        }

        // 아래 코드는 내 플레이어일 경우에만 실행됨
        EndCooldown1();
        EndCooldown2();
    }

    void Update()
    {
        // photonView가 없거나 내 것이 아니면 아무것도 안 함
        if (photonView == null || !photonView.IsMine)
        {
            return;
        }
        if(Input.GetKeyDown(KeyCode.R))
        {
            // 테스트용: 스킬 1 쿨타임 시작
            StartCooldown1();
        }
        if(Input.GetKeyDown(KeyCode.Q))
        {
            // 테스트용: 스킬 2 쿨타임 시작
            StartCooldown2();
        }

        // ... (기존 Update 로직은 그대로 유지) ...
        // 스킬 1 쿨타임 처리
        if (isCooldown1)
        {
            remainingCooldown1 -= Time.deltaTime;
            if (remainingCooldown1 > 0)
            {
                cooldownOverlay1.fillAmount = 1.0f - (remainingCooldown1 / cooldownTime1);
                if (cooldownText1 != null)
                {
                    cooldownText1.text = remainingCooldown1.ToString("F1");
                }
            }
            else
            {
                EndCooldown1();
            }
        }

        // 스킬 2 쿨타임 처리
        if (isCooldown2)
        {
            remainingCooldown2 -= Time.deltaTime;
            if (remainingCooldown2 > 0)
            {
                cooldownOverlay2.fillAmount = 1.0f - (remainingCooldown2 / cooldownTime2);
                if (cooldownText2 != null)
                {
                    cooldownText2.text = remainingCooldown2.ToString("F1");
                }
            }
            else
            {
                EndCooldown2();
            }
        }
    }

    // ... (StartCooldown, EndCooldown 함수들은 그대로 유지) ...
    public void StartCooldown1()
    {
        if (isCooldown1) return;
        isCooldown1 = true;
        remainingCooldown1 = cooldownTime1;

        if (cooldownText1 != null)
        {
            cooldownText1.gameObject.SetActive(true);
        }

        Color overlayColor = cooldownOverlay1.color;
        overlayColor.a = 128f / 255f;
        cooldownOverlay1.color = overlayColor;
        cooldownOverlay1.fillAmount = 0;

        // 아이콘을 어둡게 처리
        Color iconColor = skillIcon1.color;
        iconColor.a = 0.4f; // 어둡게
        skillIcon1.color = iconColor;
    }

    private void EndCooldown1()
    {
        isCooldown1 = false;
        remainingCooldown1 = 0;
        cooldownOverlay1.fillAmount = 1;

        if (cooldownText1 != null)
        {
            cooldownText1.gameObject.SetActive(false);
        }

        // 아이콘을 다시 밝게
        Color iconColor = skillIcon1.color;
        iconColor.a = 1f; // 원상 복구
        skillIcon1.color = iconColor;

        if (Time.time > 0) // Start에서 호출될 때 로그가 찍히지 않도록 방지
            Debug.Log("스킬 1 사용 가능!");
    }


    // --- 스킬 2 쿨타임 함수 ---
    public void StartCooldown2()
    {
        if (isCooldown2) return;
        isCooldown2 = true;
        remainingCooldown2 = cooldownTime2;

        if (cooldownText2 != null)
        {
            cooldownText2.gameObject.SetActive(true);
        }

        Color overlayColor = cooldownOverlay2.color;
        overlayColor.a = 128f / 255f;
        cooldownOverlay2.color = overlayColor;
        cooldownOverlay2.fillAmount = 0;

        // 아이콘을 어둡게 처리 (skillIcon2로 수정됨)
        Color iconColor = skillIcon2.color;
        iconColor.a = 0.4f;
        skillIcon2.color = iconColor;
    }

    private void EndCooldown2()
    {
        isCooldown2 = false;
        remainingCooldown2 = 0;
        cooldownOverlay2.fillAmount = 1;

        if (cooldownText2 != null)
        {
            cooldownText2.gameObject.SetActive(false);
        }

        // 아이콘을 다시 밝게 (skillIcon2로 수정됨)
        Color iconColor = skillIcon2.color;
        iconColor.a = 1f;
        skillIcon2.color = iconColor;

        if (Time.time > 0)
            Debug.Log("스킬 2 사용 가능!");
    }
}