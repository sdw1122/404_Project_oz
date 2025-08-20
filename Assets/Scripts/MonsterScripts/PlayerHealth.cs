using Photon.Pun;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerHealth : LivingEntity
{
    public float current_health;
    private Animator playerAnimator; // 플레이어의 애니메이터
    public PhotonView pv;
    private PlayerController playerController; // 플레이어 움직임 컴포넌트
    private PlayerInput playerInput;
    public ParticleSystem resurrectionEffect;
    public ParticleSystem HealingEffect;
    public float resurrectionDelay = 5f;
    public Camera myDeadCam;
    bool isReadyToRes=false;
    public GameObject respawnUI;
    public TextMeshProUGUI respawnText;
    Respawn respawn;
    private string respawnJob;
    private string respawnFlagLabel;

    private void Awake()
    {
        playerAnimator = GetComponent<Animator>();
        playerController = GetComponent<PlayerController>();
        current_health = startingHealth;
        pv = GetComponent<PhotonView>();
        playerInput = GetComponent<PlayerInput>();
        respawn= GetComponentInChildren<Respawn>();
        if (respawnUI != null) respawnUI.SetActive(false);
    }
    private void Update()
    {
       
        if (pv.IsMine && isReadyToRes && Input.GetKeyDown(KeyCode.T))
        {

            RespawnAtFlag();

        }
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
    [PunRPC]
    public override void Resurrection()
    {
        isReadyToRes = false;
        if (respawnUI != null) respawnUI.SetActive(false);
        base.Resurrection();
        respawn.DeactiveCol();

        AudioManager.instance.PlaySfxAtLocation("Player Resurrection",transform.position);
        resurrectionEffect.Play();
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

        var stateInfo = playerAnimator.GetCurrentAnimatorStateInfo(0);
        if (!stateInfo.IsTag("Attack"))
        {
            // 피격 애니메이션을 실행
            GetComponent<Pen_Skill_1>()?.CancelCharging();
            GetComponent<Hammer>()?.CancelCharging();
            playerAnimator.SetTrigger("Hit");
            AudioManager.instance.PlaySfxAtLocation("Player Hit",transform.position);
            pv.RPC("RPC_TriggerPlayerHit", RpcTarget.Others);
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
        respawn.ActiveCol();
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
            if (respawnUI != null) respawnUI.SetActive(true);
        }
        //죽은 사람이 마스터에게 요청
        pv.RPC("SendDataToClient", RpcTarget.MasterClient,pv.Owner.UserId);
    }
    [PunRPC]
    public void SendDataToClient(string userID)
    {
        PlayerSaveData data = SaveSystem.LoadPlayerData(userID);
        string job = data.userJob;
        string latestFlag = data.latestFlag;
        pv.RPC("ReceiveAndDelayResurrect", pv.Owner, job, latestFlag);
    }
    [PunRPC]
    public void ReceiveAndDelayResurrect(string job,string latestFlag)
    {
        if (!pv.IsMine) return;
        if (!dead) return;
        StartCoroutine(DelayedResurrection(resurrectionDelay,job,latestFlag));
    }
    private IEnumerator DelayedResurrection(float delay,string job,string latestFlag)
    {   
        Debug.Log("딜레이 부활 호출됨");
        float timer = delay;
        respawnJob = job;
        respawnFlagLabel = latestFlag;
        
        while (timer > 0)
        {
            if (respawnText != null)
            {
                // 남은시간 표시
                respawnText.text = $"Time To Respawn... {Mathf.CeilToInt(timer)}";
            }
            timer -= Time.deltaTime;
            yield return null; // 다음 프레임까지 대기
        }
        
        if (!dead) yield break;
        /*string userId = pv.Owner.UserId;
        PlayerSaveData data = SaveSystem.LoadPlayerData(userId);*/

       
            
            


        if (pv.IsMine)
        {
            isReadyToRes = true;
            if (respawnText != null)
            {
                respawnText.text = "Press T To Respawn";
            }
            // 부활 안내 UI를 켭니다.
            if (respawnUI != null) respawnUI.SetActive(true);
            Debug.Log("부활 준비 완료. 키 입력을 기다립니다.");
        }
    }
    public void RespawnAtFlag()
    {
        SaveFlag targetFlag = FindObjectsByType<SaveFlag>(default).FirstOrDefault(f => f.label == respawnFlagLabel);

        if (targetFlag != null)
        {
            Transform spawn = targetFlag.SaveFlagGetSpawnPos(respawnJob);

            if (pv.IsMine) // 위치 이동은 본인만
            {
                CharacterController cc = GetComponent<CharacterController>();
                cc.enabled = false;
                transform.position = spawn.position;
                transform.rotation = spawn.rotation;
                cc.enabled = true;
                Debug.Log($"[Resurrection] 깃발 위치로 이동: {spawn.position}");
            }
        }
        Resurrection();
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
            HealingEffect.Play();
            AudioManager.instance.PlaySfxAtLocation("Player Healed", transform.position);
            pv.RPC("RPC_HealEffectPlay", RpcTarget.Others);
            if (health > startingHealth)
            {
                health = startingHealth;
            }
        }


    }
    [PunRPC] 
    void RPC_HealEffectPlay()
    {
        HealingEffect.Play();
        AudioManager.instance.PlaySfxAtLocation("Player Healed", transform.position);
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

        resurrectionEffect.Play();
        AudioManager.instance.PlaySfxAtLocation("Player Resurrection", transform.position);
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