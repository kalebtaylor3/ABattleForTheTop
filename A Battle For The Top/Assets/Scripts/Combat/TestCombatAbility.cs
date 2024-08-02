using BFTT.Combat;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCombatAbility : AbstractCombat
{
    public override bool CombatReadyToRun()
    {
        if(_manager.currentCard == this)
            return true;
       
        return false;
    }

    public override void OnStartCombat()
    {
        Debug.Log("next ability started");
    }

    public override void OnStopCombat()
    {
        Debug.Log("next ability started");
    }

    public override void UpdateCombat()
    {
        Debug.Log("doing next ability");
    }
}
