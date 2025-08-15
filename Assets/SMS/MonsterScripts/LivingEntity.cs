using Photon.Pun;
using System;
using UnityEngine;
using UnityEngine.AI;

// 생명체로서 동작할 게임 오브젝트들을 위한 뼈대를 제공
// 체력, 데미지 받아들이기, 사망 기능, 사망 이벤트를 제공
public class LivingEntity : MonoBehaviour, IDamageable, IPunObservable
{    
    public float startingHealth = 100f; // 시작 체력
    public float health { get; protected set; } // 현재 체력
    public bool dead = false;
    public event Action onDeath; // 사망시 발동할 이벤트
    public bool HasDeathHandler => onDeath != null;

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        // 데이터를 보내는 측일 경우 (주인 또는 마스터 클라이언트)
        if (stream.IsWriting)
        {
            // 현재 체력(health) 값을 네트워크로 보냅니다.
            stream.SendNext(health);
        }
        // 데이터를 받는 측일 경우 (다른 클라이언트)
        else
        {
            // 네트워크로부터 체력 값을 받아서 내 health 변수에 덮어씁니다.
            this.health = (float)stream.ReceiveNext();
        }
    }

    // 생명체가 활성화될때 상태를 리셋
    protected virtual void OnEnable()
    {
        // 사망하지 않은 상태로 시작
        dead = false;
        // 체력을 시작 체력으로 초기화
        health = startingHealth;
    }

    // 데미지를 입는 기능
    public virtual void OnDamage(float damage, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (dead) return;
        // 데미지만큼 체력 감소
        health -= damage;
        Debug.Log("남은체력" + health);
        // 체력이 0 이하 && 아직 죽지 않았다면 사망 처리 실행
        if (health <= 0 && !dead)
        {            
            Die();
        }
    }
    protected virtual void OnPostDamage(float damage, GameObject attacker) { }
    // 체력을 회복하는 기능
    public virtual void RestoreHealth(float newHealth)
    {
        if (dead)
        {
            // 이미 사망한 경우 체력을 회복할 수 없음
            return;
        }

        // 체력 추가
        health += newHealth;
    }
    public virtual void Resurrection()
    {
        health = startingHealth;
        dead = false; 
    }
    // 사망 처리
    public virtual void Die()
    {
        Debug.Log("livingentity의 die 호출됨");        
        // onDeath 이벤트에 등록된 메서드가 있다면 실행
        if (onDeath != null)
        {
            onDeath();
        }
    }
    
}