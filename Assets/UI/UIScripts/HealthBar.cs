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

        // 체력이 0 이하가 되면 GameManager에 알림
        if (currentHealth <= 0)
        {
            PJS_GameManager.Instance.PlayerDied();
        }
    }

    // 체력을 최대로 다시 채우는 함수 (GameManager가 호출)
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