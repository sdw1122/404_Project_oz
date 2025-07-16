using Photon.Pun;
using UnityEngine;

public class PlayerHealth : LivingEntity
{
   /* public Slider healthSlider; // 체력을 표시할 UI 슬라이더*/

    public AudioClip deathClip; // 사망 소리
    public AudioClip hitClip; // 피격 소리
    public AudioClip itemPickupClip; // 아이템 습득 소리
    public float current_health;
    private AudioSource playerAudioPlayer; // 플레이어 소리 재생기
    private Animator playerAnimator; // 플레이어의 애니메이터    
    PhotonView pv;
    private PlayerController playerController; // 플레이어 움직임 컴포넌트

    private void Awake()
    {
        // 사용할 컴포넌트를 가져오기
        playerAnimator = GetComponent<Animator>();
        playerAudioPlayer = GetComponent<AudioSource>();
        playerController = GetComponent<PlayerController>();        
        current_health = startingHealth;
        pv= GetComponent<PhotonView>();
    }
    protected override void OnEnable()
    {
        // LivingEntity의 OnEnable() 실행 (상태 초기화)
        base.OnEnable();
        /*healthSlider.gameObject.SetActive(true);
        healthSlider.maxValue = startingHealth;
        healthSlider.value = health;
        playerMovement.enabled = true;*/

    }
    public override void RestoreHealth(float newHealth)
    {
        // LivingEntity의 RestoreHealth() 실행 (체력 증가)
        base.RestoreHealth(newHealth);
        /*healthSlider.value = health;*/
    }
    //데미지 처리
    public override void OnDamage(float damage, Vector3 hitPoint, Vector3 hitNormal)
    {
        // LivingEntity의 OnDamage() 실행(데미지 적용)
        if (!dead)
        {
            //playerAudioPlayer.PlayOneShot(hitClip);
            playerAnimator.SetTrigger("Hit");
            pv.RPC("RPC_TriggerPenHit", RpcTarget.Others);

        }
        base.OnDamage(damage, hitPoint, hitNormal);
        current_health = health;
        /*healthSlider.value = health;*/
    }
    public override void Die()
    {
        // LivingEntity의 Die() 실행(사망 적용)
        
        base.Die();
        Debug.Log(dead);
        
        //playerAudioPlayer.PlayOneShot(deathClip);
        playerController.canMove = false;        
        playerAnimator.ResetTrigger("Hit");
        playerAnimator.SetTrigger("Die");
        pv.RPC("RPC_TriggerPenDie", RpcTarget.Others);
        /*healthSlider.gameObject.SetActive(false);
        
        playerMovement.enabled = false;
        playerShooter.enabled = false;*/
    }
    [PunRPC]
    void RPC_TriggerPenHit()
    {
        playerAnimator.SetTrigger("Hit");
    }
    [PunRPC]
    void RPC_TriggerPenDie()
    {
        playerAnimator.ResetTrigger("Hit");
        if (!dead) // 중복 방지
        {
            dead = true;
            playerAnimator.SetTrigger("Die");
        }
    }
}
