using UnityEngine;

public class ResetTrigger : StateMachineBehaviour
{
    public string[] triggers; // Inspector에서 ["TriggerA","TriggerB"] 입력

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        foreach (var t in triggers)
            animator.ResetTrigger(t);
    }
}