﻿using UnityEngine;

public class PlayerInstantKillAnimationBehaviour : StateMachineBehaviour
{
    private PlayerMovement playerMovement;
    private PlayerMeleeAttack playerAttack;

    private void Awake()
    {
        playerMovement = FindObjectOfType<PlayerMovement>();
        playerAttack = FindObjectOfType<PlayerMeleeAttack>();
    }

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        playerMovement.OnHide(false);
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // This walking is used to always use instant kill in case the player releases the key
        playerMovement.Walking = true;
        playerAttack.InInstantKill = true;
        playerAttack.Performing = true;
        playerAttack.Anim.applyRootMotion = true;
    }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        playerAttack.InInstantKill = false;
        playerAttack.Performing = false;
        playerAttack.Anim.applyRootMotion = false;
        playerAttack.Anim.ResetTrigger("MeleeLightAttack");

        // If player is still pressing walk, player will walk, else it will go back to normal movement
        if (playerMovement.PressingWalk) playerMovement.HandleWalk(true);
        else playerMovement.HandleWalk(false);
    }

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
