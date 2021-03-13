﻿using UnityEngine;
using System;

/// <summary>
/// Class responsible for handling player attack.
/// </summary>
public class PlayerMeleeAttack : MonoBehaviour, IAction
{
    // Components
    private PlayerInputCustom input;
    private Animator anim;
    private PlayerRoll roll;
    private PlayerUseItem useItem;
    private CinemachineTarget target;
    private PlayerStats stats;

    // Weapon
    [SerializeField] private SphereCollider sword;

    // Trail
    [SerializeField] private ParticleSystem[] particles;

    // Layers
    [SerializeField] private LayerMask hittableLayers;

    // Swordhit
    [SerializeField] private GameObject swordHitPrefab;

    public bool Performing { get; private set; }

    private void Awake()
    {
        input = GetComponent<PlayerInputCustom>();
        anim = GetComponent<Animator>();
        useItem = GetComponent<PlayerUseItem>();
        roll = GetComponent<PlayerRoll>();
        target = FindObjectOfType<CinemachineTarget>();
        stats = GetComponent<PlayerStats>();
    }

    private void Start()
    {
        foreach(ParticleSystem particle in particles)
            particle.Stop();
    }

    private void OnEnable()
    {
        input.MeleeLightAttack += MeleeLightAttack;
        input.MeleeStrongAttack += MeleeStrongAttack;
    }

    private void OnDisable()
    {
        input.MeleeLightAttack -= MeleeLightAttack;
        input.MeleeStrongAttack -= MeleeStrongAttack;
    }

    public void ComponentUpdate()
    {
        
    }

    public void ComponentFixedUpdate()
    {
        
    }

    /// <summary>
    /// Handles light attack.
    /// </summary>
    private void MeleeLightAttack()
    {
        if (roll.Performing == false && useItem.Performing == false)
        {
            if (target.Targeting)
            {
                // Rotates player to target
                transform.LookAt(target.CurrentTarget);
                transform.eulerAngles = new Vector3(
                    0f, transform.eulerAngles.y, transform.eulerAngles.z);
            }

            Performing = true;

            anim.applyRootMotion = true;

            OnLightMeleeAttack();
        }
    }

    /// <summary>
    /// Handles strong attack.
    /// </summary>
    private void MeleeStrongAttack()
    {
        if (roll.Performing == false && useItem.Performing == false)
        {
            if (target.Targeting)
            {
                transform.LookAt(target.CurrentTarget);
                transform.eulerAngles = new Vector3(0f, transform.eulerAngles.y, transform.eulerAngles.z);
            }

            Performing = true;

            anim.applyRootMotion = true;

            OnStrongMeleeAttack();
        }
    }

    /// <summary>
    /// Turns trail to false. Runs on animation event.
    /// </summary>
    private void TurnTrailToTrue()
    {
        foreach (ParticleSystem particle in particles)
            particle.Play();
    }

    /// <summary>
    /// Turns trail to false. Runs on animation event.
    /// </summary>
    private void TurnTrailToFalse()
    {
        foreach (ParticleSystem particle in particles)
            particle.Stop();
    }

    /// <summary>
    /// Turns of root motion. Runs on animation event.
    /// </summary>
    private void TurnOffRootMotion()
    {
        anim.ResetTrigger("MeleeLightAttack");
        anim.ResetTrigger("MeleeStrongAttack");

        Performing = false;

        anim.speed = 1f;
        anim.applyRootMotion = false;
    }

    /// <summary>
    /// Checks attack collision through animation event.
    /// </summary>
    public void CheckLightAttackCollision()
    {
        Collider[] swordCol = 
            Physics.OverlapSphere(
                sword.transform.position + sword.center, sword.radius, hittableLayers);

        if (swordCol.Length > 0)
        {
            for (int i = 0; i < swordCol.Length; i++)
            {
                if (swordCol[i].gameObject.TryGetComponent(out IDamageable enemy))
                {
                    enemy.TakeDamage(stats.LightDamage);       
                }
                Instantiate(swordHitPrefab, sword.transform.position + sword.center, Quaternion.identity);
            }
        }
    }

    /// <summary>
    /// Checks attack collision through animation event.
    /// </summary>
    public void CheckStrongAttackCollision()
    {
        Collider[] swordCol =
            Physics.OverlapSphere(
                sword.transform.position + sword.center, sword.radius * 1.5f, hittableLayers);

        if (swordCol.Length > 0)
        {
            for (int i = 0; i < swordCol.Length; i++)
            {
                if (swordCol[i].gameObject.TryGetComponent(out IDamageable enemy))
                {
                    enemy.TakeDamage(stats.StrongDamage);
                }
                Instantiate(swordHitPrefab, sword.transform.position + sword.center, Quaternion.identity);
            }
        }
    }


    protected virtual void OnLightMeleeAttack() => LightMeleeAttack?.Invoke();

    protected virtual void OnStrongMeleeAttack() => StrongMeleeAttack?.Invoke();

    /// <summary>
    /// Event registered on PlayerAnimations.
    /// </summary>
    public event Action LightMeleeAttack;

    /// <summary>
    /// Event registered on PlayerAnimations.
    /// </summary>
    public event Action StrongMeleeAttack;
}
