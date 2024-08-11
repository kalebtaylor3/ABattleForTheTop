using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace DragAndDrop
{
    public abstract class Draggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        protected bool dragging;
        public static Draggable current;
        static Slot currentSlotOver;

        // default to left dragging
        public InputAction InputAction;

        [HideInInspector]
        public Slot slot;
        [HideInInspector]
        public UnityEngine.Object obj;

        // override this in derived classes, usually casting to to the type they deal with
        public abstract void UpdateObject();

        private void OnEnable()
        {
            InputAction.Enable();
        }

        public void SetObject(UnityEngine.Object o)
        {
            (transform as RectTransform).anchoredPosition = Vector3.zero;
            obj = o;
            UpdateObject();
        }

        Canvas canvas;

        public void OnBeginDrag(PointerEventData eventData)
        {
            // start dragging with the right mouse, and only if the container will allow it
            if (InputAction.IsPressed())
                dragging = slot.container.CanDrag(this);

            if (dragging)
            {
                current = this;
                currentSlotOver = null;

                if (slot.container != null)
                    slot.container.OnDragBegin();

                // become a sibling of our slot's parent, so we're no longer part of the container (and don't disrupt the GridLayout)
                Transform p = slot.transform.parent.parent;
                while (p.GetComponent<Canvas>() == null && p != null)
                    p = p.parent;
                transform.SetParent(p);
                // move this to the very front of the UI, so the dragged element draws over everything
                transform.SetAsLastSibling();

                // lazy initialisation to find the canvas we're a part of   
                if (canvas == null)
                {
                    Transform t = transform;
                    while (t != null && canvas == null)
                    {
                        t = t.parent;
                        canvas = t.GetComponent<Canvas>();
                    }
                }
                // move that canvas forwards, so we're dragged on top of other items, and not behind them
                if (canvas)
                    canvas.sortingOrder = 1;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            // move the dragged object with the mouse
            if (dragging)
                transform.position = eventData.position;

            // highlight squares that can receive this component?
            Slot slot = GetSlotUnderMouse();
            if (slot != currentSlotOver)
            {
                if (slot)
                    slot.OnDraggableEnter();
                if (currentSlotOver)
                    currentSlotOver.OnDraggableExit();
                ObjectContainer oldContainer = currentSlotOver != null ? currentSlotOver.container : null;
                ObjectContainer newContainer = slot != null ? slot.container : null;
                if (oldContainer != null)
                    oldContainer.OnDraggableExit();
                if (newContainer != null)
                    newContainer.OnDraggableEnter();

                currentSlotOver = slot;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!dragging)
                return;

            // raycast to find what we're dropping over
            Slot target = GetSlotUnderMouse();

            ObjectContainer containerTarget = null;
            ObjectContainer containerDrag = slot.container;

            bool legal = true;

            // check that this move is OK with both containers - they can both veto it
            if (target)
            {
                containerTarget = target.container;

                // if there is a container and we can't drop into it for game logic reasons, cancel the drag
                if (containerTarget != null)
                    if (containerTarget.CanDrop(this, target) == false)
                        legal = false;

                // check the other way too, since the drag and drop is a swap
                if (containerDrag != null)
                    if (containerDrag.CanDrop(target.item, slot) == false)
                        legal = false;
            }

            // we're Ok to move
            if (legal)
            {
                Slot fromSlot = slot;
                SwapWith(target,
                    containerDrag != null ? containerDrag.IsReadOnly() : true,
                    containerTarget != null ? containerTarget.IsReadOnly() : true);
                // game logic - let both containers know about the update
                if (containerTarget != null)
                    containerTarget.Drop(target, containerDrag);
                if (containerDrag != null)
                    containerDrag.Drop(fromSlot, containerTarget);
            }
            else
            {
                // allow us to make a sound or anything similar that the rejecting container has specified
                containerTarget.onDragFail.Invoke();
            }

            // return to our parent slot now
            transform.SetParent(slot.GetSlot());
            (transform as RectTransform).anchoredPosition3D = Vector3.zero;
            dragging = false;
            current = null;

            // call the dragexit functions when we're placing, to unhighlight everything.
            // at the slot level...
            if (currentSlotOver)
                currentSlotOver.OnDraggableExit();
            // ...and at the container level
            ObjectContainer oldContainer = currentSlotOver != null ? currentSlotOver.container : null;
            if (oldContainer != null)
                oldContainer.OnDraggableExit();
            currentSlotOver = null;

            if (canvas)
                canvas.sortingOrder = 0;
        }

        // this avoids memory allocation each time we move while dragging
        static List<RaycastResult> hits = new List<RaycastResult>();

        // finds the firstSlot component currently under the mouse
        private Slot GetSlotUnderMouse()
        {
            PointerEventData pointer = new PointerEventData(EventSystem.current);
            pointer.position = Mouse.current.position.value;
            EventSystem.current.RaycastAll(pointer, hits);
            foreach (RaycastResult hit in hits)
            {
                Slot slot = hit.gameObject.GetComponent<Slot>();
                if (slot != null)
                    return slot;
            }
            return null;
        }

        void SwapWith(Slot _slot, bool readOnlySource, bool readOnlyTarget)
        {
            if (_slot == null)
            {
                // call a virtual function on the container that can eg spawn the item into the world
                if (slot.container != null)
                    slot.container.ThrowAway(this);

                // dispose if we're throwing the item away, no slot was under the mouse
                if (!readOnlySource)
                    SetObject(null);
            }
            else
            {
                // swap the two valid slot items around
                Draggable other = _slot.item;
                if (other)
                {
                    UnityEngine.Object o = obj;
                    if (!readOnlySource)
                    {
                        SetObject(other.obj);
                        if (other.obj != null && _slot != null)
                            _slot.onSlot.Invoke();
                    }
                    if (!readOnlyTarget)
                    {
                        other.SetObject(o);
                        if (o != null && other.slot != null)
                            other.slot.onSlot.Invoke();
                    }
                }
            }
        }
    }
}