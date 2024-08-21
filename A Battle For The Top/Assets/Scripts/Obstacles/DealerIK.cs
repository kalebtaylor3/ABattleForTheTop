using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BFTT.IK;

public class DealerIK : MonoBehaviour
{
    public Transform leftHandTargetIdle;   // Idle position for the left hand
    public Transform rightHandTargetIdle;  // Idle position for the right hand
    public Transform spineTargetIdle;      // Idle position for the spine

    public Transform leftHandTargetMiddle;   // Middle position for the left hand
    public Transform rightHandTargetMiddle;  // Middle position for the right hand
    public Transform spineTargetMiddle;      // Middle position for the spine

    public Transform leftHandTargetLost;   // Losing position for the left hand
    public Transform rightHandTargetLost;  // Losing position for the right hand
    public Transform spineTargetLost;      // Losing position for the spine

    public Transform neckTargetDown;       // Position where the neck is fully down (head on the table)
    public Transform neckTargetUp;         // Position where the neck is up (head raised)

    private IKScheduler _scheduler;
    private bool dealerLost = false;
    private bool transitionComplete = false;
    private bool returningToIdle = false; // Flag to track if we're returning to idle
    private float transitionProgress = 0f;    // Progress of the transition
    private float neckBangProgress = 0f;      // Progress of the neck-banging animation
    private float elapsedTime = 0f;           // Time elapsed since the head-banging started

    [SerializeField] private float transitionDuration = 1.0f;  // Duration of the transition to "lost"
    [SerializeField] private float returnToIdleDuration = 0.5f;  // Duration of the transition from "lost" to "idle"
    [SerializeField] private float neckBangDuration = 0.5f;    // Duration of one head-bang cycle
    [SerializeField] private float maxBangTime = 3.0f;         // Maximum time for the head-banging animation

    private void Awake()
    {
        _scheduler = GetComponent<IKScheduler>();
    }

    private void Start()
    {
        StartCoroutine(WaitToLose());
        StartCoroutine(WaitToIdle());
    }

    private void Update()
    {
        // Transition to "lost" position
        if (dealerLost && !returningToIdle && transitionProgress < 1f)
        {
            transitionProgress += Time.deltaTime / transitionDuration;
        }
        // Transition back to "idle" position, skipping the middle position
        else if (returningToIdle && transitionProgress > 0f)
        {
            transitionProgress -= Time.deltaTime / returnToIdleDuration;
        }

        // Determine if the transition to the lost position is complete
        transitionComplete = transitionProgress >= 1f;

        // Use the transition progress to determine the current position
        Vector3 leftHandPos, rightHandPos;
        Quaternion leftHandRot, rightHandRot, spineRot;

        if (!returningToIdle && transitionProgress < 0.5f)
        {
            // First half: Idle -> Middle
            float t = transitionProgress * 2f;
            leftHandPos = Vector3.Lerp(leftHandTargetIdle.position, leftHandTargetMiddle.position, t);
            leftHandRot = Quaternion.Slerp(leftHandTargetIdle.rotation, leftHandTargetMiddle.rotation, t);
            rightHandPos = Vector3.Lerp(rightHandTargetIdle.position, rightHandTargetMiddle.position, t);
            rightHandRot = Quaternion.Slerp(rightHandTargetIdle.rotation, rightHandTargetMiddle.rotation, t);
            spineRot = Quaternion.Slerp(spineTargetIdle.rotation, spineTargetMiddle.rotation, t);
        }
        else if (!returningToIdle)
        {
            // Second half: Middle -> Lost
            float t = (transitionProgress - 0.5f) * 2f;
            leftHandPos = Vector3.Lerp(leftHandTargetMiddle.position, leftHandTargetLost.position, t);
            leftHandRot = Quaternion.Slerp(leftHandTargetMiddle.rotation, leftHandTargetLost.rotation, t);
            rightHandPos = Vector3.Lerp(rightHandTargetMiddle.position, rightHandTargetLost.position, t);
            rightHandRot = Quaternion.Slerp(rightHandTargetMiddle.rotation, rightHandTargetLost.rotation, t);
            spineRot = Quaternion.Slerp(spineTargetMiddle.rotation, spineTargetLost.rotation, t);
        }
        else
        {
            // Directly transition from Lost to Idle, skipping the Middle
            float t = 1f - transitionProgress;
            leftHandPos = Vector3.Lerp(leftHandTargetLost.position, leftHandTargetIdle.position, t);
            leftHandRot = Quaternion.Slerp(leftHandTargetLost.rotation, leftHandTargetIdle.rotation, t);
            rightHandPos = Vector3.Lerp(rightHandTargetLost.position, rightHandTargetIdle.position, t);
            rightHandRot = Quaternion.Slerp(rightHandTargetLost.rotation, rightHandTargetIdle.rotation, t);
            spineRot = Quaternion.Slerp(spineTargetLost.rotation, spineTargetIdle.rotation, t);
        }

        // Apply IK passes for hands and spine
        IKPass rightHandPass = new IKPass(rightHandPos, rightHandRot, AvatarIKGoal.RightHand, 1, 1);
        _scheduler.ApplyIK(rightHandPass);

        IKPass leftHandPass = new IKPass(leftHandPos, leftHandRot, AvatarIKGoal.LeftHand, 1, 1);
        _scheduler.ApplyIK(leftHandPass);

        SpineIKPass spinePass = new SpineIKPass(Vector3.zero, spineRot, HumanBodyBones.Spine, 0, 1);
        _scheduler.ApplySpineIK(spinePass);

        // Handle neck head-banging animation if the dealer has lost and time has not elapsed
        if (dealerLost && transitionComplete && elapsedTime < maxBangTime)
        {
            neckBangProgress += Time.deltaTime / neckBangDuration;
            elapsedTime += Time.deltaTime;

            // Loop the neckBangProgress between 0 and 1 for a continuous back-and-forth animation
            float t = Mathf.PingPong(neckBangProgress, 1f);

            // Interpolate between the up and down positions for the neck
            Quaternion neckRot = Quaternion.Slerp(neckTargetUp.rotation, neckTargetDown.rotation, t);
            NeckIKPass neckPass = new NeckIKPass(Vector3.zero, neckRot, HumanBodyBones.Neck, 0, 1);
            _scheduler.ApplyNeckIK(neckPass);
        }
        else
        {
            // Ensure the neck returns to its idle position when not in the losing state or time has elapsed
            NeckIKPass neckPass = new NeckIKPass(Vector3.zero, neckTargetUp.rotation, HumanBodyBones.Neck, 0, 1);
            _scheduler.ApplyNeckIK(neckPass);
        }
    }

    IEnumerator WaitToLose()
    {
        yield return new WaitForSeconds(3);
        DealerLose();
    }

    IEnumerator WaitToIdle()
    {
        yield return new WaitForSeconds(6);
        ResetToIdle();
    }

    public void DealerLose()
    {
        dealerLost = true;
        transitionProgress = 0f;  // Reset progress for the smooth transition to lost pose
        neckBangProgress = 0f;    // Reset progress for the head-banging animation
        elapsedTime = 0f;         // Reset the elapsed time for the head-banging animation
        returningToIdle = false;
    }

    public void ResetToIdle()
    {
        dealerLost = false;
        returningToIdle = true;
        transitionProgress = 1f;  // Start progress for the smooth transition to idle pose
    }
}
