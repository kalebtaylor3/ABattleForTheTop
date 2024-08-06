using System.Collections;
using System.Collections.Generic;
using BFTT.Combat;
using UnityEngine;

public class HorseCard : AbstractCombat
{

    public RideHorse ridingAbility;

    public override bool CombatReadyToRun()
    {
        return _manager.currentCard == this;
    }

    public override void OnStartCombat()
    {
        
    }

    public override void OnStopCombat()
    {
        
    }

    public override bool ReadyToExit()
    {
        if (ridingAbility.ridingHorse)
            return false;

        return true;
    }

    public override void UpdateCombat()
    {
       
    }
}
