using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro�� ����Ϸ��� �߰�

public class CoolDown_UI : MonoBehaviour
{
    [Header("UI Elements")]
    public Image skillIcon;         // ��ų ������ (���� ������)
    public Image cooldownOverlay;   // ��Ÿ���� �� �� ���ư��� �̹���
    public TextMeshProUGUI cooldownText;    // ���� �ð� �ؽ�Ʈ

    [Header("Cooldown Settings")]
    public float cooldownTime = 5.0f; // ��ų ��Ÿ�� (��)

    private float remainingCooldown;  // ���� ��Ÿ���� ����
    private bool isCooldown = false;  // ��Ÿ�� ���� �÷���

    void Start()
    {
        // ó������ ��Ÿ���� �ƴϹǷ� �ؽ�Ʈ�� ��Ȱ��ȭ
        if (cooldownText != null)
        {
            cooldownText.gameObject.SetActive(false);
        }
        // ��Ÿ�� �������̵� ��Ȱ��ȭ
        cooldownOverlay.fillAmount = 0;
    }

    void Update()
    {
        // QŰ�� ������ ��Ÿ�� ���� (�׽�Ʈ��)
        if (Input.GetKeyDown(KeyCode.M) && !isCooldown)
        {
            StartCooldown();
        }

        // ��Ÿ���� ���� ���� ���� ���� ����
        if (isCooldown)
        {
            remainingCooldown -= Time.deltaTime;

            if (remainingCooldown > 0)
            {
                // ��Ÿ�� UI ������Ʈ
                cooldownOverlay.fillAmount = 1.0f - (remainingCooldown / cooldownTime);
                if (cooldownText != null)
                {
                    cooldownText.text = remainingCooldown.ToString("F1"); // �Ҽ��� �� �ڸ����� ǥ��
                }
            }
            else
            {
                // ��Ÿ�� ����
                EndCooldown();
            }
        }
    }

    public void StartCooldown()
    {
        isCooldown = true;
        remainingCooldown = cooldownTime;

        // ��Ÿ�� �ؽ�Ʈ Ȱ��ȭ
        if (cooldownText != null)
        {
            cooldownText.gameObject.SetActive(true);
        }

        // ��Ÿ�� �������� �̹����� ������ 126���� ����
        Color overlayColor = cooldownOverlay.color;
        overlayColor.a = 126f / 255f; // ���İ��� 0~1 �����̹Ƿ� 255�� ����
        cooldownOverlay.color = overlayColor;

        cooldownOverlay.fillAmount = 0; // ä��⸦ 0���� ����
    }

    private void EndCooldown()
    {
        isCooldown = false;
        remainingCooldown = 0;

        // ��Ÿ�� ���� UI ��Ȱ��ȭ
        cooldownOverlay.fillAmount = 0; // �������� �����
        if (cooldownText != null)
        {
            cooldownText.gameObject.SetActive(false);
        }

        // ��ų �������� ������ �����ϰ� (���� 255)
        Color iconColor = skillIcon.color;
        iconColor.a = 1f; // ���İ� 1 = 255
        skillIcon.color = iconColor;

        Debug.Log("��ų ��� ����!");
    }
}
