using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHealth : LivingEntity
{
    public float current_health;
    private Animator playerAnimator; // 플레이어의 애니메이터
    public PhotonView pv;
    private PlayerController playerController; // 플레이어 움직임 컴포넌트
    private PlayerInput playerInput;    

    private void Awake()
    {
        playerAnimator = GetComponent<Animator>();
        playerController = GetComponent<PlayerController>();
        current_health = startingHealth;
        pv = GetComponent<PhotonView>();
        playerInput = GetComponent<PlayerInput>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
    }

    public override void RestoreHealth(float newHealth)
    {
        base.RestoreHealth(newHealth);
        pv.RPC("RPC_TriggerPlayerHeal", RpcTarget.All, newHealth);
    }

    public override void Resurrection()
    {
        base.Resurrection();

        pv.RPC("SetDeadState", RpcTarget.All, false);
        pv.RPC("RPC_TriggerPlayerResurrection", RpcTarget.All);
        pv.RPC("DeActivateCamera", RpcTarget.All);
    }

    [PunRPC]

    public void DeActivateCamera()
    {
        if (pv.IsMine)
        {
            playerController.Deactivate();
        }
    }

    // 데미지 처리
    [PunRPC]
    public override void OnDamage(float damage, Vector3 hitPoint, Vector3 hitNormal)
    {
        // 이 코드가 주인이 아닌 마스터 클라이언트에서 실행된 경우
        if (!pv.IsMine && PhotonNetwork.IsMasterClient)
        {
            // 이 PhotonView의 주인(Player 객체) 정보를 가져옵니다.
            Photon.Realtime.Player owner = pv.Owner;

            // 주인이 정상적으로 있다면, 그 주인 플레이어에게만 RPC를 보냅니다.
            if (owner != null)
            {
                pv.RPC("OnDamage", owner, damage, hitPoint, hitNormal);
            }

            return; // 중계 역할 후 종료
        }

        // --- 이 아래 코드는 실제 주인 클라이언트만 실행합니다. ---
        if (dead) return;

        // 전달받은 만큼 자신의 체력을 직접 깎습니다.
        health -= damage;
        current_health = health;

        if (!playerController.isCharge)
        {
            // 피격 애니메이션을 로컬에서 실행합니다.
            playerAnimator.SetTrigger("Hit");
        }

        if (health <= 0)
        {
            Die();
        }
    }

    public override void Die()
    {
        PJS_GameManager.Instance.photonView.RPC("ProcessPlayerDeath", RpcTarget.MasterClient);
        // LivingEntity의 Die() 실행 (사망 적용)
        base.Die();
        dead = true;
        Debug.Log(dead);

        // 사망 시 애니메이션 및 컴포넌트 비활성화는 모든 클라이언트에서 동기화
        pv.RPC("SetDeadState", RpcTarget.All, true);
        pv.RPC("RPC_TriggerPlayerDie", RpcTarget.All);
        pv.RPC("DeadCamera", RpcTarget.All);
        // 죽은 사람은 누구든지 상관없이 woodman에게 전달
        if (pv.IsMine)
        {
            WoodMan woodMan = FindFirstObjectByType<WoodMan>();
            if (woodMan != null)
            {
                AggroSystem aggroSystem = woodMan.GetComponent<AggroSystem>();
                if (aggroSystem != null)
                {
                    aggroSystem.GetComponent<PhotonView>().RPC("ResetAndHalfAggro", RpcTarget.MasterClient, pv.ViewID);
                    Debug.Log($"{this.gameObject}:WoodMan에게 어그로 리셋 전달 완료");
                }
                else
                {
                    Debug.LogWarning("AggroSystem 컴포넌트 없음");
                }
            }
            else
            {
                Debug.LogWarning("WoodMan을 씬에서 찾지 못함");
            }
        }
    }
 

    [PunRPC]
    public void DeadCamera()
    {
        if (pv.IsMine)
        {
            playerController.ActivateCamera();
        }
    }

    [PunRPC]
    public void SetDeadState(bool isDead)
    {
        dead = isDead;
    }
    [PunRPC]
    void RPC_TriggerPlayerHit()
    {
        playerAnimator.SetTrigger("Hit");
    }
    [PunRPC]
    void RPC_TriggerPlayerHeal(float healAmount)
    {
        if (!pv.IsMine || dead) { return; }
        if (health < startingHealth)
        {
            health += healAmount;
            if (health > startingHealth)
            {
                health = startingHealth;
            }
        }


    }
    [PunRPC]
    void RPC_TriggerPlayerDie()
    {
        playerAnimator.ResetTrigger("Hit");
        playerAnimator.SetTrigger("Die");


        // 움직임 중지


        // 조작비활성화 (그 클라이언트만)
        if (pv.IsMine)
        {
            
            playerInput.actions.FindAction("Move")?.Disable();
            playerInput.actions.FindAction("Attack")?.Disable();
            playerInput.actions.FindAction("Skill1")?.Disable();
            playerInput.actions.FindAction("Skill2")?.Disable();
            playerInput.actions.FindAction("Jump")?.Disable();
            playerInput.actions.FindAction("Sprint")?.Disable();
            playerInput.actions.FindAction("Look")?.Disable();
            playerInput.actions.FindAction("Resurrection")?.Disable();
            playerInput.actions.FindAction("HealRay")?.Disable();
        }
    }
    [PunRPC]
    void RPC_TriggerPlayerResurrection()
    {


        playerAnimator.ResetTrigger("Die");
        playerAnimator.SetTrigger("Resurrection");
        // 조작활성화,체력 동기화
        if (pv.IsMine)
        {
            health = startingHealth;
            Debug.Log(health);
            Debug.Log($"[RPC] {name} 부활 RPC 실행됨. IsMine: {pv.IsMine}");

            playerInput.actions.FindAction("Attack")?.Enable();
            playerInput.actions.FindAction("Skill1")?.Enable();
            playerInput.actions.FindAction("Skill2")?.Enable();
            playerInput.actions.FindAction("Jump")?.Enable();
            playerInput.actions.FindAction("Sprint")?.Enable();
            playerInput.actions.FindAction("Look")?.Enable();
            playerInput.actions.FindAction("Move")?.Enable();
            playerInput.actions.FindAction("Resurrection")?.Enable();
            playerInput.actions.FindAction("HealRay")?.Enable();
        }

    }
}