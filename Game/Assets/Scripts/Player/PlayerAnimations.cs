﻿using UnityEngine;
using System;

/// <summary>
/// Class responsible for playing player's animations, uses a kind of sandbox pattern.
/// This class also handles most of the animation events.
/// </summary>
public class PlayerAnimations : MonoBehaviour
{
    // Components
    private Animator anim;
    private PlayerMovement movement;
    private PlayerRoll roll;
    private PlayerMeleeAttack attack;
    private PlayerBlock block;
    private PlayerDeathBehaviour death;
    private PlayerWallHug wallHug;
    private PlayerInputCustom input;
    private CharacterController controller;
    private PlayerStats stats;
    private PlayerUseItem useItem;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        movement = GetComponent<PlayerMovement>();
        roll = GetComponent<PlayerRoll>();
        attack = GetComponent<PlayerMeleeAttack>();
        block = GetComponent<PlayerBlock>();
        death = GetComponent<PlayerDeathBehaviour>();
        wallHug = GetComponent<PlayerWallHug>();
        input = FindObjectOfType<PlayerInputCustom>();
        controller = GetComponent<CharacterController>();
        stats = GetComponent<PlayerStats>();
        useItem = GetComponent<PlayerUseItem>();
    }

    private void OnEnable()
    {
        roll.Roll += TriggerRollAnimation;
        attack.LightMeleeAttack += TriggerLightMeleeAttack;
        death.PlayerDied += TriggerDeath;
        stats.NoDamageBlock += TriggerBlockReflect;
        stats.TookDamage += TriggerOnHitAnimation;
    }

    private void OnDisable()
    {
        roll.Roll -= TriggerRollAnimation;
        attack.LightMeleeAttack -= TriggerLightMeleeAttack;
        death.PlayerDied -= TriggerDeath;
        stats.NoDamageBlock -= TriggerBlockReflect;
        stats.TookDamage -= TriggerOnHitAnimation;
    }

    private void Update()
    {
        anim.SetFloat("MovementSpeed", movement.MovementSpeed);
        anim.SetBool("IsGrounded", movement.IsGrounded());
        anim.SetBool("Block", block.Performing);
        anim.SetBool("Walking", movement.Walking);
        anim.SetBool("Dead", stats.Dead);
        if (wallHug.InvertedAnimation == false)
            anim.SetFloat("WallHugSpeed", input.Movement.x * controller.velocity.magnitude);
        else
            anim.SetFloat("WallHugSpeed", -input.Movement.x * controller.velocity.magnitude);
        anim.SetBool("BotWallHug", wallHug.Performing);
    }

    /// <summary>
    /// Triggers on hit animation.
    /// </summary>
    private void TriggerOnHitAnimation()
    {
        if (useItem.Performing == false && roll.Performing == false)
            anim.SetTrigger("OnHit");
    }

    private void TriggerDeath() =>
        anim.SetTrigger("Death");

    private void TriggerBlockReflect() => anim.SetTrigger("BlockReflect");

    private void TriggerRollAnimation() => anim.SetTrigger("Rolling");

    /// <summary>
    /// Triggers light melee attack. Normal or instant kill animation
    /// depending on the condition.
    /// </summary>
    /// <param name="condition">If true, it triggers normal attacks, else
    /// it triggers instant kill animation.</param>
    private void TriggerLightMeleeAttack(bool condition)
    {
        if (condition == true)
            anim.SetTrigger("MeleeLightAttack");
        else
            anim.SetTrigger("InstantKill");
    }

    /// <summary>
    /// The action of this item is called on PlayerUseItem class.
    /// Its behaviour is called with animation events with a method on that class.
    /// </summary>
    public void TriggerKunaiAnimation() => anim.SetTrigger("ThrowKunai");

    /// <summary>
    /// The action of this item is called on PlayerUseItem class.
    /// Its behaviour is called with animation events with a method on that class.
    /// </summary>
    public void TriggerHealthFlaskAnimation() => anim.SetTrigger("UseHealthFlask");

    /// <summary>
    /// The action of this item is called on PlayerUseItem class.
    /// Its behaviour is called with animation events with a method on that class.
    /// </summary>
    public void TriggerSmokeGrenadeAnimation() => anim.SetTrigger("ThrowSmokeGrenade");

    /// <summary>
    /// This method is called with an animation event.
    /// The method is triggered on the final of the animation.
    /// Sets active SpawnUI and triggers pause.
    /// </summary>
    private void AnimationEventDeathBehaviour()
    {
        OnPlayerDiedEndOfAnimation();
        OnPlayerDiedEndOfAnimation(PauseSystemEnum.Paused);
    }

    protected virtual void OnPlayerDiedEndOfAnimation() =>
        PlayerDiedEndOfAnimationUIRespawn?.Invoke();
    protected virtual void OnPlayerDiedEndOfAnimation(PauseSystemEnum condition) =>
        PlayerDiedEndOfAnimationPauseSystem?.Invoke(condition);

    /// <summary>
    /// Event registered on UIRespawn. Only happens after the player death animation is over.
    /// Calls respawn screen UI.
    /// </summary>
    public event Action PlayerDiedEndOfAnimationUIRespawn;

    /// <summary>
    /// Event registered on PauseSystem. Triggers pause after the animation is over.
    /// Pauses game.
    /// </summary>
    public event Action<PauseSystemEnum> PlayerDiedEndOfAnimationPauseSystem;
}
