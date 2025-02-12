﻿using System.Collections.Generic;
using UnityEngine;

public class SmokeGrenade : ItemBehaviour
{
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private GameObject smokePrefab;
    [SerializeField] private float smokeRange;

    public override void Execute()
    {
        Vector3 spawnPosition = new Vector3
            (playerStats.transform.position.x,
            playerStats.transform.position.y + 2,
            playerStats.transform.position.z) + playerStats.transform.forward;

        Instantiate(smokePrefab, spawnPosition, Quaternion.identity);
        
        // Gets enemies around the grenade
        Collider[] collisions =
                Physics.OverlapSphere(transform.position, smokeRange, enemyLayer);

        HashSet<EnemySimple> enemiesToBlind = new HashSet<EnemySimple>();

        foreach (Collider col in collisions)
        {
            // Only applies if it's an an enemy
            if (col.gameObject.TryGetComponent(out EnemySimple en))
            {
                enemiesToBlind.Add(en);                
            }
        }

        foreach (EnemySimple enemy in enemiesToBlind)
            enemy.BlindEnemy();

        playerStats.SmokeGrenades--;
        base.Execute();
    }
}
