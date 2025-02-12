﻿using UnityEngine;
using System;

/// <summary>
/// Class responsible for handling usage of items.
/// </summary>
public class PlayerUseItem : MonoBehaviour, IAction
{
    [Header("Transforms with item positions")]
    [SerializeField] private Transform kunaiItemPosition;
    [SerializeField] private Transform leftHandItemPosition;
    [SerializeField] private Transform leftHand;
    private GameObject leftHandSpawnedItem;

    [Range(0, 3)][SerializeField] private int delayAfterUse;
    public int Delay => delayAfterUse;

    // Components
    private ItemControl itemControl;
    private PlayerInputCustom input;
    private PlayerAnimations playerAnims;
    private PlayerMeleeAttack attack;
    private PlayerRoll roll;
    private Animator anim;
    private PlayerMovement movement;
    private CinemachineTarget target;
    private PlayerStats stats;
    private PlayerBlock block;
    private PlayerWallHug wallHug;
    private Player player;
    private PlayerTakingHitAnimationBehaviour takingHit;

    public float TimeItemWasUsed { get; private set; }
    private bool canUseItemDelayOver;

    public bool Performing { get; private set; }

    private void Awake()
    {
        itemControl = FindObjectOfType<ItemControl>();
        input = FindObjectOfType<PlayerInputCustom>();
        playerAnims = GetComponent<PlayerAnimations>();
        attack = GetComponent<PlayerMeleeAttack>();
        roll = GetComponent<PlayerRoll>();
        anim = GetComponent<Animator>();
        movement = GetComponent<PlayerMovement>();
        target = FindObjectOfType<CinemachineTarget>();
        stats = GetComponent<PlayerStats>();
        block = GetComponent<PlayerBlock>();
        wallHug = GetComponent<PlayerWallHug>();
        player = GetComponent<Player>();
        takingHit = GetComponent<Animator>().GetBehaviour<PlayerTakingHitAnimationBehaviour>();
    }

    private void Start()
    {
        TimeItemWasUsed = 0f;
        canUseItemDelayOver = true;
    }

    private void OnEnable() =>
        input.ItemUse += HandleItemUse;

    private void OnDisable() =>
        input.ItemUse -= HandleItemUse;

    /// <summary>
    /// Rotates the character towards something.
    /// </summary>
    private void RotationBeforeItemUse()
    {
        if (target.Targeting)
        {
            transform.RotateTo(target.CurrentTarget.position);
        }

        // If the player is pressing any direction
        // rotates the character instantly in that direction
        else if (target.Targeting == false)
        {
            if (movement.Direction != Vector3.zero)
            {
                // Finds angle
                float targetAngle = Mathf.Atan2(movement.Direction.x, movement.Direction.z) *
                        Mathf.Rad2Deg + Camera.main.transform.eulerAngles.y;

                // Rotates to that angle
                transform.rotation = Quaternion.Euler(0f, targetAngle, 0f);
            }
        }
    }

    /// <summary>
    /// Uses the item and starts it's delay.
    /// THis method also rotates the player towards a target or towards the
    /// direction the player is moving.
    /// </summary>
    private void HandleItemUse()
    {
        if (canUseItemDelayOver && attack.Performing == false && 
            roll.Performing == false && block.Performing == false && 
            wallHug.Performing == false && takingHit.Performing == false)
        {
            if (player.InTutorial)
                OnTutorialItemUse(TypeOfTutorial.ItemUse);

            // Plays an animation depending on the item used
            switch (itemControl.CurrentItem.ItemType)
            {
                case ListOfItems.Kunai:
                    if (stats.Kunais > 0)
                    {
                        PerformingItemUseToTrue();
                        playerAnims.TriggerKunaiAnimation();
                        RotationBeforeItemUse();
                        TimeItemWasUsed = Time.time;
                        canUseItemDelayOver = false;
                        OnUsedItemDelay();
                        movement.Walking = false;
                    }
                    break;
                case ListOfItems.FirebombKunai:
                    if (stats.FirebombKunais > 0)
                    {
                        PerformingItemUseToTrue();
                        playerAnims.TriggerKunaiAnimation();
                        RotationBeforeItemUse();
                        TimeItemWasUsed = Time.time;
                        canUseItemDelayOver = false;
                        OnUsedItemDelay();
                        movement.Walking = false;
                    }
                    break;
                case ListOfItems.HealthFlask:
                    if (stats.HealthFlasks > 0)
                    {
                        PerformingItemUseToTrue();
                        playerAnims.TriggerHealthFlaskAnimation();
                        RotationBeforeItemUse();
                        TimeItemWasUsed = Time.time;
                        canUseItemDelayOver = false;
                        OnUsedItemDelay();
                        movement.Walking = false;
                    }
                    break;
                case ListOfItems.SmokeGrenade:
                    if (stats.SmokeGrenades > 0)
                    {
                        PerformingItemUseToTrue();
                        playerAnims.TriggerSmokeGrenadeAnimation();
                        RotationBeforeItemUse();
                        TimeItemWasUsed = Time.time;
                        canUseItemDelayOver = false;
                        OnUsedItemDelay();
                        movement.Walking = false;
                    }
                    break;           
            }
        }
    }

    protected virtual void OnUsedItemDelay() => UsedItemDelay?.Invoke();

    /// <summary>
    /// Event registered on ItemUIParent.
    /// Event registered on PlayerMovement.
    /// </summary>
    public event Action UsedItemDelay;

    /// <summary>
    /// Called on animation event. Throws a normal kunai or firebomb kunai,
    /// depending on the current item selected
    /// </summary>
    private void AnimationEventThrowKunai()
    {
        Instantiate(itemControl.CurrentItemObject, kunaiItemPosition.position, Quaternion.identity);
    }

    /// <summary>
    /// Called on animation event.
    /// </summary>
    private void AnimationEventSpawnItemLeftHand()
    {
        leftHandSpawnedItem = 
            Instantiate(itemControl.CurrentItemObject, 
            leftHandItemPosition.position, 
            leftHand.rotation);
    }

    /// <summary>
    /// Called on animation event.
    /// </summary>
    private void AnimationEventExecuteItemLeftHand() =>
        leftHandSpawnedItem.GetComponent<IUsableItem>().Execute();

    /// <summary>
    /// Called on animation event.
    /// </summary>
    private void AnimationEventDestroyItemLeftHand() =>
        Destroy(leftHandSpawnedItem);

    /// <summary>
    /// Called on item use animation events.
    /// </summary>
    private void PerformingItemUseToTrue()
    {
        anim.applyRootMotion = true;
        Performing = true;
    }

    /// <summary>
    /// Called on item use animation events.
    /// </summary>
    private void PerformingItemUseToFalse()
    {
        anim.applyRootMotion = false;
        Performing = false;
    }

    public void ComponentUpdate()
    {
        // Only possible to use an item after delay is over
        if (Time.time - TimeItemWasUsed > delayAfterUse) canUseItemDelayOver = true;
    }

    public void ComponentFixedUpdate()
    {
        if (leftHandSpawnedItem)
        {
            leftHandSpawnedItem.transform.position = leftHandItemPosition.position;
            leftHandSpawnedItem.transform.rotation = leftHand.rotation;
        }
    }


    ///////////////////// Tutorial methods and events //////////////////////////
    protected virtual void OnTutorialItemUse(TypeOfTutorial typeOfTut) =>
        TutorialItemUse?.Invoke(typeOfTut);

    public event Action<TypeOfTutorial> TutorialItemUse;
    ////////////////////////////////////////////////////////////////////////////
}
