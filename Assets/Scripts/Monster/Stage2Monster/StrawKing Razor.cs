using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.VFX;

public class StrawKingRazor : MonoBehaviour
{
    public Animator animator;
    public ParticleSystem chargeEffect;
    public VisualEffect blockedRazor;
    public VisualEffect Razor;
    public ParticleSystem shoutEffect;

    public bool isBlock = false;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }


    public void RazorCharge()
    {
        animator.speed = 0f;
        chargeEffect.Play();
        StartCoroutine(StopAnimation(4.0f));
    }
    IEnumerator StopAnimation(float stopTime)
    {
        yield return new WaitForSeconds(stopTime);
        animator.speed = 1.0f;
    }

    public void RazorEffect()
    {
        animator.speed = 0f;
        Debug.Log("isBlock" + isBlock);
        if (isBlock)
        {
            blockedRazor.Play();
        }
        else
        {
            Razor.Play();
        }
        StartCoroutine(BeamAfter(3.0f));

    }
    private IEnumerator BeamAfter(float duration)
    {
        float timer = 0.0f;
        while (timer < duration)
        {
            Razor.transform.rotation = Quaternion.RotateTowards(Razor.transform.rotation, transform.rotation * Quaternion.Euler(0f, -20.0f, 0f), 12.7f * Time.deltaTime);
            timer += Time.deltaTime;
            yield return null;
        }
        animator.speed = 1.0f;
        isBlock = false;
        Razor.transform.rotation = transform.rotation * Quaternion.Euler(0f,20.0f,0f);
    }

    public void ShoutEffectPlay()
    {
        shoutEffect.Play();
    }
    public void ShoutEffectStop()
    {
        shoutEffect.Stop();
    }
}
