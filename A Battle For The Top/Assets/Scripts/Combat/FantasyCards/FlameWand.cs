using BFTT.Combat;
using BFTT.IK;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlameWand : AbstractCombat
{
    public static event Action<AbstractCombat> OnWandBreak;
    public float wandDurability = 100;
    public ParticleSystem flame;
    public float durabilityDecreaseAmount = 1f; // Amount to decrease per interval
    public float decreaseInterval = 0.1f; // Interval in seconds

    public Vector3 wandOffset = Vector3.zero; // Public offset for the wand

    private bool isDecreasingDurability = false;
    private IKScheduler _ikScheduler;

    private void Awake()
    {
        _ikScheduler = GetComponent<IKScheduler>();
    }

    public override bool CombatReadyToRun()
    {
        if (_manager.currentCard == this && _manager.currentCard != null)
            HandleIK();
            

        if (_manager.currentCard == this && _action.UseCardHold && wandDurability > 0)
            return true;
        return false;
    }

    public override void OnStartCombat()
    {
        Debug.Log("Started Flame");
        flame.Play();
        if (!isDecreasingDurability)
        {
            isDecreasingDurability = true;
        }
    }

    public override void OnStopCombat()
    {
        Debug.Log("Stopped Flame");
        flame.Stop();
        if (_ikScheduler != null)
        {
            _ikScheduler.StopIK(AvatarIKGoal.RightHand);
        }
        if (isDecreasingDurability)
        {
            isDecreasingDurability = false;
            CancelInvoke(nameof(DecreaseDurability));
        }
    }

    public override void UpdateCombat()
    {
        Debug.Log("Fire is going down");
        InvokeRepeating(nameof(DecreaseDurability), 0f, decreaseInterval);
        FaceCenterOfScreen();
        HandleIK();

        if (wandDurability <= 0)
        {
            Debug.Log("Wand broke");
            OnWandBreak?.Invoke(this);
            abilityProp.SetActive(false);
            StopCombat();
        }

        if (!_action.UseCardHold)
            StopCombat();
    }

    private void DecreaseDurability()
    {
        if (wandDurability > 0)
        {
            wandDurability -= durabilityDecreaseAmount;
        }
    }

    private void FaceCenterOfScreen()
    {
        // Get the screen center point
        Vector3 screenCenter = new Vector3(Screen.width / 2, Screen.height / 2, 0);

        // Convert screen center point to world point
        Ray ray = Camera.main.ScreenPointToRay(screenCenter);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Get the direction to the hit point
            Vector3 direction = (hit.point - flame.transform.position).normalized;

            // Rotate the flame emitter to face the direction
            flame.transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    private void HandleIK()
    {
        Vector3 screenCenter = new Vector3(Screen.width / 2, Screen.height / 2, 0);
        Ray ray = Camera.main.ScreenPointToRay(screenCenter);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Get the direction to the hit point
            Vector3 direction = (hit.point - transform.position).normalized;

            // Determine the target position in front of the player, including the offset
            Vector3 targetPosition = transform.position + direction * 1.0f + wandOffset;

            if (_ikScheduler != null)
            {
                // Create a rotation that faces the direction
                Quaternion targetRotation = Quaternion.LookRotation(direction);

                // Apply IK to the right hand
                IKPass rightHandPass = new IKPass(targetPosition, targetRotation, AvatarIKGoal.RightHand, 1, 1);
                _ikScheduler.ApplyIK(rightHandPass);
            }
        }
    }

    public override bool ReadyToExit()
    {
        if(!_action.UseCardHold) return true;
        else return false;
    }
}
