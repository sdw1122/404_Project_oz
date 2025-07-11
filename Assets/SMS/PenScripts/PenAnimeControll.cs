using Photon.Pun;
using UnityEngine;

public class PenAnimeControll : MonoBehaviour
{
    Animator animator;
    Rigidbody rb;
    PhotonView pv;
    private void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
    }
    private void Update()
    {
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float speed = horizontalVelocity.magnitude;

        
        animator.SetFloat("Move", speed);
     



    }
    
}
