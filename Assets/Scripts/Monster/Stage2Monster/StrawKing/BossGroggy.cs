using Photon.Pun;
using UnityEngine;
using System.Collections;

public class BossGroggy : MonoBehaviour
{
    PhotonView pv;
    Animator animator;    
    public Transform[] roadTransform;
    public GameObject[] road = new GameObject[3];    
    public WisdomCannon[] cannon = new WisdomCannon[3];

    public int count = 0;
    public bool isGroggy = false;
    public bool isFirst = true;

    private float groggyTime = 10f;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        pv = GetComponent<PhotonView>();
        animator = GetComponent<Animator>();              
    }

    // Update is called once per frame
    void Update()
    {
        if (count == 3)
        {
            if (!isGroggy)
            {
                pv.RPC("BossIsGroggy", RpcTarget.All);
                isGroggy = true;

                if (isFirst)
                {
                    pv.RPC("MakeRoad", RpcTarget.All);
                    isFirst = false;
                }
                else
                {
                    pv.RPC("ReMakeRoad", RpcTarget.All);
                }
                
            }            
        }  
    }

    [PunRPC]
    public void MakeRoad()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        for (int i = 0; i < 3; i++)
        {
            road[i].transform.position = roadTransform[i].position;
            road[i].transform.rotation = roadTransform[i].rotation;
        }        
    }

    [PunRPC]
    public void ReMakeRoad()
    {
        for (int i = 0; i < 3; i++)
        {
            road[i].SetActive(true);          
        }
    }

    [PunRPC]
    public void BossIsGroggy()
    {
        animator.SetTrigger("Groggy");
    }

    public void PauseGroggy()
    {
        animator.speed = 0f; // 애니메이션 일시 정지
        StartCoroutine(ResumeAnimator(groggyTime)); // 15초 뒤 재개(원하는 시간으로 조절)
    }

    IEnumerator ResumeAnimator(float delay)
    {
        yield return new WaitForSeconds(delay);
        animator.speed = 1f; // 애니메이션 재생 재개
    }
    
    public void PauseShake()
    {
        animator.speed = 0f;
        pv.RPC("DoingShake", RpcTarget.All);
    }

    [PunRPC]
    public void DoingShake()
    {
        StartCoroutine(DoShake(10, 0.2f));
    }

    IEnumerator DoShake(float shakeTime, float magnitude)
    {
        // 각각 흔들기
        GameObject roadObj = GameObject.Find("RealTestRoad(Clone)");
        Platform testRoad = null;
        if (roadObj != null)
        {
            testRoad = roadObj.GetComponent<Platform>();
        }
        if (testRoad != null)
        {
            Coroutine shakeRoutine = testRoad.StartCoroutine(testRoad.Shake(shakeTime, magnitude));
            yield return new WaitForSeconds(shakeTime);
            if (shakeRoutine != null)
                testRoad.StopCoroutine(shakeRoutine);

            Collider col = testRoad.GetComponent<Collider>();
            Rigidbody rb = testRoad.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = false;
            if (col != null) col.isTrigger = true;
        }        
        
        // 바닥 떨어짐
        for (int i = 0; i < 3; i++)
        {
            pv.RPC("RoadReload", RpcTarget.All, i);
            pv.RPC("CannonReload", RpcTarget.All, i);
        }

        animator.speed = 1f;
    }

    [PunRPC]
    public void CannonReload(int i)
    {
        Transform child = cannon[i].transform.Find("Small_cannon");
        StartCoroutine(cannon[i].RotateCannonSmoothly(child, 20f, 340f, 1.5f));
        cannon[i].isShot = false;
    }

    [PunRPC]
    public void RoadReload(int i)
    {
        road[i].SetActive(false);
    }
}
