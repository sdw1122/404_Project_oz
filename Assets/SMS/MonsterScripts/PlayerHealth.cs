using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHealth : LivingEntity
{
    public float current_health;
    private Animator playerAnimator; // 플레이어의 애니메이터
    PhotonView pv;
    private PlayerController playerController; // 플레이어 움직임 컴포넌트

    private void Awake()
    {
        playerAnimator = GetComponent<Animator>();
        playerController = GetComponent<PlayerController>();
        current_health = startingHealth;
        pv = GetComponent<PhotonView>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
    }

    public override void RestoreHealth(float newHealth)
    {
        base.RestoreHealth(newHealth);
    }

    public override void Resurrection()
    {
        base.Resurrection();
    }

    // 데미지 처리
    public override void OnDamage(float damage, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (!dead)
        {
            playerAnimator.SetTrigger("Hit");
            // Hit 애니메이션은 모든 클라이언트에서 동기화되어야 하므로 RPC 호출
            pv.RPC("RPC_TriggerPlayerHit", RpcTarget.All);
        }
        base.OnDamage(damage, hitPoint, hitNormal);
        current_health = health;
    }

    public override void Die()
    {
        // LivingEntity의 Die() 실행 (사망 적용)
        base.Die();
        Debug.Log(dead);

        // 사망 시 애니메이션 및 컴포넌트 비활성화는 모든 클라이언트에서 동기화
        pv.RPC("RPC_TriggerPlayerDie", RpcTarget.All);
    }

    [PunRPC]
    void RPC_TriggerPlayerHit()
    {
        playerAnimator.SetTrigger("Hit");
    }

    [PunRPC]
    void RPC_TriggerPlayerDie()
    {
        playerAnimator.ResetTrigger("Hit");
        playerAnimator.SetTrigger("Die");
        /* if (!dead) // 중복 호출 방지
         {
             dead = true;
             playerAnimator.SetTrigger("Die");
         }*/

        // 움직임 중지
        if (playerController != null)
        {
            playerController.canMove = false;
        }

        // 조작비활성화 (그 클라이언트만)
        if (pv.IsMine)
        {
            /*if (TryGetComponent(out PlayerInput playerInput))
            {
                playerInput.enabled = false;
            }*/
            if (TryGetComponent(out Hammer hammer))
            {
                hammer.enabled = false;
            }
            if (TryGetComponent(out PenAttack penAttack))
            {
                Debug.Log("펜 어택 false");
                penAttack.enabled = false;
            }
            if (TryGetComponent(out Pen_Skill_1 penSkill1))
            {
                penSkill1.enabled = false;
            }
            if (TryGetComponent(out Pen_Skill_2 penSkill2))
            {
                penSkill2.enabled = false;
            }
            
        }
    }
}