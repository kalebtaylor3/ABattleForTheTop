using BFTT.Combat;
using BFTT.Abilities;
using UnityEngine;

public class BallRollCard : AbstractCombat
{
    public BallRollAbility ballRollAbility;
    [HideInInspector] public bool activated = false;

    public override bool CombatReadyToRun()
    {
        // The combat is ready to run if this card is the current card
        return _manager.currentCard == this && _action.UseCard;
    }

    public override void OnStartCombat()
    {
        // Activate the ball rolling ability
        activated = !activated;

        if (!activated)
            ballRollAbility.StopAbility(); 
        Debug.Log("activated");
    }

    public override void OnStopCombat()
    {
        
    }

    public override bool ReadyToExit()
    {
        // The combat can exit if the ball is not currently rolling
        return true;
    }

    public override void UpdateCombat()
    {
        StopCombat();
    }
}
