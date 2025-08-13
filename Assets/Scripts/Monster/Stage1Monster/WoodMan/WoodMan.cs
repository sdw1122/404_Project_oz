using Photon.Pun;
using System.Collections;
using UnityEngine;
using static WoodMan;

public class WoodMan : Enemy
{
    // 어그로 시스템
    private AggroSystem aggroSystem;
    public GameObject currentTarget;
    public float groggyTime=8.0f;
    public float speed = 6.0f;
    // 공격들
    WoodManAttack woodManAttack;
    WoodManEarthQuake woodManQuake;
    WoodManRoar woodManRoar;
    float originalDamage;
    float meleeAttackRange;
    float QuakeRange;
    float roarRange;
    float distanceToTarget;
    public float minChaseDistance=0.5f;
    Rigidbody woodmanRb;    
    Animator animator;

    public AudioSource fireBust;
    public AudioSource steam;

    public enum WoodMan_State 
    {
        
        MeleeAttack,EarthQuake,
        Idle,Roar
    }
    public enum WoodMan_Mode
    {
        Normal, Overheat, Vulnerable
    }
    /*public override bool CanAct()
    {
        return true;
    }*/
    [SerializeField] private WoodMan_State _currentState;
    [SerializeField] public WoodMan_Mode _currentMode;

    [HideInInspector] // Inspector 창에서는 숨김
    public float groggyRemainingTime = 0f;

    public override void Awake()
    {
        base.Awake();
        animator = GetComponent<Animator>();
        aggroSystem = GetComponent<AggroSystem>();
        woodManAttack = GetComponent<WoodManAttack>();
        woodManQuake = GetComponent<WoodManEarthQuake>();
        woodmanRb = GetComponent<Rigidbody>();   
        woodManRoar = GetComponent<WoodManRoar>();
        _currentState=WoodMan_State.Idle;
        _currentMode = WoodMan_Mode.Normal;
        meleeAttackRange = woodManAttack.meleeAttackRange;
        QuakeRange = woodManQuake.skillRadius;
        roarRange= woodManRoar.skillRadius;
        attackRange = meleeAttackRange;
        originalDamage = WoodManAttack.meleeAttackDamage;
        navMeshAgent.speed = speed;

    }
    public override void Attack()
    {
        /*pv.RPC("PerformMeleeAttackRPC", RpcTarget.All);*/
        /*pv.RPC("EarthQuakeRPC", RpcTarget.All);*/
        /*pv.RPC("RoarRPC",RpcTarget.All);*/
        
        switch (_currentState)
        {
            

            case WoodMan_State.MeleeAttack:
                pv.RPC("PerformMeleeAttackRPC", RpcTarget.All);
                _currentState = WoodMan_State.Idle;
                StartCoroutine(DelayedAction(2.4f));
                break;
            case WoodMan_State.Roar:
                pv.RPC("RoarRPC", RpcTarget.All);
                _currentState = WoodMan_State.Idle;
                StartCoroutine(DelayedAction(2.0f));
                break;
            case WoodMan_State.EarthQuake:
                pv.RPC("EarthQuakeRPC", RpcTarget.All);
                _currentState = WoodMan_State.Idle;
                StartCoroutine(DelayedAction(2.5f));
                break;

            
        }
        
    }
    private IEnumerator DelayedAction(float delay)
    {
        yield return new WaitForSeconds(delay);
        isAttacking = false;
    }
    public override void Update()
    {        
        if (!PhotonNetwork.IsMasterClient) return;       
        if (isAttacking) return;
        if (isBinded && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.isStopped = true;
            return;
        }
        base.Update();
        SwitchMode();
        // 갈망 업데이트
        UpdateAggroTarget();
        if(targetEntity != null) 
        {  
            distanceToTarget= Vector3.Distance(transform.position, targetEntity.transform.position); 
        }
        
        //
        if (distanceToTarget < minChaseDistance)
        {
            navMeshAgent.isStopped = true;
            woodmanRb.isKinematic = true;
            return;
        }

        // 공격 조건 체크
        if (woodManAttack.IsReady() && distanceToTarget < meleeAttackRange)
        {
            base.attackRange = meleeAttackRange;
            _currentState = WoodMan_State.MeleeAttack;
            navMeshAgent.isStopped = true;
            isAttacking = true;
            Attack();
            return;
        }
        // 충격파 포효
        if (woodManRoar.IsReady() && distanceToTarget < roarRange && distanceToTarget > QuakeRange)
        {
            base.attackRange = roarRange;
            navMeshAgent.isStopped = true;
            _currentState = WoodMan_State.Roar;

            isAttacking = true;
            Vector3 dir = (targetEntity.transform.position - transform.position).normalized;
            dir.y = 0f;

            /*if (dir != Vector3.zero)
            {
                navMeshAgent.updateRotation = false;
                Quaternion lookRotation = Quaternion.LookRotation(dir);
                transform.rotation = lookRotation;
                navMeshAgent.updateRotation = true;
            }*/
            Attack();
            return;
        }
        // 대지 파쇄
        if (woodManQuake.isReady() && woodManQuake.IsTargetInRange())
        {
            base.attackRange = QuakeRange;
            navMeshAgent.isStopped = true;
            _currentState = WoodMan_State.EarthQuake;
            
            isAttacking = true;
            enemyAnimator.SetFloat("Move", 0f); // 공격 전 Idle자세
            pv.RPC("RPC_BlendIdle", RpcTarget.Others, 0f);
            Attack();
            return;
        }

        // Idle 상태
        _currentState = WoodMan_State.Idle;
        base.attackRange = 0f;
        /*rb.isKinematic = true; // 이동 상태니까 물리 꺼두기*/
        
        /*CurrentState = WoodMan_State.ChaseTarget; */



    }
    void SwitchMode()
    {
        switch(_currentMode)
        {
            case WoodMan_Mode.Normal:
                DEF_Factor = 0.5f;
                woodManAttack.SetDamage(originalDamage);
                woodManQuake.SetDamage(originalDamage*1.5f);
                woodManRoar.SetDamage(originalDamage*2f);
                break;
            case WoodMan_Mode.Overheat:
                DEF_Factor = 0.8f;
                woodManAttack.SetDamage(originalDamage*1.25f);
                woodManQuake.SetDamage(originalDamage*1.5f*1.25f);
                woodManRoar.SetDamage(originalDamage *2f* 1.25f);
                break;
            case WoodMan_Mode.Vulnerable:
                DEF_Factor = 1.5f;
                woodManAttack.SetDamage(originalDamage);
                woodManQuake.SetDamage(originalDamage*1.5f);
                woodManRoar.SetDamage(originalDamage*2f);
                //pv.RPC("VulnerableRPC", RpcTarget.All);
                
                break;
        }
    }
    [PunRPC]
    public void VulnerableRPC()
    {
        StartCoroutine(VulnerableRoutine());
    }

    private IEnumerator VulnerableRoutine()
    {
        isBinded = true;
        animator.SetTrigger("Groggy");

        // --- 타이머 로직 추가 ---
        float timer = groggyTime;
        while (timer > 0)
        {
            groggyRemainingTime = timer; // 남은 시간을 계속 업데이트
            timer -= Time.deltaTime;
            yield return null;
        }
        groggyRemainingTime = 0; // 타이머 종료
        // --- 타이머 로직 끝 ---

        animator.SetTrigger("Awake");
        yield return new WaitForSeconds(2.767f);

        SetMode(WoodMan_Mode.Normal);
        isBinded = false;
    }
    public void SetMode(WoodMan_Mode newMode)
    {
        // 이미 같은 모드이면 아무것도 하지 않음
        if (_currentMode == newMode) return;

        _currentMode = newMode;

        // 모드가 Vulnerable로 변경될 때만 RPC를 호출
        if (newMode == WoodMan_Mode.Vulnerable)
        {
            pv.RPC("VulnerableRPC", RpcTarget.All);
        }
    }

    public void OnDamaged(GameObject attacker, float damage)
    {
        if (attacker != null)
        {
            Debug.Log($"공격자 : {attacker} 데미지 : {damage}");
            aggroSystem.AddAggro(attacker, damage);
        }
    }
    void UpdateAggroTarget()
    {
        GameObject newTargetObject = aggroSystem.GetTopAggroTarget();

        // 새로 찾은 타겟이 있고 (null이 아니고),
        // 그 타겟이 현재 타겟과 다를 경우에만 동기화
        if (newTargetObject != null)
        {
            LivingEntity newTargetEntity = newTargetObject.GetComponent<LivingEntity>();

            // targetEntity가 아직 없거나, 새로 찾은 타겟과 다를 때만 RPC 호출
            if (newTargetEntity != null && targetEntity != newTargetEntity)
            {
                // 부모(Enemy.cs)의 SetTarget RPC를 호출하여 모든 클라이언트의 타겟을 동기화
                pv.RPC("SetTarget", RpcTarget.All, newTargetEntity.GetComponent<PhotonView>().ViewID);
            }
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        // "Lava" 레이어와 겹쳤고, 현재 상태가 "Normal"일 때
        if (other.gameObject.layer == LayerMask.NameToLayer("Lava") && _currentMode == WoodMan_Mode.Normal)
        {
            Debug.Log("[Woodman 상태 변화] Lava와 접촉하여 '과열(Overheat)' 상태로 전환!");
            PlayBustClip();
            SetMode(WoodMan_Mode.Overheat);
        }
        // "Coolant" 레이어와 겹쳤고, 현재 상태가 "Overheat"일 때
        else if (other.gameObject.layer == LayerMask.NameToLayer("Coolant") && _currentMode == WoodMan_Mode.Overheat)
        {
            PlaySteamClip();
            SetMode(WoodMan_Mode.Vulnerable);
        }
    }
    private void PlayBustClip()
    {
        AudioClip clip = fireBust.clip;
        fireBust.PlayOneShot(clip);
    }
    private void PlaySteamClip()
    {
        AudioClip clip = steam.clip;
        steam.PlayOneShot(clip);
    }
}
