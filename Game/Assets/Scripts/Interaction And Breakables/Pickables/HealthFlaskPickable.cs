﻿/// <summary>
/// Class responsible for what happens when the player picks a HealthFlask.
/// </summary>
public class HealthFlaskPickable : Pickable
{
    /// <summary>
    /// What happens when the player collides with this item.
    /// </summary>
    /// <param name="playerStats">Player stats variable.</param>
    public override void Execute(PlayerStats playerStats)
    {
        int quantity = 0;
        switch (TypeOfDrop)
        {
            case TypeOfDropEnum.Treasure:
                quantity = rand.Next(2, 4); // between 2,3
                break;
            case TypeOfDropEnum.Lootbox:
                quantity = rand.Next(1, 3); // between 1,2
                break;
        }
        playerStats.HealthFlasks += quantity;
    }
}