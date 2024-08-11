using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DragAndDrop
{

    // class which represents a C# array of objects of some kind
    public abstract class ObjectContainerArray : ObjectContainer
    {
        // we can rely on C#'s ability to cast an array of Objects to an array of any Object-derived class here.
        // so this is a reference to say an array of Items in an inventory, or array of Spells or whatever.
        protected UnityEngine.Object[] objects;

        // array of child UI objects which we take ownership of
        protected Slot[] slots;

        public override void Drop(Slot slot, ObjectContainer fromContainer)
        {
            // copy the slot's data back into the array we're working with.
            objects[slot.index] = slot.item.obj;
        }

        // Use this for initialization
        protected void CreateSlots(UnityEngine.Object[] array)
        {
            // hook up the appropriate array. This is a reference, so we're now writing to the player data if we change this
            objects = array;
            slots = new Slot[objects.Length];

            if (preMadeSlots.Length == 0)
                for (int i = 0; i < transform.childCount; i++)
                {
                    Destroy(transform.GetChild(i).gameObject);
                }

            // create a Slot for each object in the list, or use a premade one
            for (int i = 0; i < objects.Length; i++)
            {
                Slot premade = preMadeSlots != null && preMadeSlots.Length > i ? preMadeSlots[i] : null;
                slots[i] = MakeSlot(objects[i], premade);
                slots[i].index = i;
            }
        }

        // to be called from events
        public void HighlightSlots(bool on)
        {
            for (int i=0; i<slots.Length; i++)
            {
                if (on)
                    slots[i].OnDraggableEnter();
                else
                    slots[i].OnDraggableExit();
            }
        }
    }
}