using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BFTT.Combat;
using static UnityEditor.IMGUI.Controls.PrimitiveBoundsHandle;
using BFTT.IK;
using BFTT.Abilities;

public class KnightCard : AbstractCombat
{

    bool throwSword = false;
    bool swordOut = false;
    bool isReturning = false;
    public Transform target, curvePoint;
    private Vector3 oldPosition;
    Rigidbody rb;
    SwordEffects effects;
    private float time = 0.0f;

    private IKScheduler _ikScheduler;

    private AbstractAbility _strafe;

    // Store original exclude layer
    public LayerMask originalExcludeLayer;
    public LayerMask climbLayer;


    private void Awake()
    {
        _ikScheduler = GetComponent<IKScheduler>();
        _strafe = GetComponent<Strafe>();
        abilityProp.layer = LayerMask.NameToLayer("Character");
    }

    public override bool CombatReadyToRun()
    {

        if (_manager.currentCard == this && !swordOut)
        {
            GetComponent<Animator>().SetBool("Sword", true);
            swordOut = true;
        }
        else if (_manager.currentCard != this && swordOut)
        {
            GetComponent<Animator>().SetBool("Sword", false);
            swordOut = false;
        }


        if (_manager.currentCard == this && _action.UseCard && !_action.zoom)
        {
            throwSword = false;
            return true;
        }

        if (_manager.currentCard == this && _action.UseCard && _action.zoom)
        {
            throwSword = true;
            return true;
        }

        return false;
    }

    public override void OnStartCombat()
    {

        rb = abilityProp.GetComponent<Rigidbody>();
        effects = rb.GetComponent<SwordEffects>();

        if (throwSword)
        {
            Debug.Log("Threw the sword");
            effects.hitSomething = false;
            if (_ikScheduler != null)
            {
                _ikScheduler.StopIK(AvatarIKGoal.RightHand);
            }
            _strafe.canMove = false;
            GetComponent<Animator>().SetTrigger("Throw");
            ThrowSword();
        }
        else
        {
            Debug.Log("Swung the sword");
            GetComponent<Animator>().SetTrigger("Swing");
        }
    }

    public override void OnStopCombat()
    {
        Debug.Log("stopped sword swing");
    }

    public override bool ReadyToExit()
    {

        ////**************

        //eventually also check if the swird is currently hurdeling toward the hit point
        if (!_action.UseCard) return true;
        else return false;
    }

    public override void UpdateCombat()
    {
        if (!_action.UseCard && !throwSword && time == 0 && !isReturning)
            StopCombat();

        if (_manager.currentCard == this && throwSword && effects.hitSomething)
        {
            abilityProp.layer = LayerMask.NameToLayer("Short Climb");
            _strafe.canMove = true;
            if (_action.zoom)
                ReturnSword();
        }

        if (isReturning)
        {
            // Returning calcs
            // Check if we haven't reached the end point, where time = 1
            if (time < 1.0f)
            {
                // Update its position by using the Bezier formula based on the current time
                rb.position = getBQCPoint(time, oldPosition, curvePoint.position, target.position);
                // Reset its rotation (from current to the targets rotation) with 50 units/s
                rb.rotation = Quaternion.Slerp(rb.transform.rotation, target.rotation, 50 * Time.unscaledDeltaTime);
                // Increase our timer, if you want the axe to return faster, then increase "time" more
                // With something like:
                // time += Timde.deltaTime * 2;
                // It will return as twice as fast
                time += Time.deltaTime;
            }
            else
            {
                // Otherwise, if it is 1 or more, we reached the target so reset
                ResetSword();
            }
        }
    }

    public void ThrowSword()
    {
        GetComponent<Animator>().SetBool("Sword", false);
        isReturning = false;
        effects.activated = true;
        rb.isKinematic = false;
        rb.transform.parent = null;
        rb.AddForce(Camera.main.transform.TransformDirection(Vector3.forward) * 25, ForceMode.Impulse);
        //rb.AddTorque(rb.transform.TransformDirection(Vector3.right) * 300, ForceMode.Impulse);
    }

    void ReturnSword()
    {
        Debug.Log("returning sword");
        isReturning = true;
        oldPosition = rb.position;
        rb.velocity = Vector3.zero;
        rb.isKinematic = true;
        throwSword = false;
        //has left the wall so reset the exclude layers for box collider and rb
        rb.excludeLayers = originalExcludeLayer;
        rb.GetComponent<BoxCollider>().excludeLayers = originalExcludeLayer;
        abilityProp.layer = LayerMask.NameToLayer("Character");
    }

    Vector3 getBQCPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        // "t" is always between 0 and 1, so "u" is other side of t
        // If "t" is 1, then "u" is 0
        float u = 1 - t;
        // "t" square
        float tt = t * t;
        // "u" square
        float uu = u * u;
        // this is the formula in one line
        // (u^2 * p0) + (2 * u * t * p1) + (t^2 * p2)
        Vector3 p = (uu * p0) + (2 * u * t * p1) + (tt * p2);
        return p;
    }

    void ResetSword()
    {
        // Axe has reached, so it is not returning anymore
        isReturning = false;
        // Attach back to its parent, in this case it will attach it to the player (or where you attached the script to)
        rb.transform.parent = target;
        // Set its position to the target's
        rb.position = target.position;
        // Set its rotation to the target's
        rb.rotation = target.rotation;
        time = 0;

        GetComponent<Animator>().SetBool("Sword", true);
        throwSword = false;
        effects.activated = false;    }
}