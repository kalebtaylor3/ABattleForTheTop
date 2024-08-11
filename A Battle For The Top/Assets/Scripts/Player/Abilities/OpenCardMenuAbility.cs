using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BFTT.Abilities;
using BFTT.Components;
using BFTT.Controller;

public class OpenCardMenuAbility : AbstractAbility
{
    private RigidbodyMover _mover;
    bool menuOpen = false;
    public GameObject menu;
    public PlayerController _player;


    private void Awake()
    {
        _mover = GetComponent<RigidbodyMover>();
        menu.SetActive(false);
    }

    public override void OnStartAbility()
    {
        Debug.Log("Card Menu Opened");
        _mover.StopMovement();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        menu.SetActive(true);
        _player.canControl = false;
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
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        menu.SetActive(false);
        _player.canControl = true;
    }

    public override void UpdateAbility()
    {
        if (menuOpen && _action.OpenCardMenu)
            StopAbility();
    }
}
