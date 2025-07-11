using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class HealthBar : MonoBehaviour
{
    public Image fillImage;
    private float maxHealth = 100f;
    private float currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthBar();
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth < 0)
        {
            currentHealth = 0;
        }
        UpdateHealthBar();

        // ü���� 0 ���ϰ� �Ǹ� GameManager�� �˸�
        if (currentHealth <= 0)
        {
            PJS_GameManager.Instance.PlayerDied();
        }
    }

    // ü���� �ִ�� �ٽ� ä��� �Լ� (GameManager�� ȣ��)
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        float fillAmount = currentHealth / maxHealth;
        fillImage.fillAmount = fillAmount;
    }
}