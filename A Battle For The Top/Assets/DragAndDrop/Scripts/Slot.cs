using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace DragAndDrop
{
    // these never move
    // we create one per slot in a backpack or whatever type of container, and each has a child Draggable that gets turned on or off.
    public class Slot : MonoBehaviour
    {
        [Tooltip("Leave as null for this to be the parent of the Draggable placed inside, or set to a child object if required")]
        public Transform slot;
        // the container that this Slot belongs to
        [HideInInspector]
        public ObjectContainer container;
        // used by ObjectContainerArray and ObjectContainerList to identify which element of the collection this Slot corresponds to
        [HideInInspector]
        public int index;

        Image image;

        public UnityEvent onDragEnterCanSlot;
        public UnityEvent onDragEnterCannotSlot;
        public UnityEvent onDragExit;
        public UnityEvent onSlot;

        // handy thing for greying out slots that can't be used yet
        bool isUnlocked;
        public bool Unlocked
        {
            get { return isUnlocked; }
            set
            {
                isUnlocked = value;
                if (image == null)
                    image = GetComponent<Image>();
                if (image)
                    image.color = isUnlocked ? Color.white : Color.grey;
            }
        }

        public void OnDraggableEnter()
        {
            if (container != null && container.CanDrop(Draggable.current, this))
                onDragEnterCanSlot.Invoke();
            else
                onDragEnterCannotSlot.Invoke();
        }

        public void OnDraggableExit()
        {
            onDragExit.Invoke();
        }

        // get the parent transform for any draggables placed in here.
        public Transform GetSlot()
        {
            return slot ? slot : transform;
        }

        // updates the slot's state when the item inside it changes.
        public virtual void UpdateSlot() { }

        // the child item sittign in this slot
        Draggable _item;

        // public accessor that updates the slot when set
        public Draggable item
        {
            get { return _item;}
            set { _item = value; UpdateSlot(); }
        }
    
    }
}