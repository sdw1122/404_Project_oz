using UnityEngine;
using UnityEngine.InputSystem;

public class Skill1 : MonoBehaviour
{
    Animator animator;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        animator.SetBool("Charge", true);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
