using System.Collections;
using UnityEngine;

public class StrawKingRazor : MonoBehaviour
{
    public Animator animator;
    public ParticleSystem[] chargeEffect;

    public float chargeTime = 4f;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void RazorCharge()
    {
        animator.speed = 0f;
        foreach (ParticleSystem particleSystem in chargeEffect)
        {
            var CEmain = particleSystem.main;
            CEmain.duration = chargeTime;
            CEmain.startLifetime = chargeTime;
            particleSystem.Play();
        }
        StartCoroutine(AfterCharge());
    }
    IEnumerator AfterCharge()
    {
        yield return new WaitForSeconds(chargeTime);
        animator.speed = 1f;
    }
}
