using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdleBlendBehavior : StateMachineBehaviour
{
    const float CHANGETIME = 5f;
    float m_TimeLeft = CHANGETIME;
    //bool changed = false;

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        m_TimeLeft = CHANGETIME;
        //changed = false;
        int RunningStateHashCached = Animator.StringToHash("Base Layer.Running");
        int IdleStateHashCached = Animator.StringToHash("Base Layer.Idle_Blend");

        if (RunningStateHashCached == animator.GetCurrentAnimatorStateInfo(0).fullPathHash)
        {
            animator.SetFloat("IdleType", 0.5f);
        }
        else if (IdleStateHashCached == animator.GetCurrentAnimatorStateInfo(0).fullPathHash)
        {
            //animator.SetFloat("IdleType", 1f);
        }
        else
        {
            //animator.SetFloat("IdleType", 0);
            animator.SetFloat("IdleType", 0);
        }

    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (m_TimeLeft >= 0.0f)
            m_TimeLeft -= Time.deltaTime;
        else
        {
            m_TimeLeft = CHANGETIME;
            //animator.CrossFadeInFixedTime("Idle_Blend", 2f);

        }
    }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    //override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateMove is called right after Animator.OnAnimatorMove()
    //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that processes and affects root motion
    //}

    // OnStateIK is called right after Animator.OnAnimatorIK()
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that sets up animation IK (inverse kinematics)
    //}

}
