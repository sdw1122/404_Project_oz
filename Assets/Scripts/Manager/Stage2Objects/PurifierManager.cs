using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

/// <summary>
/// 맵의 모든 CooperativePurifier의 완료 상태를 관리하고,
/// 모든 장치가 완료되면 지정된 장애물을 비활성화합니다.
/// </summary>
[RequireComponent(typeof(PhotonView))]
public class PurifierManager : MonoBehaviourPunCallbacks
{
    // 이 매니저에 쉽게 접근할 수 있도록 싱글턴 인스턴스를 제공합니다.
    public static PurifierManager Instance;

    [Header("관리 대상")]
    [Tooltip("맵에 있는 모든 정화 장치(CooperativePurifier)를 여기에 연결하세요.")]
    [SerializeField] private List<CooperativePurifier> purifiers;

    [Tooltip("모든 정화 장치가 완료되면 비활성화할 장애물 게임 오브젝트입니다.")]
    [SerializeField] private GameObject obstacleObject;

    // 완료된 정화 장치의 수를 추적하는 변수 (마스터 클라이언트에서만 사용)
    private int completedPurifierCount = 0;

    void Awake()
    {
        // 싱글턴 패턴 설정
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// CooperativePurifier가 완료될 때 호출하는 메서드입니다.
    /// 마스터 클라이언트에서만 실행되어 완료 카운트를 안전하게 증가시킵니다.
    /// </summary>
    [PunRPC]
    public void NotifyPurifierCompleted()
    {
        // 이 로직은 마스터 클라이언트만 실행합니다.
        if (!PhotonNetwork.IsMasterClient) return;

        completedPurifierCount++;
        Debug.Log($"정화 장치 완료! (현재 {completedPurifierCount} / {purifiers.Count}개)");

        // 모든 정화 장치가 완료되었는지 확인합니다.
        if (completedPurifierCount >= purifiers.Count)
        {
            Debug.Log("모든 정화 장치 완료! 장애물을 비활성화합니다.");
            // 모든 클라이언트에게 장애물을 비활성화하라는 명령을 보냅니다.
            photonView.RPC("RPC_DeactivateObstacle", RpcTarget.AllBuffered);
        }
    }

    /// <summary>
    /// [RPC] 모든 클라이언트에서 장애물 오브젝트를 비활성화합니다.
    /// </summary>
    [PunRPC]
    private void RPC_DeactivateObstacle()
    {
        if (obstacleObject != null)
        {
            obstacleObject.SetActive(false);
        }
    }
}