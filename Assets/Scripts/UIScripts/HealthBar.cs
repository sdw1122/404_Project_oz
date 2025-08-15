using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class HealthBar : MonoBehaviour
{
    public Image fillImage;        // 채워질 UI 이미지 (에디터에서 할당)
    private PlayerHealth playerHealth; // 연동할 플레이어 체력 스크립트
    private PhotonView photonView;     // 플레이어의 PhotonView

    void Awake()
    {
        // 프리팹의 부모 오브젝트에서 PlayerHealth와 PhotonView 컴포넌트를 찾아옴
        playerHealth = GetComponentInParent<PlayerHealth>();
        photonView = GetComponentInParent<PhotonView>();
    }

    void Update()
    {
        // photonView를 찾았고, 이 HealthBar가 내 소유의 플레이어 것인지 확인
        if (photonView != null && photonView.IsMine)
        {
            // PlayerHealth 컴포넌트를 성공적으로 찾았는지 확인
            if (playerHealth != null)
            {
                // PlayerHealth 스크립트로부터 최대 체력과 현재 체력 정보를 가져옴
                float maxHealth = playerHealth.startingHealth;
                float currentHealth = playerHealth.health; // LivingEntity로부터 상속받은 health 변수

                // 체력 비율을 계산하여 UI 이미지의 fillAmount에 적용
                if (maxHealth > 0)
                {
                    fillImage.fillAmount = currentHealth / maxHealth;
                }
            }
        }
        else
        {
            // 내 플레이어의 HealthBar가 아니라면 UI를 비활성화
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }
    }
}