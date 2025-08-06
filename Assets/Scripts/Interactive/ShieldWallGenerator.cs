using UnityEngine;
using Photon.Pun;
using System.Collections;

/// <summary>
/// 플레이어가 상호작용하여 지정된 방어벽을 '활성화/비활성화'하는 스크립트입니다.
/// </summary>
public class ShieldWallGenerator : InteractableBase
{
    [Header("활성화 대상")]
    [Tooltip("활성화시킬 방어벽 게임 오브젝트를 씬에서 직접 연결하세요.")]
    [SerializeField] private GameObject shieldWallObject;

    [Header("쿨타임")]
    [Tooltip("방어벽을 활성화한 후 다시 사용하기까지의 쿨타임(초)입니다.")]
    [SerializeField] private float cooldown = 10f;

    private bool isReady = true;

    void Start()
    {
        // 게임 시작 시 방어벽이 비활성화 상태인지 확인합니다.
        if (shieldWallObject != null)
        {
            shieldWallObject.SetActive(false);
        }
        else
        {
            Debug.LogError("ShieldWallGenerator에 방어벽 오브젝트가 연결되지 않았습니다!", this);
        }
    }

    public override void Interact(PlayerController player)
    {
        // 준비 상태이고, 방어벽 오브젝트가 연결되어 있으며, 현재 비활성화 상태일 때만 작동
        if (isReady && shieldWallObject != null && !shieldWallObject.activeInHierarchy)
        {
            // 모든 클라이언트에게 방어벽을 활성화하라는 RPC를 보냅니다.
            pv.RPC("RPC_ToggleWall", RpcTarget.All, true);

            // 쿨타임 적용
            isReady = false;
            StartCoroutine(CooldownRoutine());
        }
    }

    /// <summary>
    /// [RPC] 모든 클라이언트에서 방어벽의 활성화/비활성화 상태를 변경합니다.
    /// </summary>
    [PunRPC]
    private void RPC_ToggleWall(bool isActive)
    {
        if (shieldWallObject != null)
        {
            shieldWallObject.SetActive(isActive);
        }
    }

    private IEnumerator CooldownRoutine()
    {
        yield return new WaitForSeconds(cooldown);
        isReady = true;
    }

    public override bool CanInteract(PlayerController player)
    {
        // 쿨타임 중이거나 방어벽이 이미 활성화되어 있으면 상호작용 불가
        if (!isReady || (shieldWallObject != null && shieldWallObject.activeInHierarchy))
        {
            return false;
        }
        return base.CanInteract(player);
    }
}