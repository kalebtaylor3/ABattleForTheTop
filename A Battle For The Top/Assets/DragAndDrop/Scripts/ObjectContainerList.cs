using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DragAndDrop
{
    public abstract class ObjectContainerList<T> : ObjectContainer where T : UnityEngine.Object
    {
        // Start is called before the first frame update
        protected List<T> objects;
        protected Slot[] slots;

        public override void Drop(Slot slot, ObjectContainer fromContainer)
        {
            // copy the slot's data back into the list we're working with.
            objects[slot.index] = slot.item.obj as T;
        }

        // Use this for initialization
        protected void CreateSlots(List<T> list)
        {
            // hook up the appropriate array. This is a reference, so we're now writing to the player data if we change this
            objects = list;
            slots = new Slot[objects.Count];

            if (preMadeSlots.Length == 0)
                for (int i = 0; i < transform.childCount; i++)
                {
                    Destroy(transform.GetChild(i).gameObject);
                }

            // create a Slot for each object in the list, or use a premade one
            for (int i = 0; i < objects.Count; i++)
            {
                Slot premade = preMadeSlots != null && preMadeSlots.Length > i ? preMadeSlots[i] : null;
                slots[i] = MakeSlot(objects[i], premade);
                slots[i].index = i;
            }
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
}