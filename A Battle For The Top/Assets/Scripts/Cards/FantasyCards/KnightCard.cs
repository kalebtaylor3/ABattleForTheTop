using BFTT.Abilities;
using BFTT.Combat;
using BFTT.IK;
using System.Collections;
using UnityEngine;

public class KnightCard : AbstractCombat
{
    bool throwSword = false;
    bool swordOut = false;
    bool isReturning = false;
    bool readyToReturn = false; // Flag to check if the sword is ready to return
    bool zoomWasReleased = false; // New flag to track if zoom was released
    public Transform target, curvePoint;
    private Vector3 oldPosition;
    Rigidbody rb;
    SwordEffects effects;
    private float time = 0.0f;

    private IKScheduler _ikScheduler;
    private AbstractAbility _strafe;

    public LayerMask originalExcludeLayer;

    public ParticleSystem[] trail;

    public Vector3 origionalScale;

    public DisplayMessage simpleMessageDisplay;
    public float messageDisplayTime = 2f; // Time in seconds to show the message

    private bool isMessageActive = false;

    bool hasStoodOnSword = false;

    private void Awake()
    {
        _ikScheduler = GetComponent<IKScheduler>();
        _strafe = GetComponent<Strafe>();
        abilityProp.layer = LayerMask.NameToLayer("Character");
        origionalScale = abilityProp.transform.localScale;
    }

    public override bool CombatReadyToRun()
    {
        if (_manager.currentCard == this && !swordOut)
        {
            GetComponent<Animator>().SetBool("Sword", true);

            for (int i = 0; i < trail.Length; i++)
            {
                trail[i].Stop();
            }

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
            zoomWasReleased = false; // Reset zoom release state
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

            //handle combo system
        }
    }

    public override void OnStopCombat()
    {
        Debug.Log("stopped sword swing");
    }

    public override bool ReadyToExit()
    {
        if (!_action.UseCard && !isReturning && !throwSword) return true;
        else return false;
    }

    public override void UpdateCombat()
    {

        if (_action.NextCard || _action.PreviousCard)
            if (throwSword || isReturning)
                if (!isMessageActive)
                    StartCoroutine(HideMessageAfterTime(messageDisplayTime));

        if (!_action.UseCard && !throwSword && time == 0 && !isReturning)
            StopCombat();

        if (_manager.currentCard == this && throwSword && effects.hitSomething)
        {
            abilityProp.layer = LayerMask.NameToLayer("Short Climb");
            abilityProp.transform.localScale = transform.localScale * 2;
            _strafe.canMove = true;
            for (int i = 0; i < trail.Length; i++)
            {
                trail[i].Stop();
            }

            if (!_action.zoom && !zoomWasReleased) // Check if zoom was released
            {
                zoomWasReleased = true;
            }

            if (_action.zoom && zoomWasReleased) // Return sword if zoom was released and then zoom is true again
            {
                ReturnSword();
            }
        }

        if (isReturning)
        {
            // Failsafe threshold distance
            float thresholdDistance = 0.15f;

            if (Vector3.Distance(rb.position, target.position) <= thresholdDistance)
            {
                // Snap to the target position and rotation if within the threshold
                ResetSword();
                abilityProp.transform.position = target.position;
            }
            else if (time < 1.0f)
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

    private IEnumerator HideMessageAfterTime(float time)
    {
        simpleMessageDisplay.SetShowMessage(true, "", "Return sword to change ability");
        isMessageActive = true;
        yield return new WaitForSeconds(time);
        simpleMessageDisplay.SetShowMessage(false, "", "Return sword to change ability");
        isMessageActive = false;
    }

    public void ThrowSword()
    {
        GetComponent<Animator>().SetBool("Sword", false);
        isReturning = false;
        readyToReturn = false; // Reset the return readiness
        effects.activated = true;
        rb.isKinematic = false;
        rb.transform.parent = null;
        rb.AddForce(Camera.main.transform.TransformDirection(Vector3.forward) * 25, ForceMode.Impulse);

        for (int i = 0; i < trail.Length; i++)
        {
            trail[i].Play();
        }
    }

    void ReturnSword()
    {
        Debug.Log("returning sword");
        isReturning = true;
        zoomWasReleased = false; // Reset zoom release state after return
        abilityProp.transform.localScale = origionalScale * 2.2f;
        oldPosition = rb.position;
        rb.velocity = Vector3.zero;
        rb.isKinematic = true;
        throwSword = false;
        rb.excludeLayers = originalExcludeLayer;
        rb.GetComponent<MeshCollider>().excludeLayers = originalExcludeLayer;
        rb.GetComponent<BoxCollider>().excludeLayers = originalExcludeLayer;
        abilityProp.layer = LayerMask.NameToLayer("Character");

        for (int i = 0; i < trail.Length; i++)
        {
            trail[i].Play();
        }
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
        abilityProp.transform.localScale = origionalScale;
        GetComponent<Animator>().SetBool("Sword", true);
        throwSword = false;
        effects.activated = false;
        for (int i = 0; i < trail.Length; i++)
        {
            trail[i].Stop();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Sword") && swordOut)
        {
            // Check if the player's position is above the sword
            Vector3 playerPosition = transform.position;
            Vector3 swordPosition = other.transform.position;

            // Check if the player is within a certain distance above the sword
            float verticalThreshold = 0.5f; // Adjust this threshold as needed
            if (playerPosition.y > swordPosition.y && playerPosition.y - swordPosition.y <= verticalThreshold)
            {
                // If the player is standing on the sword, apply the bending effect
                hasStoodOnSword = true;
                effects.StartBend(other.GetComponent<Rigidbody>().mass, 0.3f);
            }
        }
    }


    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Sword") && swordOut)
        {
            if (hasStoodOnSword)
            {
                // When the player jumps off the sword, apply the diving board effect
                Rigidbody playerRb = GetComponent<Rigidbody>();
                Vector3 jumpForce = Vector3.up * 7f; // Adjust force as needed
                playerRb.AddForce(jumpForce, ForceMode.Impulse);

                // Reset the sword's bend
                effects.StartBend(0f, 0.3f);
                hasStoodOnSword = false;
            }
        }
    }
}