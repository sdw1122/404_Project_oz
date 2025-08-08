using System.Collections;
using System.Collections.Generic; // List 사용을 위해 추가
using UnityEngine;

public class MovingObj : MonoBehaviour
{
    public Transform startPoint;
    public Transform endPoint;
    public float moveDuration = 5f;

    // --- 변수 추가 ---
    private Vector3 lastPosition; // 이전 프레임의 발판 위치
    private List<CharacterController> passengers = new List<CharacterController>(); // 발판 위의 플레이어 목록

    private void Start()
    {
        // 시작 시 현재 위치를 기록
        lastPosition = transform.position;
        // 발판이 자동으로 움직이게 하려면 여기서 코루틴을 시작하세요.
        StartCoroutine(MoveRoutine());
    }

    // LateUpdate는 모든 Update가 끝난 후 마지막에 호출되어 위치 동기화에 적합합니다.
    private void LateUpdate()
    {
        // 이번 프레임에 발판이 얼마나 움직였는지 계산
        Vector3 moveDelta = transform.position - lastPosition;

        // 움직임이 있었다면
        if (moveDelta != Vector3.zero)
        {
            // 발판 위의 모든 승객(플레이어)에게
            foreach (CharacterController passenger in passengers)
            {
                // 발판이 움직인 만큼 똑같이 플레이어를 움직여줍니다.
                passenger.Move(moveDelta);
            }
        }

        // 다음 프레임을 위해 현재 위치를 다시 기록
        lastPosition = transform.position;
    }

    // 플레이어가 감지 영역에 들어왔을 때
    private void OnTriggerEnter(Collider other)
    {
        // CharacterController를 가진 오브젝트가 들어오면 승객 목록에 추가
        if (other.TryGetComponent<CharacterController>(out CharacterController controller))
        {
            if (!passengers.Contains(controller))
            {
                passengers.Add(controller);
            }
        }
    }

    // 플레이어가 감지 영역에서 나갔을 때
    private void OnTriggerExit(Collider other)
    {
        // CharacterController를 가진 오브젝트가 나가면 승객 목록에서 제거
        if (other.TryGetComponent<CharacterController>(out CharacterController controller))
        {
            if (passengers.Contains(controller))
            {
                passengers.Remove(controller);
            }
        }
    }

    // 왕복 이동 코루틴 (이 부분은 그대로 사용하거나 필요에 맞게 수정)
    private IEnumerator MoveRoutine()
    {
        while (true)
        {
            yield return StartCoroutine(MoveTo(transform.position, endPoint.position));
            yield return StartCoroutine(MoveTo(transform.position, startPoint.position));
        }
    }

    private IEnumerator MoveTo(Vector3 start, Vector3 end)
    {
        float elapsedTime = 0f;
        while (elapsedTime < moveDuration)
        {
            transform.position = Vector3.Lerp(start, end, elapsedTime / moveDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.position = end;
    }
}