using BFTT.Combat;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlameWand : AbstractCombat
{
    public override bool CombatReadyToRun()
    {
        if (_action.jump)
            return true;
        return false;

    }

    public override void OnStartCombat()
    {
        Debug.Log("shot");
    }

    public override void OnStopCombat()
    {
        Debug.Log("stopped");
    }

    public override void UpdateCombat()
    {
        if (!_action.jump)
            StopCombat();
    }

    
}
