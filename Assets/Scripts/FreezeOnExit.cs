using UnityEngine;

public class FreezeOnExit : StateMachineBehaviour
{
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.enabled = false;
    }
}