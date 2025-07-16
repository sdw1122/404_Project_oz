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
    }

    public override void Resurrection()
    {
        base.Resurrection();
        pv.RPC("SetDeadState", RpcTarget.All, false);
        pv.RPC("RPC_TriggerPlayerResurrection", RpcTarget.All);
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
        dead = true;
        Debug.Log(dead);

        // 사망 시 애니메이션 및 컴포넌트 비활성화는 모든 클라이언트에서 동기화
        pv.RPC("SetDeadState", RpcTarget.All, true);
        pv.RPC("RPC_TriggerPlayerDie", RpcTarget.All);
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
        }
    }
    [PunRPC]
    void RPC_TriggerPlayerResurrection()
    {
        
       
        playerAnimator.ResetTrigger("Die");
        playerAnimator.SetTrigger("Resurrection");
        // 조작활성화 (그 클라이언트만)
        if (pv.IsMine)
        {
            Debug.Log($"[RPC] {name} 부활 RPC 실행됨. IsMine: {pv.IsMine}");
            Debug.Log($"죽어있는가? : {dead}");
            playerInput.actions.FindAction("Attack")?.Enable();
            playerInput.actions.FindAction("Skill1")?.Enable();
            playerInput.actions.FindAction("Skill2")?.Enable();
            playerInput.actions.FindAction("Jump")?.Enable();
            playerInput.actions.FindAction("Sprint")?.Enable();
            playerInput.actions.FindAction("Look")?.Enable();
            playerInput.actions.FindAction("Move")?.Enable();
            playerInput.actions.FindAction("Resurrection")?.Enable();

            Debug.Log(playerInput.actions.FindAction("Move"));
        }
        
    }
}