using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BFTT.Combat;
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

    public LayerMask originalExcludeLayer;

    private void Awake()
    {
        _ikScheduler = GetComponent<IKScheduler>();
        _strafe = GetComponent<Strafe>();
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
        if (!_action.UseCard) return true;
        else return false;
    }

    public override void UpdateCombat()
    {
        if (!_action.UseCard && !throwSword && time == 0 && !isReturning)
            StopCombat();

        if (_manager.currentCard == this && throwSword && effects.hitSomething)
        {
            _strafe.canMove = true;
            if (_action.zoom)
                ReturnSword();
        }

        if (isReturning)
        {
            if (time < 1.0f)
            {
                rb.position = getBQCPoint(time, oldPosition, curvePoint.position, target.position);
                rb.rotation = Quaternion.Slerp(rb.transform.rotation, target.rotation, 50 * Time.unscaledDeltaTime);
                time += Time.deltaTime;
            }
            else
            {
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
    }

    void ReturnSword()
    {
        Debug.Log("returning sword");
        isReturning = true;
        oldPosition = rb.position;
        rb.velocity = Vector3.zero;
        rb.isKinematic = true;
        throwSword = false;
        rb.excludeLayers = originalExcludeLayer;
        rb.GetComponent<BoxCollider>().excludeLayers = originalExcludeLayer;
    }

    Vector3 getBQCPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        Vector3 p = (uu * p0) + (2 * u * t * p1) + (tt * p2);
        return p;
    }

    void ResetSword()
    {
        isReturning = false;
        rb.transform.parent = target;
        rb.position = target.position;
        rb.rotation = target.rotation;
        time = 0;
        GetComponent<Animator>().SetBool("Sword", true);
        throwSword = false;
        effects.activated = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Sword") && swordOut)
        {
            // If the player lands on the sword, apply the bending effect
            effects.ApplyBend(collision.rigidbody.mass);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Sword") && swordOut)
        {
            // When the player jumps off the sword, apply the diving board effect
            Rigidbody playerRb = GetComponent<Rigidbody>();
            Vector3 jumpForce = Vector3.up * 7f; // Adjust force as needed
            playerRb.AddForce(jumpForce, ForceMode.Impulse);

            // Reset the sword's bend
            effects.ApplyBend(0f);
        }
    }
}
