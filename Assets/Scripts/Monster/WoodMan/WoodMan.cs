using Photon.Pun;
using System.Collections;
using UnityEngine;
using static WoodMan;

public class WoodMan : Enemy
{
    public string m_name = "갈망하는 양철 나무꾼";
    // 어그로 시스템
    private AggroSystem aggroSystem;
    public GameObject currentTarget;
    public float groggyTime=8.0f;
    // 공격들
    WoodManAttack woodManAttack;
    WoodManEarthQuake woodManQuake;
    WoodManRoar woodManRoar;
    float originalDamage;
    float meleeAttackRange;
    float QuakeRange;
    float roarRange;
    public float minChaseDistance=0.5f;
    Rigidbody rb;
    bool canAct = true;
    Animator animator;
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
    private void Awake()
    {
        animator = GetComponent<Animator>();
        aggroSystem = GetComponent<AggroSystem>();
        woodManAttack = GetComponent<WoodManAttack>();
        woodManQuake = GetComponent<WoodManEarthQuake>();
        rb=GetComponent<Rigidbody>();   
        woodManRoar = GetComponent<WoodManRoar>();
        _currentState=WoodMan_State.Idle;
        _currentMode = WoodMan_Mode.Normal;
        meleeAttackRange = woodManAttack.meleeAttackRange;
        QuakeRange = woodManQuake.skillRadius;
        roarRange= woodManRoar.skillRadius;
        attackRange = meleeAttackRange;
        originalDamage = WoodManAttack.meleeAttackDamage;

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
                StartCoroutine(DelayedAction(2.833f));
                break;
            case WoodMan_State.EarthQuake:
                pv.RPC("EarthQuakeRPC", RpcTarget.All);
                _currentState = WoodMan_State.Idle;
                StartCoroutine(DelayedAction(2.633f));
                break;

            
        }
        
    }
    private IEnumerator DelayedAction(float delay)
    {
        yield return new WaitForSeconds(delay);
        canAct = true;
    }
    protected override void Update()
    {
        
        if (!PhotonNetwork.IsMasterClient) return;
        if (!canAct) return;
        if (isBinded && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.isStopped = true;
            return;
        }
        SwitchMode();
        // 갈망 업데이트
        UpdateAggroTarget();

        float distanceToTarget = Vector3.Distance(transform.position, targetEntity.transform.position);
        //
        if (distanceToTarget < minChaseDistance)
        {
            navMeshAgent.isStopped = true;
            rb.isKinematic = true;
            return;
        }

        // 공격 조건 체크
        if (woodManAttack.IsReady() && distanceToTarget < meleeAttackRange)
        {
            base.attackRange = meleeAttackRange;
            _currentState = WoodMan_State.MeleeAttack;
            navMeshAgent.isStopped = true;
            rb.isKinematic = false; // 물리 활성화
            canAct = false;
            return;
        }
        // 충격파 포효
        if (woodManRoar.IsReady() && distanceToTarget < roarRange && distanceToTarget > QuakeRange)
        {
            base.attackRange = roarRange;
            _currentState = WoodMan_State.Roar;
            navMeshAgent.isStopped = true;
            rb.isKinematic = false;
            canAct = false;
            return;
        }
        // 대지 파쇄
        if (woodManQuake.isReady() && woodManQuake.IsTargetInRange())
        {
            base.attackRange = QuakeRange;
            _currentState = WoodMan_State.EarthQuake;
            navMeshAgent.isStopped = true;
            rb.isKinematic = false;
            canAct = false;
            enemyAnimator.SetFloat("Blend", 0f); // 공격 전 Idle자세
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
                WoodManAttack.meleeAttackDamage = originalDamage;
                break;
            case WoodMan_Mode.Overheat:
                DEF_Factor = 0.8f;
                WoodManAttack.meleeAttackDamage *= 1.25f;
                break;
            case WoodMan_Mode.Vulnerable:
                DEF_Factor = 1.5f;
                WoodManAttack.meleeAttackDamage = originalDamage;
                pv.RPC("VulnerableRPC", RpcTarget.All);
                
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

        
        yield return new WaitForSeconds(groggyTime);

       
        animator.SetTrigger("Awake");

        
        yield return new WaitForSeconds(2.767f);

        
        SetMode(WoodMan_Mode.Normal);
        isBinded = false;
    }
    public void SetMode(WoodMan_Mode newMode)
    {
        _currentMode = newMode;
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
        GameObject target = aggroSystem.GetTopAggroTarget();
        if (target != null)
            targetEntity = target.GetComponent<LivingEntity>();
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Lava")&&_currentMode==WoodMan_Mode.Normal)
        {
            SetMode(WoodMan_Mode.Overheat);
            Debug.Log("Overheat 전환");
        }
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Coolant") && _currentMode == WoodMan_Mode.Overheat)
        {
            SetMode(WoodMan_Mode.Vulnerable);
            Debug.Log("냉각상태 전환");
        }
    }


}
