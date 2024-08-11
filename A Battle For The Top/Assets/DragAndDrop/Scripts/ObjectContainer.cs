using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using BFTT.Combat;

namespace DragAndDrop
{
    // base class for a UI front end that creates a slot object and child item for each entry
    // marked abstract because it's too general to do anything by itself yet.
    public abstract class ObjectContainer : MonoBehaviour
    {
        // prefab for slot UI element
        public Slot slotPrefab;

        // prefab for the item UI front ends
        public Draggable itemPrefab;

        public Slot[] preMadeSlots;

        [HideInInspector] public AbstractCombat ability;

        [Tooltip("Invoked when an item is first dragged out of this container")]
        public UnityEvent onDragBegin;
        [Tooltip("Invoked when an item has been dragged out of this container, and that drag has ended")]
        public UnityEvent onDragEnd;

        [Tooltip("Invoked when an item has been dragged intot he wrong slot, and snaps back to its start position")]
        public UnityEvent onDragFail;

        [Tooltip("Invoked when an item is dragged over one of our slots, entering our airspace")]
        public UnityEvent onDragEnter;
        [Tooltip("Invoked when the dragged item no longer lies over our slots")]
        public UnityEvent onDragExit;

        // in the general class, we can drag and drop anything anywhere
        public virtual bool CanDrag(Draggable dragged) { return true; }
        public virtual bool CanDrop(Draggable dragged, Slot slot) { return true; }
        public abstract void Drop(Slot slot, ObjectContainer fromContainer);
        public virtual void ThrowAway(Draggable dragged) { }
        public virtual void OnDragBegin()
        {
            onDragBegin.Invoke();
        }

        public virtual void OnDraggableEnter()
        {
            onDragEnter.Invoke();
        }

        public virtual void OnDraggableExit()
        {
            onDragExit.Invoke();
        }

        public virtual bool IsReadOnly()
        {
            return false;
        }


        // create a Slot (or use the optional supplied one) and populate it with the given object
        protected Slot MakeSlot(UnityEngine.Object obj, Slot preMade = null)
        {
            GameObject go = null;
            Slot slot = preMade;

            if (slot != null)
            {
                // use the existing one and get its GameObject
                go = slot.gameObject;
            }
            else
            {
                // make a child slot
                go = Instantiate(slotPrefab.gameObject, transform);
                slot = go.GetComponent<Slot>();
            }

            // make an item object inside it as a child
            GameObject goi = Instantiate(itemPrefab.gameObject, slot.GetSlot());
            Draggable item = goi.GetComponent<Draggable>();
            item.SetObject(obj);

            // set up all required pointers
            slot.item = item;
            slot.container = this;
            item.slot = slot;

            return slot;
        }
    }
}