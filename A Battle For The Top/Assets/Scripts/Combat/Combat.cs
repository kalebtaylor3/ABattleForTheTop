using BFTT.Combat;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Combat : AbstractCombat
{
    public override bool CombatReadyToRun()
    {
        return false;
    }

    public override void OnStartCombat()
    {
        Debug.Log("combat action triggered");
    }

    public override void OnStopCombat()
    {
        throw new System.NotImplementedException();
    }

    public override void UpdateCombat()
    {
        Debug.Log("combatting");
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
