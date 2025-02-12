﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Class responsible for handling player attack.
/// </summary>
public class PlayerMeleeAttack : MonoBehaviour, IAction
{
    [SerializeField] private LayerMask enemyLayers;
    [Range(-1, 1)][SerializeField] private float minDotProductToStealthKill;
    private float instantKillDistance;
    private Transform closestEnemyIcon;

    // Components
    private PlayerInputCustom input;
    public Animator Anim { get; private set; }
    private PlayerRoll roll;
    private PlayerUseItem useItem;
    private CinemachineTarget target;
    private PlayerStats stats;
    private PlayerBlock block;
    private PlayerInteract interact;
    private PlayerWallHug wallHug;
    private PlayerMovement movement;
    private SlowMotionBehaviour slowMotion;
    private PlayerValuesScriptableObj values;
    private Player player;
    private CinemachineTarget cineTarget;
    private Camera mainCam;
    private PlayerTakingHitAnimationBehaviour takingHit;

    // Weapon
    [SerializeField] private SphereCollider sword;

    // Trail
    [SerializeField] private ParticleSystem[] particles;

    // Layers
    [SerializeField] private LayerMask hittableLayers;

    // Swordhit
    [SerializeField] private GameObject swordHitPrefab;

    public bool Performing { get; set; }
    public bool InInstantKill { get; set; }

    private EnemySimple enemyToInstakill;
    private bool executeInstaKill;

    // Rotation
    private float smoothTimeRotation;
    private float turnSmooth;

    private void Awake()
    {
        input = FindObjectOfType<PlayerInputCustom>();
        Anim = GetComponent<Animator>();
        useItem = GetComponent<PlayerUseItem>();
        roll = GetComponent<PlayerRoll>();
        target = FindObjectOfType<CinemachineTarget>();
        stats = GetComponent<PlayerStats>();
        block = GetComponent<PlayerBlock>();
        interact = GetComponent<PlayerInteract>();
        wallHug = GetComponent<PlayerWallHug>();
        movement = GetComponent<PlayerMovement>();
        slowMotion = FindObjectOfType<SlowMotionBehaviour>();
        values = GetComponent<Player>().Values;
        player = GetComponent<Player>();
        cineTarget = FindObjectOfType<CinemachineTarget>();
        takingHit = GetComponent<Animator>().GetBehaviour<PlayerTakingHitAnimationBehaviour>();
        mainCam = Camera.main;
    }

    private void Start()
    {
        closestEnemyIcon = null;
        instantKillDistance = 2f;
        turnSmooth = values.TurnSmooth;
        executeInstaKill = false;

        foreach (ParticleSystem particle in particles)
            particle.Stop();
    }

    private void OnEnable()
    {
        input.MeleeLightAttack += MeleeLightAttack;
        slowMotion.SlowMotionEvent += ChangeTurnSmoothValue;
    }

    private void OnDisable()
    {
        input.MeleeLightAttack -= MeleeLightAttack;
        slowMotion.SlowMotionEvent -= ChangeTurnSmoothValue;
    }

    /// <summary>
    /// Controls enemy instant kill icon.
    /// This part of code MUST be in this script to be exactly the same
    /// as stealthkill method. If this code is in another script the values
    /// might not be 100% the same, which will cause some fails when the
    /// icon pops up.
    /// </summary>
    public void ComponentUpdate()
    {
        // Turns stealth kill icon canvas on if stealth kill is possible 
        if (movement.Walking)
        {
            // Checks if there is an enemy to insta kill
            // Checks all enemies around the player
            Collider[] enemies =
                    Physics.OverlapSphere(transform.position, instantKillDistance, enemyLayers);

            // Checks if it's an enemy
            if (enemies.Length > 0)
            {
                List<Transform> enemyList = new List<Transform>();

                // If enemy has an Enemy script
                for (int i = 0; i < enemies.Length; i++)
                {
                    if (enemies[i].gameObject.TryGetComponent(out EnemySimple allens))
                    {
                        enemyList.Add(enemies[i].transform);
                    }
                }

                float nearestEnemyDistance = float.MaxValue;
                float distance;
                // Orders enemies by distance to get the closest one
                foreach (Transform enemy in enemyList)
                {
                    distance = (enemy.transform.position - transform.position).sqrMagnitude;
                    if (distance < nearestEnemyDistance)
                    {
                        nearestEnemyDistance = distance;
                        closestEnemyIcon = enemy;
                    }
                }
            }
            else // else if the player left stealth kill range
            {
                if (closestEnemyIcon != null)
                {
                    GameObject instantKillSprite =
                    closestEnemyIcon.gameObject.GetComponentInChildren<EnemyInstantKillIcon>().
                    transform.GetChild(0).gameObject;

                    if (instantKillSprite.activeSelf == true)
                        instantKillSprite.SetActive(false);

                    closestEnemyIcon = null;
                }
            }

            // If player is close to the enemy
            if (closestEnemyIcon != null)
            {
                GameObject instantKillSprite =
                    closestEnemyIcon.gameObject.GetComponentInChildren<EnemyInstantKillIcon>().
                    transform.GetChild(0).gameObject;

                if (cineTarget.Targeting == false)
                {
                    // If the player is facing the enemy's forward, meaning it's
                    // looking towards him while he has is back turned
                    if (Vector3.Dot(closestEnemyIcon.transform.forward, transform.forward) >=
                        minDotProductToStealthKill)
                    {
                        // Only happens if the player is BEHIND the enemy, prevents
                        // from doing instant kill while the enemy is behind the player
                        if (Vector3.Angle(
                            transform.Direction(closestEnemyIcon.transform), transform.forward) < 20)
                        {
                            if (instantKillSprite.activeSelf == false)
                                instantKillSprite.SetActive(true);
                            return;
                        } // else if the player is in front of the enemy

                        if (instantKillSprite.activeSelf == true)
                            instantKillSprite.SetActive(false);
                        return;

                    } // else if player is not facing the enemy

                    if (instantKillSprite.activeSelf == true)
                        instantKillSprite.SetActive(false);
                    return;
                }
                else // if targetting is true
                {
                    // If camera is facing the enemy's forward, meaning it's
                    // looking towards him while he has is back turned
                    if (Vector3.Dot(closestEnemyIcon.transform.forward, mainCam.transform.forward) >=
                        minDotProductToStealthKill + 0.35f)
                    {
                        if (instantKillSprite.activeSelf == false)
                            instantKillSprite.SetActive(true);

                        return;
                    }
                    else
                    {
                        if (instantKillSprite.activeSelf == true)
                            instantKillSprite.SetActive(false);
                    }
                }
            }
        }
    }

    public void ComponentFixedUpdate()
    {
        if (Performing && target.Targeting == false && 
            input.Movement.magnitude > 0 && InInstantKill == false)
        {
            // Rotates towards player's pressing direction
            Vector3 movement = new Vector3(input.Movement.x, 0, input.Movement.y);
            float targetAngle = Mathf.Atan2(movement.x, movement.z) *
                Mathf.Rad2Deg + Camera.main.transform.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(
                transform.eulerAngles.y,
                targetAngle,
                ref smoothTimeRotation,
                turnSmooth);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
        }
    }

    /// <summary>
    /// Changes turn smooth value after slow motion.
    /// </summary>
    private void ChangeTurnSmoothValue(SlowMotionEnum condition)
    {
        // Changes turn value on player and changes camera update mode
        if (condition == SlowMotionEnum.SlowMotion)
            turnSmooth = values.TurnSmoothInSlowMotion;
        else
            turnSmooth = values.TurnSmooth;
    }

    /// <summary>
    /// Handles light attack.
    /// Attacks if the player is not rolling, not using an item, not blocking
    /// and not near an interectable object.
    /// </summary>
    private void MeleeLightAttack()
    {
        if (roll.Performing == false && useItem.Performing == false &&
            block.Performing == false && interact.InterectableObject == null &&
            wallHug.Performing == false && takingHit.Performing == false)
        {
            if (target.Targeting)
            {
                transform.RotateTo(target.CurrentTarget.position);
            }

            OnMeleeAttack();

            if (player.InTutorial)
                OnTutorialAttack(TypeOfTutorial.Attack);
        }
    }

    /// <summary>
    /// Turns trail to false. Runs on Animation event.
    /// </summary>
    private void TurnTrailToTrue()
    {
        foreach (ParticleSystem particle in particles)
            particle.Play();
    }

    /// <summary>
    /// Turns trail to false. Runs on Animation event.
    /// </summary>
    public void TurnTrailToFalse()
    {
        foreach (ParticleSystem particle in particles)
            particle.Stop();
    }

    /// <summary>
    /// Checks attack collision through Animation event.
    /// </summary>
    public void CheckLightAttackCollision()
    {
        Collider[] swordCol = 
            Physics.OverlapSphere(
                sword.transform.position + sword.center, 
                sword.radius, 
                hittableLayers);

        // Checks if this object or parent has a damageable body
        GameObject body = null;
        if (swordCol.Length > 0)
            body = GetDamageableBody(swordCol[0].gameObject);

        // If this object can receive damage
        if (body != null)
        {
            if (body.TryGetComponent(out IDamageable damageableBody))
            {
                // If player performed instantkill, it instakills enemy and sets variables to default
                if (executeInstaKill)
                {
                    if (player.InTutorial)
                        OnTutorialInstantKill(TypeOfTutorial.InstantKill);

                    enemyToInstakill.OnInstanteDeath();
                    enemyToInstakill = null;
                    executeInstaKill = false;
                }

                damageableBody?.TakeDamage(stats.LightDamage, TypeOfDamage.PlayerMelee);
            }
            else if (body.TryGetComponent(out IBreakable breakable))
            {
                breakable?.Execute();
            }

            Instantiate(
                swordHitPrefab, 
                sword.transform.position, 
                Quaternion.identity);
        }
    }

    /// <summary>
    /// Checks if this object has a damgeable body, if it's null, it it will check
    /// its parent until parent is null.
    /// </summary>
    /// <param name="col">Gameobject to check.</param>
    /// <returns>Returns a gameobject with IDamageable interface.</returns>
    private GameObject GetDamageableBody(GameObject col)
    {
        col.TryGetComponent(out IDamageable damageableBody);
        col.TryGetComponent(out IBreakable breakable);
        if (damageableBody != null || breakable != null) return col.gameObject;

        else if (col.gameObject.transform.parent != null)
        {
            GetDamageableBody(col.transform.parent.gameObject);
        }
        return null;
    }

    /// <summary>
    /// Triggers light melee attack. Normal or instant kill Animation
    /// depending on the condition.
    /// Happens as soon as the player pressed the attack key.
    /// </summary>
    protected virtual void OnMeleeAttack()
    {
        if (movement.Walking == false)
        {
            // Normal attack Anim
            OnLightMeleeAttack(true);
            return;
        }
        // ELSE
        // happens if the player is walking or an enemy is in blindness state

        // Checks all enemies around the player
        Collider[] enemies =
                Physics.OverlapSphere(transform.position, instantKillDistance, enemyLayers);

        Transform enemyToAttack = null;
        // If there are no enemies
        if (enemies.Length == 0)
        {
            // Normal attack Anim
            OnLightMeleeAttack(true);
            return;
        }
        else // else if there are enemies
        {
            // Creates a list for all enemies around the player
            List<Transform> allEnemies = new List<Transform>();

            // If enemy has an Enemy script
            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i].gameObject.TryGetComponent(out EnemyBase allens))
                {
                    allEnemies.Add(enemies[i].transform);
                }
            }

            // Orders array with all VISIBLE enemies by distance
            enemyToAttack =
                allEnemies.OrderBy(i => (i.position - transform.position).magnitude).First();

            enemyToInstakill = enemyToAttack.GetComponent<EnemySimple>();
        }

        // If there is any enemy to attack
        Vector3 dir = transform.Direction(enemyToAttack);

        // If the player is facing the enemy's forward, meaning it's
        // looking towards him while he has is back turned
        if (Vector3.Dot(enemyToAttack.forward, transform.forward) >= 
            minDotProductToStealthKill)
        {
            // Only happens if the player is BEHIND the enemy, prevents
            // from doing instant kill while the enemy is behind the player
            if (Vector3.Angle(dir, transform.forward) < 20)
            {
                executeInstaKill = true;

                // Instant kill Anim.
                OnLightMeleeAttack(false);
                return;
            }
            // Else
            // Normal attack Anim
            OnLightMeleeAttack(true);
            return;
        }
        // Else
        // Normal attack Anim
        OnLightMeleeAttack(true);
    }

    protected virtual void OnLightMeleeAttack(bool condition) =>
        LightMeleeAttack?.Invoke(condition);

    /// <summary>
    /// Event registered on PlayerAnimations.
    /// Event registered on PlayerMovement.
    /// Triggers light melee attack. Normal or instant kill Animation
    /// depending on the condition.
    public event Action<bool> LightMeleeAttack;

    ///////////////////// Tutorial methods and events //////////////////////////
    protected virtual void OnTutorialInstantKill(TypeOfTutorial typeOfTutorial) =>
        TutorialInstantKill?.Invoke(typeOfTutorial);

    protected virtual void OnTutorialAttack(TypeOfTutorial typeOfTutorial) =>
        TutorialAttack?.Invoke(typeOfTutorial);

    public event Action<TypeOfTutorial> TutorialInstantKill;
    public event Action<TypeOfTutorial> TutorialAttack;
    ////////////////////////////////////////////////////////////////////////////

    #region Gizmos
    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(sword.transform.position + sword.center,
                sword.radius);
    }
    #endregion
}
