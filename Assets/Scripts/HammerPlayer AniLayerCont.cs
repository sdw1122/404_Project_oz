using UnityEngine;

[RequireComponent(typeof(Animator))]
public class HammerPlayerAniLayerCont : MonoBehaviour
{
    Animator animator;
    int upperLayerIndex;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        animator = GetComponent<Animator>();
        upperLayerIndex = animator.GetLayerIndex("Upper Body");
    }

    public void UpperAniEnd()
    {
        animator.SetLayerWeight(upperLayerIndex, 0.0001f);
    }
    public void UpperAniStart()
    {
        animator.SetLayerWeight(upperLayerIndex, 1);
    }
}
