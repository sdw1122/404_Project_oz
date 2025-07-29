// 파일 이름: EnemyHealthBarController.cs

using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBarController : MonoBehaviour
{
    public Slider healthSlider;

    // 모든 체력 바 UI가 공유할 단 하나의 '정적(static)' 카메라 변수입니다.
    // static으로 선언하면, 이 변수는 모든 EnemyHealthBarController 인스턴스가 함께 사용합니다.
    public static Camera LocalPlayerCamera { get; set; }

    void LateUpdate()
    {
        // LocalPlayerCamera 변수에 카메라가 할당되었다면, 그 카메라를 바라보도록 회전합니다.
        // LateUpdate에서 처리해야 카메라 움직임이 모두 끝난 후 UI가 회전하여 떨림이 없습니다.
        if (LocalPlayerCamera != null)
        {
            transform.LookAt(transform.position + LocalPlayerCamera.transform.rotation * Vector3.forward,
                             LocalPlayerCamera.transform.rotation * Vector3.up);
        }
    }

    /// <summary>
    /// 체력 바의 값을 업데이트하고, 상태에 따라 보여주거나 숨깁니다.
    /// </summary>
    public void UpdateHealth(float currentHealth, float maxHealth)
    {
        // 체력이 가득 찼거나, 0 이하면 숨기고, 그 외의 경우(즉, 피해를 입은 상태)에만 보여줍니다.
        bool shouldBeVisible = currentHealth > 0 && currentHealth < maxHealth;

        // 현재 상태와 달라질 필요가 있을 때만 SetActive를 호출하여 성능을 아낍니다.
        if (healthSlider.gameObject.activeSelf != shouldBeVisible)
        {
            healthSlider.gameObject.SetActive(shouldBeVisible);
        }

        // 슬라이더의 값을 0과 1 사이의 비율로 설정합니다.
        healthSlider.value = currentHealth / maxHealth;
    }

    /// <summary>
    /// 몬스터가 죽었을 때 체력 바를 확실하게 숨깁니다.
    /// </summary>
    public void Hide()
    {
        healthSlider.gameObject.SetActive(false);
    }
}