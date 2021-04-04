﻿using UnityEngine;

/// <summary>
/// Abstract Scriptable object responsible for controlling enemy states with vision.
/// </summary>
public abstract class EnemyStateWithVision : EnemyState
{
    // Vision
    [Header("Vision Cone Attributes")]
    [SerializeField] private float coneRange;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask collisionLayers;
    protected float lastTimeChecked;

    // Targets
    protected Transform playerTarget;
    protected Transform myTarget;

    /// <summary>
    /// Method that defines what happens when this state is initialized.
    /// </summary>
    /// <param name="enemy">Enemy to get variables from.</param>
    public override void Initialize(Enemy enemy)
    {
        myTarget = enemy.MyTarget;
        playerTarget = enemy.PlayerTarget;
    }

    /// <summary>
    /// Search for player every searchCheckDelay seconds inside a cone vision.
    /// </summary>
    /// <returns>True if player is inside a vision cone.</returns>
    protected bool PlayerInRange()
    {
        bool playerFound = false;

        Collider[] playerCollider =
            Physics.OverlapSphere(myTarget.position, coneRange, playerLayer);

        // If player is in this collider
        if (playerCollider.Length > 0)
        {
            if (playerTarget != null)
            {
                Vector3 direction = playerTarget.position - myTarget.position;
                Ray rayToPlayer = new Ray(myTarget.position, direction);

                // If player is in the cone range
                if (Vector3.Angle(direction, myTarget.forward) < 45)
                {
                    if (Physics.Raycast(
                        rayToPlayer,
                        out RaycastHit hit,
                        coneRange,
                        collisionLayers))
                    {
                        // If it's player layer
                        if (hit.collider.gameObject.layer == 11)
                        {
                            playerFound = true;
                        }
                        else
                        {
                            playerFound = false;
                        }
                    }
                }
                else
                {
                    playerFound = false;
                }
            }
        }

        lastTimeChecked = Time.time;

        return playerFound;
    }
}