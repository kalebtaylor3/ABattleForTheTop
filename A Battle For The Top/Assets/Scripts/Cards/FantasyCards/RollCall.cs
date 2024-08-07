using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BFTT.Combat;

public class RollCall : AbstractCombat
{
    public override bool CombatReadyToRun()
    {
        if (_manager.currentCard == this)
            return true;

        return false;
    }

    public override void OnStartCombat()
    {
        
    }

    public override void OnStopCombat()
    {
        
    }

    public override bool ReadyToExit()
    {
        return true;
    }

    public override void UpdateCombat()
    {
        StopCombat();
    }
}
