using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class TowerBuilder : MonoBehaviour
{
    [Tooltip("쌓을 벽 모듈 Prefab 리스트")]
    public GameObject[] wallModules;
    [Tooltip("타워 층 높이(모듈 하나 높이)")]
    public float moduleHeight = 2f;
    [Tooltip("총 쌓을 층 수")]
    public int floorCount = 10;
    [Tooltip("층마다 회전 각도(0이면 쌓기만)")]
    public float angleStep = 0f;

    // 에디터에서 버튼 클릭용
#if UNITY_EDITOR
    [ContextMenu("Build Tower")]
    void BuildTower()
    {
        // 기존 자식 삭제
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        for (int i = 0; i < floorCount; i++)
        {
            // 랜덤 또는 순차적으로 wallModules 선택
            var prefab = wallModules[i % wallModules.Length];
            var go = PrefabUtility.InstantiatePrefab(prefab, transform) as GameObject;
            go.transform.localPosition = Vector3.up * (moduleHeight * i);
            go.transform.localRotation = Quaternion.Euler(0, angleStep * i, 0);
        }
    }
#endif
}
