using Photon.Pun;
using UnityEngine;
using System.Collections;

public class WisdomCannon : InteractableBase
{
    public Transform firePoint;
    public GameObject cannonballPrefab;
    public Animator animator;
    public float cannonDamage=400f;
    public float cannonballSpeed = 20f;

    public bool isShot = false;

    protected override void Awake()
    {
        base.Awake();
        animator = GetComponent<Animator>();
        
    }
    public override void Interact(PlayerController player)
    {
        pv.RPC("TryFireWisdom", RpcTarget.MasterClient);
    }

    [PunRPC]
    public void TryFireWisdom()
    {
        if(WisdomManager.Instance.GetCurrentWisdom()>=WisdomManager.Instance.requiredWisdom && !isShot)
        {
            WisdomManager.Instance.UseWisdom(WisdomManager.Instance.requiredWisdom);
            pv.RPC(nameof(FireWisdom), RpcTarget.MasterClient);
        }
        else if (isShot)
        {
            Debug.Log("이미 발사함");
        }
        else
        {
            Debug.Log("지혜가 부족합니다");
        }
    }
    [PunRPC]
    public void FireWisdom()
    {
        GameObject cannonball = PhotonNetwork.Instantiate("test/" + cannonballPrefab.name, firePoint.position, Quaternion.identity);
        WisdomCannonBall ecb = cannonball.GetComponent<WisdomCannonBall>();
        Rigidbody rb = cannonball.GetComponent<Rigidbody>();
        if (rb != null)
        {
            ecb.Initialize(cannonDamage);
            rb.AddForce(firePoint.forward * cannonballSpeed, ForceMode.VelocityChange);
            isShot = true;            
            Transform child = gameObject.transform.Find("Small_cannon");
            StartCoroutine(RotateCannonSmoothly(child, 340f, 20f, 1.5f));
        }
    }

    public IEnumerator RotateCannonSmoothly(Transform cannon, float fromAngle, float toAngle, float duration)
    {
        float elapsed = 0f;
        Vector3 startEuler = cannon.localEulerAngles;
        Vector3 newEuler = startEuler;
        while (elapsed < duration)
        {
            Debug.Log("되냐?");
            float angle = Mathf.LerpAngle(fromAngle, toAngle, elapsed / duration);
            newEuler = startEuler;    // 기준점 매번 재설정(혹은 new Vector3(angle, startEuler.y, startEuler.z))
            newEuler.x = angle;
            cannon.localEulerAngles = newEuler;
            Debug.Log($"코루틴 내부 euler.x:{newEuler.x} 실제:{cannon.localEulerAngles.x}");
            elapsed += Time.deltaTime;
            yield return null;
        }
        newEuler = startEuler;
        newEuler.x = toAngle;
        cannon.localEulerAngles = newEuler;
    }
}
