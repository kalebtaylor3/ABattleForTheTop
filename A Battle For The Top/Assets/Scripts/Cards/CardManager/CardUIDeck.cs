using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DragAndDrop;
using BFTT.Combat;

using System;

// specialisation of Draggable that displays charms
public class CardUIDeck : Draggable, IToolTip
{
    public Image image;

    public string getToolTipMessage()
    {
        AbstractCombat charm = obj as AbstractCombat;
        return (charm == null) ? "" : charm.GetDescription();
    }

    public override void UpdateObject()
    {
        AbstractCombat charm = obj as AbstractCombat;

        if (charm == null && obj != null)
        {
            Debug.LogWarning("Trying to place something that isn't a Charm into a CharmUI!");
        }

        // set the visible data
        if (charm)
        {
            image.sprite = charm.cardIcon;
        }

        // turn off if it was null
        gameObject.SetActive(charm != null);
    }
}
