﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Base class for all enemies. Implements IFindPlayer to update enemy variables
/// in case the player was lost and spawned.
/// </summary>
/// [RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyStats))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Rigidbody))]

public abstract class EnemyBase : MonoBehaviour, IFindPlayer
{
    // Fields and their getters
    [SerializeField] protected LayerMask myLayer;

    [Header("Enemy target")]
    [SerializeField] protected Transform myTarget;
    public Transform MyTarget => myTarget;

    [Header("Size to alert other enemies when the enemy finds the player")]
    [SerializeField] private float sizeOfAlert;

    [Header("Enemy melee weapon")]
    [SerializeField] protected SphereCollider weaponCollider;
    public SphereCollider WeaponCollider => weaponCollider;

    [Header("Enemy animator")]
    [SerializeField] protected Animator anim;
    public Animator Anim => anim;
    ////////////////////////////////////////////////////////////////////////////

    // State machine variables
    protected IEnumerable<IState> states;
    protected StateMachine stateMachine;

    // Common state for every enemies
    [Header("Enemy death state")]
    [SerializeField] private EnemyAbstractState deathStateOriginal;
    public IState DeathState { get; private set; }

    // Player variables
    public Player Player { get; private set; }
    public bool PlayerCurrentlyFighting
    {
        get => Player.PlayerCurrentlyFighting;
        set
        {
            if (Player != null)
                Player.PlayerCurrentlyFighting = value;
        }
    }

    // Components
    public CinemachineTarget CineTarget { get; private set; }
    public Transform PlayerTarget { get; private set; }
    public NavMeshAgent Agent { get; private set; }
    protected Stats enemyStats;
    protected EnemyAnimationEvents animationEvents;

    protected void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
        CineTarget = FindObjectOfType<CinemachineTarget>();
        enemyStats = GetComponent<Stats>();
        animationEvents = GetComponentInChildren<EnemyAnimationEvents>();

        if (deathStateOriginal != null)
            DeathState = Instantiate(deathStateOriginal);
    }

    /// <summary>
    /// Happens once on enable, registers to events.
    /// </summary>
    private void OnEnable()
    {
        enemyStats.Die += OnDeath;
        animationEvents.Hit += OnWeaponHit;
    }

    /// <summary>
    /// Happens once on disable, unregisters from events.
    /// </summary>
    private void OnDisable()
    {
        enemyStats.Die -= OnDeath;
        animationEvents.Hit -= OnWeaponHit;
    }

    /// <summary>
    /// Runs on state machine states.
    /// </summary>
    private void FixedUpdate()
    {
        stateMachine?.FixedUpdate();
    }

    /// <summary>
    /// In case this enemy finds the player, it alerts the surrounding enemies.
    /// </summary>
    public void AlertSurroundings()
    {
        Collider[] enemiesAround =
            Physics.OverlapSphere(myTarget.position, sizeOfAlert, myLayer);

        if (enemiesAround.Length > 0)
        {
            foreach (Collider enemyCollider in enemiesAround)
            {
                if (enemyCollider.TryGetComponent(out EnemySimple otherEnemy))
                {
                    if (otherEnemy.gameObject != gameObject)
                    {
                        otherEnemy.OnAlert();
                    }
                }
            }
        }
    }

    protected virtual void OnAlert()
    {
        // Left brank on purpose
    }

    /// <summary>
    /// Method that triggers DeathState.
    /// Is triggered when enemy's health reaches 0.
    /// </summary>
    private void OnDeath()
    {
        if (DeathState != null)
            stateMachine?.SwitchToNewState(DeathState);
    }

    /// <summary>
    /// Finds Player when the Player spawns.
    /// </summary>
    public void FindPlayer()
    {
        Player = FindObjectOfType<Player>();
        if (Player != null)
        {
            PlayerTarget =
                GameObject.FindGameObjectWithTag("playerTarget").transform;
        }
    }

    /// <summary>
    /// Turns PlayerTarget to null when the Player disappears.
    /// </summary>
    public void PlayerLost()
    {
        PlayerTarget = myTarget;
        PlayerCurrentlyFighting = false;
    }

    /// <summary>
    /// Invokes WeaponHit event.
    /// </summary>
    protected virtual void OnWeaponHit() => WeaponHit?.Invoke();

    /// <summary>
    /// Event registered on Aggressive State.
    /// Is triggered after the enemy atacks.
    /// </summary>
    public event Action WeaponHit;

    #region Gizmos
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sizeOfAlert);
    }
    #endregion
}
