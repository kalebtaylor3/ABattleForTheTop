using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BFTT.Abilities;

public class LassoAbility : AbstractAbility
{
    public Lasso _lasso;

    public override void OnStartAbility()
    {
        // Initialize any necessary components or states here
    }

    public override bool ReadyToRun()
    {
        // This runs if we are swinging with the lasso
        if (_lasso.GetIsSwining())
            return true;
        return false;
    }

    public override void UpdateAbility()
    {
        // Handle any updates necessary while swinging
        if (!_lasso.GetIsSwining())
            StopAbility();
    }

    public override void OnStopAbility()
    {
        base.OnStopAbility();
        // Handle cleanup after stopping the ability
    }
}
