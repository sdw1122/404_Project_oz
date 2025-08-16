using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun; // Photon 기능을 사용하기 위해 추가

/// <summary>
/// [동기화 버전] 마스터 클라이언트가 몬스터의 죽음을 감지하고,
/// 모든 클라이언트의 장애물을 동시에 비활성화하는 스크립트입니다.
/// </summary>
[RequireComponent(typeof(PhotonView))] // 이 스크립트는 PhotonView 컴포넌트가 반드시 필요합니다.
public class ObstacleByMonster : MonoBehaviour
{
    [Header("감시할 몬스터")]
    [Tooltip("이 리스트에 있는 몬스터들이 모두 죽어야 장애물이 사라집니다. 씬에 있는 몬스터들을 여기에 연결하세요.")]
    public List<Enemy> enemiesToMonitor;

    [Header("제어할 장애물")]
    [Tooltip("몬스터들이 모두 죽었을 때 비활성화시킬 장애물 오브젝트를 연결하세요.")]
    [SerializeField] private GameObject obstacleObject;

    private PhotonView pv;

    // 이미 조건이 충족되었는지 확인하는 플래그 (중복 실행 방지)
    private bool isConditionMet = false;

    void Awake()
    {
        pv = GetComponent<PhotonView>();
    }

    void Start()
    {
        if (obstacleObject == null)
        {
            Debug.LogError("장애물 오브젝트가 연결되지 않았습니다!", this);
            enabled = false;
            return;
        }

        // 몬스터 리스트에서 null인 항목을 미리 제거합니다.
        enemiesToMonitor.RemoveAll(item => item == null);

        // 감시할 몬스터가 처음부터 없다면, 즉시 장애물을 비활성화합니다.
        // 이 로직은 마스터 클라이언트에서만 실행되어야 합니다.
        if (PhotonNetwork.IsMasterClient && enemiesToMonitor.Count == 0)
        {
            Debug.LogWarning("감시할 몬스터가 설정되지 않았습니다. 장애물을 즉시 비활성화합니다.");
            // RPC를 통해 모든 클라이언트의 장애물을 비활성화합니다.
            pv.RPC("RPC_DeactivateObstacle", RpcTarget.AllBuffered);
        }
    }

    void Update()
    {
        // *** 핵심 변경점 1: 마스터 클라이언트만 아래 로직을 실행 ***
        // 몬스터의 상태를 감시하고 장애물을 없애는 결정은 마스터 클라이언트만 내립니다.
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        if (isConditionMet || enemiesToMonitor.Count == 0)
        {
            return;
        }

        // 몬스터 리스트를 순회하며 죽었는지 확인합니다.
        // 리스트를 뒤에서부터 순회해야 순회 중에 아이템을 제거해도 오류가 발생하지 않습니다.
        for (int i = enemiesToMonitor.Count - 1; i >= 0; i--)
        {
            if (enemiesToMonitor[i] == null || enemiesToMonitor[i].dead)
            {
                enemiesToMonitor.RemoveAt(i);
            }
        }

        // 모든 몬스터가 리스트에서 제거되었다면 (즉, 모두 죽었다면)
        if (enemiesToMonitor.Count == 0)
        {
            // *** 핵심 변경점 2: 직접 비활성화하는 대신 RPC 호출 ***
            // 마스터 클라이언트는 장애물을 직접 끄는 대신, 모든 클라이언트에게 끄라는 "명령"을 내립니다.
            DialogueManager.Instance.pv.RPC("StartDialogue_RPC", RpcTarget.All, "GoBoss");
            pv.RPC("RPC_DeactivateObstacle", RpcTarget.AllBuffered);
        }
    }

    /// <summary>
    /// [RPC] 모든 클라이언트에서 실행되어 장애물을 비활성화합니다.
    /// </summary>
    [PunRPC]
    private void RPC_DeactivateObstacle()
    {
        // 이 코드는 RPC를 수신한 모든 클라이언트(나 자신 포함)에서 실행됩니다.
        if (isConditionMet) return; // 중복 실행 방지

        Debug.Log("모든 몬스터가 처치되어 장애물을 비활성화합니다.");
        if (obstacleObject != null)
        {
            obstacleObject.SetActive(false);
        }
        isConditionMet = true;
    }
}