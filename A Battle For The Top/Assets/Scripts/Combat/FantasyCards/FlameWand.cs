using BFTT.Combat;
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

    private bool isDecreasingDurability = false;

    public override bool CombatReadyToRun()
    {
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
}
