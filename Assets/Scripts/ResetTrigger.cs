using UnityEngine;

public class ResetTrigger : StateMachineBehaviour
{
    public string[] triggers; // Inspector���� ["TriggerA","TriggerB"] �Է�

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        foreach (var t in triggers)
            animator.ResetTrigger(t);
    }
}