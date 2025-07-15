using UnityEngine;

public class Test : MonoBehaviour
{
    public float moveAmount = 1f; // 한 번에 이동할 높이
    public float moveDuration = 0.5f; // 이동에 걸리는 시간(초)

    private bool isMoving = false;
    void Awake()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (!isMoving)
        {
            StartCoroutine(MoveUp());
        }
    }
    System.Collections.IEnumerator MoveUp()
    {
        isMoving = true;
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + Vector3.up * moveAmount;
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            transform.position = Vector3.Lerp(startPos, endPos, elapsed / moveDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = endPos;
        isMoving = false;
    }
}
