using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BFTT.Abilities;

public class LassoAbility : AbstractAbility
{
    public Lasso _lasso;

    public override void OnStartAbility()
    {
        //
    }

    public override bool ReadyToRun()
    {
        //
        if (_lasso.GetIsRopeOut())
            return true;
        return false;
    }

    public override void UpdateAbility()
    {
        //

        if (!_lasso.GetIsRopeOut())
            StopAbility();
    }

    public override void OnStopAbility()
    {
        base.OnStopAbility();
    }
}
