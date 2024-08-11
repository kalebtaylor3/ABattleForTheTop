using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DragAndDrop;

// example drag and drop container using custom data layout instead of an array
public class EquipScreen : ObjectContainer
{
    public Player player;

    public Slot helmet;
    public Slot amulet;
    public Slot ring1;
    public Slot ring2;
    public Slot gloves;
    public Slot boots;
    public Slot armour;

    [HideInInspector]
    public Slot[] slots;

    void Start()
    {
        slots = new Slot[7];

        // base these on existing slots set up in UI editor
        // we grab the return value so you could not specify values in the editor and rely on a layout group instead
        //slots[0] = helmet = MakeSlot(player.helmet, helmet);
        //slots[1] = amulet = MakeSlot(player.amulet, amulet);
        //slots[2] = ring1 = MakeSlot(player.ring1, ring1);
        //slots[3] = ring2 = MakeSlot(player.ring2, ring2);
        //slots[4] = armour = MakeSlot(player.armour, armour);
        //slots[5] = gloves = MakeSlot(player.gloves, gloves);
        //slots[6] = boots = MakeSlot(player.boots, boots);
    }

    //// we can drop anything in the right slot
    //public override bool CanDrop(Draggable dragged, Slot slot)
    //{
    //    Charm charm = dragged.obj as Charm;

    //    // if we're dropping into empty space?
    //    if (charm == null || slot == null)
    //        return true;

    //    // player must meet level requirements
    //    if (charm.level > player.level)
    //        return false;

    //    // only let the right type of item be dropped in this slot
    //    return (charm.charmType == Charm.CharmType.Helmet && slot == helmet)
    //        || (charm.charmType == Charm.CharmType.Amulet && slot == amulet)
    //        || (charm.charmType == Charm.CharmType.Ring && (slot == ring1 || slot == ring2))
    //        || (charm.charmType == Charm.CharmType.Gloves && slot == gloves)
    //        || (charm.charmType == Charm.CharmType.Boots && slot == boots)
    //        || (charm.charmType == Charm.CharmType.Armour && slot == armour);
    //}

    public override void Drop(Slot slot, ObjectContainer fromContainer)
    {
        // write info back into player's data
        //player.amulet = amulet.item.obj as Card;
        //player.helmet = helmet.item.obj as Card;
        //player.gloves = gloves.item.obj as Card;
        //player.boots = boots.item.obj as Card;
        //player.ring1 = ring1.item.obj as Card;
        //player.ring2 = ring2.item.obj as Card;
        //player.armour = armour.item.obj as Card;
    }

    // to be called from events
    public void HighlightSlots(bool on)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (on)
                slots[i].OnDraggableEnter();
            else
                slots[i].OnDraggableExit();
        }
    }

}
