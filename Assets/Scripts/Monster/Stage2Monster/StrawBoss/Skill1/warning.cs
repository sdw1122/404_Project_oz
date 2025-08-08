using UnityEngine;

public class warning : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        if (Camera.main != null)
        {
            Vector3 targetPos = Camera.main.transform.position;
            Vector3 myPos = transform.position;

            // y만 맞추고 x, z는 고정!
            targetPos.y = myPos.y;

            // y축만 회전
            transform.LookAt(targetPos);
            transform.Rotate(0, 180, 0); // 이미지 방향이 거꾸로면 추가
        }
    }
}
