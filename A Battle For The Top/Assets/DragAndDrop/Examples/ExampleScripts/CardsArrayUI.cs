using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DragAndDrop;
using BFTT.Combat;

public class CardsArrayUI : ObjectContainerArray {

    // where the data comes from - this represents the player, and either their backpack or belt
    public CardManager deckManager;
    public CardManager.CardList cardList;

    public AbstractCombat.CardType charmType = AbstractCombat.CardType.All;
    public Text description;

    void Start()
    {
        CreateSlots(deckManager.GetCards(cardList));
    }

    public void UpdateUI()
    {
        clearSlots();
        CreateSlots(deckManager.GetCards(cardList));
    }


    public override void Drop(Slot slot, ObjectContainer fromContainer)
    {
        base.Drop(slot, fromContainer);
        deckManager.UpdateCardUI();
    }

    // to be able to drop, it must be a charm of the appropriate type
    public override bool CanDrop(Draggable dragged, Slot slot)
    {
        CharmUI charm = dragged as CharmUI;

        // must be a charm
        if (charm == null)
            return false;

        AbstractCombat ch = charm.obj as AbstractCombat;

        // replacing empty slots in here is OK
        if (ch == null)
            return true;

        // check the type of items in the belt vs what we can hold
        int mask = (int)ch.cardType & (int)charmType;
        return mask != 0;
    }

    public override void OnDragBegin()
    {
        // when we start to drag an object, show its name and requirements in the text field provided
        base.OnDragBegin();
        if (description)
        {
            CharmUI charmUi = Draggable.current as CharmUI;
            if (charmUi)
            {
                AbstractCombat charm = charmUi.obj as AbstractCombat;
                if (charm)
                    description.text = charm.GetDescription();
            }
        }
    }

    public override void OnDraggableExit()
    {
        base.OnDraggableExit();
    }
}
