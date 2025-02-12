﻿/// <summary>
/// Class responsible for what happens when the player picks a FirebombKunai.
/// </summary>
public class FirebombKunaiPickable : Pickable
{
    /// <summary>
    /// What happens when the player collides with this item.
    /// </summary>
    /// <param name="playerStats">Player stats variable.</param>
    public override void Execute(PlayerStats playerStats)
    {
        base.Execute(playerStats);
        playerStats.FirebombKunais += quantity;
    }
}
