using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class TowerBuilder : MonoBehaviour
{
    [Tooltip("���� �� ��� Prefab ����Ʈ")]
    public GameObject[] wallModules;
    [Tooltip("Ÿ�� �� ����(��� �ϳ� ����)")]
    public float moduleHeight = 2f;
    [Tooltip("�� ���� �� ��")]
    public int floorCount = 10;
    [Tooltip("������ ȸ�� ����(0�̸� �ױ⸸)")]
    public float angleStep = 0f;

    // �����Ϳ��� ��ư Ŭ����
#if UNITY_EDITOR
    [ContextMenu("Build Tower")]
    void BuildTower()
    {
        // ���� �ڽ� ����
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        for (int i = 0; i < floorCount; i++)
        {
            // ���� �Ǵ� ���������� wallModules ����
            var prefab = wallModules[i % wallModules.Length];
            var go = PrefabUtility.InstantiatePrefab(prefab, transform) as GameObject;
            go.transform.localPosition = Vector3.up * (moduleHeight * i);
            go.transform.localRotation = Quaternion.Euler(0, angleStep * i, 0);
        }
    }
#endif
}
