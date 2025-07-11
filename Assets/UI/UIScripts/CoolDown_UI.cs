using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CoolDown_UI : MonoBehaviour
{
    [Header("--- 스킬 1 ---")]
    public Image skillIcon1;
    public Image cooldownOverlay1;
    public TextMeshProUGUI cooldownText1;
    public float cooldownTime1 = 5.0f;

    private float remainingCooldown1;
    private bool isCooldown1 = false;

    [Header("--- 스킬 2 ---")]
    public Image skillIcon2;
    public Image cooldownOverlay2;
    public TextMeshProUGUI cooldownText2;
    public float cooldownTime2 = 8.0f;

    private float remainingCooldown2;
    private bool isCooldown2 = false;


    void Start()
    {
        // 스킬 1 UI 초기화
        if (cooldownText1 != null)
        {
            cooldownText1.gameObject.SetActive(false);
        }
        cooldownOverlay1.fillAmount = 1;
        Color startIconColor1 = skillIcon1.color;
        startIconColor1.a = 1f;
        skillIcon1.color = startIconColor1;

        // 스킬 2 UI 초기화
        if (cooldownText2 != null)
        {
            cooldownText2.gameObject.SetActive(false);
        }
        cooldownOverlay2.fillAmount = 1;
        Color startIconColor2 = skillIcon2.color;
        startIconColor2.a = 1f;
        skillIcon2.color = startIconColor2;
    }

    void Update()
    {
        // 'M' 키로 스킬 1 사용 (테스트용)
        if (Input.GetKeyDown(KeyCode.M) && !isCooldown1)
        {
            Color iconColor1 = skillIcon1.color;
            iconColor1.a = 0.2f;
            skillIcon1.color = iconColor1;
            StartCooldown1();
        }

        // 'N' 키로 스킬 2 사용 (테스트용)
        if (Input.GetKeyDown(KeyCode.N) && !isCooldown2)
        {
            Color iconColor2 = skillIcon1.color;
            iconColor2.a = 0.2f;
            skillIcon1.color = iconColor2;
            StartCooldown2();
        }

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
                Color iconColor1 = skillIcon1.color;
                iconColor1.a = 1f;
                skillIcon1.color = iconColor1;
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
                Color iconColor2 = skillIcon1.color;
                iconColor2.a = 1f;
                skillIcon1.color = iconColor2;
                EndCooldown2();
            }
        }
    }

    // --- 스킬 1 쿨타임 함수 ---
    public void StartCooldown1()
    {
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
    }

    private void EndCooldown1()
    {
        isCooldown1 = false;
        cooldownOverlay1.fillAmount = 1;
        if (cooldownText1 != null)
        {
            cooldownText1.gameObject.SetActive(false);
        }
        Color iconColor = skillIcon1.color;
        iconColor.a = 1f;
        skillIcon1.color = iconColor;
        Debug.Log("스킬 1 사용 가능!");
    }


    // --- 스킬 2 쿨타임 함수 ---
    public void StartCooldown2()
    {
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
    }

    private void EndCooldown2()
    {
        isCooldown2 = false;
        cooldownOverlay2.fillAmount = 1;
        if (cooldownText2 != null)
        {
            cooldownText2.gameObject.SetActive(false);
        }
        Color iconColor = skillIcon2.color;
        iconColor.a = 1f;
        skillIcon2.color = iconColor;
        Debug.Log("스킬 2 사용 가능!");
    }
}