using UnityEngine;
using Photon.Pun;

/// <summary>
/// 허수아비 왕 보스전의 전체적인 흐름과 UI를 관리하는 매니저 스크립트입니다.
/// </summary>
public class StrawKingBossManager : MonoBehaviour
{
    [Header("보스 및 UI 설정")]
    [Tooltip("씬에 있는 StrawKing 보스를 연결하세요.")]
    [SerializeField] private StrawKing strawKingBoss;

    [Tooltip("화면에 생성할 보스 UI 프리팹을 연결하세요.")]
    [SerializeField] private GameObject bossUIPrefab;

    void Start()
    {
        // 모든 클라이언트가 각자 자신의 UI를 생성합니다.
        if (bossUIPrefab != null && strawKingBoss != null)
        {
            // UI 프리팹을 씬에 생성
            GameObject uiInstance = Instantiate(bossUIPrefab);

            // UI 컨트롤러 스크립트를 가져옴
            StrawKingUIController uiController = uiInstance.GetComponent<StrawKingUIController>();

            if (uiController != null)
            {
                // UI 컨트롤러에 보스 정보를 넘겨주어 초기화
                uiController.Setup(strawKingBoss);
                Debug.Log("StrawKing 보스 UI가 성공적으로 생성 및 연결되었습니다.");
            }
            else
            {
                Debug.LogError("생성된 UI 프리팹에서 StrawKingUIController 컴포넌트를 찾지 못했습니다!");
            }
        }
        else
        {
            Debug.LogError("StrawKingBossManager에 보스 또는 UI 프리팹이 연결되지 않았습니다!");
        }
    }
}