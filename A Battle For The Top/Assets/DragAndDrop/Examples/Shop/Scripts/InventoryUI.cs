using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DragAndDrop;

namespace DragAndDropShop
{
    public class InventoryUI : ObjectContainerArray
    {
        public Inventory inventory;

        // Start is called before the first frame update
        void Start()
        {
            CreateSlots(inventory.items);
        }

        public override void Drop(Slot slot, ObjectContainer fromContainer)
        {
            Item item = slot.item.obj as Item;

            if (item)
            {
                if (inventory.isPlayer)
                    inventory.gold -= item.cost;

                // if we're taking it out, refund them!
                InventoryUI fromInventoryUI = fromContainer as InventoryUI;
                if (fromInventoryUI && fromInventoryUI.inventory.isPlayer)
                    fromInventoryUI.inventory.gold += item.cost;
            }

            base.Drop(slot, fromContainer);
        }

        public override bool CanDrop(Draggable dragged, Slot slot)
        {
            // can always shuffle around inside ourselves.
            if (dragged.slot.container == this)
                return true;

            // if this is the player inventory, make sure we have enough money to drop the item in there.
            if (inventory.isPlayer)
            {
                // work out the total cost of this transaction
                int cost = 0;
                Item item = dragged.obj as Item;
                if (item)
                    cost += item.cost;

                Item itemOut = slot.item.obj as Item;
                if (itemOut)
                    cost -= itemOut.cost;

                return cost <= inventory.gold;
            }

            return true;
        }


    }
}
