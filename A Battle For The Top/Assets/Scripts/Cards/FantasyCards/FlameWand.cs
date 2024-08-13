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
    public float iceDetectionRange = 2.0f;

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
            //CancelInvoke(nameof(DecreaseDurability));
        }
    }

    public override void UpdateCombat()
    {
        Debug.Log("Fire is going down");

        if (wandDurability > 0)
        {
            wandDurability -= durabilityDecreaseAmount * Time.unscaledDeltaTime;
        }
        //InvokeRepeating(nameof(DecreaseDurability), 0f, decreaseInterval);
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
            wandDurability -= durabilityDecreaseAmount * Time.unscaledDeltaTime;
        }
    }

    private void HandleIK()
    {
        Vector3 direction = transform.forward;

        // Determine the default target position in front of the player, including the offset
        Vector3 targetPosition = transform.position + transform.TransformDirection(wandOffset);
        Quaternion targetRotation = Quaternion.LookRotation(direction); // Default rotation

        // Check for objects with the tag "Ice" within the detection range
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, iceDetectionRange);
        foreach (Collider hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Ice"))
            {
                // Set the target position to the position of the object with the "Ice" tag
                targetPosition = hitCollider.transform.position;
                // Calculate the direction with a downward bias
                direction = (targetPosition - transform.position).normalized + Vector3.down * 0.6f;
                targetRotation = Quaternion.LookRotation(direction);
                break;
            }
        }

        if (_ikScheduler != null)
        {
            // Apply IK to the right hand with the updated position and rotation
            IKPass rightHandPass = new IKPass(targetPosition, targetRotation, AvatarIKGoal.RightHand, 1, 1);
            _ikScheduler.ApplyIK(rightHandPass);
        }
    }

    public override bool ReadyToExit()
    {
        if(!_action.UseCardHold) return true;
        else return false;
    }
}
