using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BFTT.Abilities;
using BFTT.Components;

public class OpenCardMenuAbility : AbstractAbility
{
    private RigidbodyMover _mover;
    bool menuOpen = false;


    private void Awake()
    {
        _mover = GetComponent<RigidbodyMover>();    
    }

    public override void OnStartAbility()
    {
        Debug.Log("Card Menu Opened");
        _mover.StopMovement();
    }

    public override bool ReadyToRun()
    {
        if (_action.OpenCardMenu)
        {
            menuOpen = true;
            return true;
        }

        return false;
    }

    public override void OnStopAbility()
    {
        Debug.Log("menu closed");
    }

    public override void UpdateAbility()
    {
        if (menuOpen && _action.OpenCardMenu)
            StopAbility();
    }
}
