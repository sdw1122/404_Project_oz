using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MovingObj : MonoBehaviour
{
    public Transform startPoint;
    public Transform endPoint;
    public float moveDuration = 5f;
    public int roundTime = 2;

    // 이동량 파라미터
    public Vector3 MoveDelta { get; private set; }

    private Vector3 lastPosition;
    private bool coroutineIsRunning = false;
    private List<PlayerController> passengers = new List<PlayerController>();


    private void FixedUpdate()
    {
        // 매 프레임마다 이동량 계산, 플레이어와의 update 주기 맞추기위해 FIxedUpdate
        MoveDelta = transform.position - lastPosition;
        lastPosition = transform.position;
        for (int i = passengers.Count - 1; i >= 0; i--)
        {
            if (passengers[i] == null || passengers[i].GetComponent<PlayerHealth>().dead==true)
            {
                passengers.RemoveAt(i);
                
            }
        }
    }

    // 플레이어가 올라타면 이동 시작
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<PlayerController>(out PlayerController controller))
        {
            //승객 목록에 플레이어 추가
            if (!passengers.Contains(controller))
            {
                passengers.Add(controller);
            }

            // 코루틴 스타트
            if (!coroutineIsRunning)
            {
                StartCoroutine(MoveRoutine());
            }
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<PlayerController>(out PlayerController controller))
        {
            // 플레이어 제거, 발판 이동량 계산 멈춤
            if (passengers.Contains(controller))
            {
                controller.ClearPlatform();
                passengers.Remove(controller);
            }
        }
    }

    // 왕복 이동
    private IEnumerator MoveRoutine()
    {
        coroutineIsRunning = true;
        int time = roundTime;
        while (time > 0)
        {
            yield return StartCoroutine(MoveTo(transform.position, endPoint.position));
            yield return StartCoroutine(MoveTo(transform.position, startPoint.position));
            time--;
        }
        gameObject.SetActive(false);
        coroutineIsRunning = false;
    }
    // 발판 이동
    private IEnumerator MoveTo(Vector3 start, Vector3 end)
    {
        float elapsedTime = 0f;
        while (elapsedTime < moveDuration)
        {
            transform.position = Vector3.Lerp(start, end, elapsedTime / moveDuration);
            elapsedTime += Time.deltaTime;
            yield return new WaitForFixedUpdate();
        }
        transform.position = end;
    }
}