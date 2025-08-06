using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RazorBeam : MonoBehaviour
{
    public Transform startPoint;
    public Transform endPoint;
    public float scrollSpeed = 2f;

    private LineRenderer lr;
    private float offset;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
    }

    void Update()
    {
        // 1) 시작/끝 지점 실시간 업데이트
        lr.SetPosition(0, startPoint.position);
        lr.SetPosition(1, endPoint.position);

        // 2) 머티리얼 텍스처 오프셋 스크롤
        offset += Time.deltaTime * scrollSpeed;
        lr.material.mainTextureOffset = new Vector2(-offset, 0);
    }
}
