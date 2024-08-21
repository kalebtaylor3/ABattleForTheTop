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

    public Transform desiredNeckRotation;

    private IKScheduler _scheduler;
    private bool dealerLost = false;
    private float transitionProgress = 0f;    // Progress of the transition

    [SerializeField] private float transitionDuration = 1.0f;  // Duration of the transition

    private void Awake()
    {
        _scheduler = GetComponent<IKScheduler>();
    }

    private void Start()
    {
        StartCoroutine(WaitToLose());
    }

    private void Update()
    {
        // If the dealer has lost, transition to the lost pose
        if (dealerLost && transitionProgress < 1f)
        {
            transitionProgress += Time.deltaTime / transitionDuration;
        }
        else if (!dealerLost && transitionProgress > 0f) // If transitioning back to idle, reduce the transitionProgress
        {
            transitionProgress -= Time.deltaTime / transitionDuration;
        }

        // Use the transition progress to determine which phase of the transition we're in
        Vector3 leftHandPos, rightHandPos;
        Quaternion leftHandRot, rightHandRot, spineRot;

        if (transitionProgress < 0.5f)
        {
            // First half: Idle -> Middle
            float t = transitionProgress * 2f;
            leftHandPos = Vector3.Lerp(leftHandTargetIdle.position, leftHandTargetMiddle.position, t);
            leftHandRot = Quaternion.Slerp(leftHandTargetIdle.rotation, leftHandTargetMiddle.rotation, t);
            rightHandPos = Vector3.Lerp(rightHandTargetIdle.position, rightHandTargetMiddle.position, t);
            rightHandRot = Quaternion.Slerp(rightHandTargetIdle.rotation, rightHandTargetMiddle.rotation, t);
            spineRot = Quaternion.Slerp(spineTargetIdle.rotation, spineTargetMiddle.rotation, t);
        }
        else
        {
            // Second half: Middle -> Lost
            float t = (transitionProgress - 0.5f) * 2f;
            leftHandPos = Vector3.Lerp(leftHandTargetMiddle.position, leftHandTargetLost.position, t);
            leftHandRot = Quaternion.Slerp(leftHandTargetMiddle.rotation, leftHandTargetLost.rotation, t);
            rightHandPos = Vector3.Lerp(rightHandTargetMiddle.position, rightHandTargetLost.position, t);
            rightHandRot = Quaternion.Slerp(rightHandTargetMiddle.rotation, rightHandTargetLost.rotation, t);
            spineRot = Quaternion.Slerp(spineTargetMiddle.rotation, spineTargetLost.rotation, t);
        }

        // Apply IK passes
        IKPass rightHandPass = new IKPass(rightHandPos, rightHandRot, AvatarIKGoal.RightHand, 1, 1);
        _scheduler.ApplyIK(rightHandPass);

        IKPass leftHandPass = new IKPass(leftHandPos, leftHandRot, AvatarIKGoal.LeftHand, 1, 1);
        _scheduler.ApplyIK(leftHandPass);

        SpineIKPass spinePass = new SpineIKPass(Vector3.zero, spineRot, HumanBodyBones.Spine, 0, 1);
        _scheduler.ApplySpineIK(spinePass);

        NeckIKPass neckPass = new NeckIKPass(Vector3.zero, desiredNeckRotation.rotation, HumanBodyBones.Neck, 0, 1);
        _scheduler.ApplyNeckIK(neckPass);

    }

    IEnumerator WaitToLose()
    {
        yield return new WaitForSeconds(3);
        DealerLose();
    }

    public void DealerLose()
    {
        dealerLost = true;
        transitionProgress = 0f;  // Reset progress for the smooth transition to lost pose
    }

    public void ResetToIdle()
    {
        dealerLost = false;
        transitionProgress = 1f;  // Reset progress for the smooth transition to idle pose
    }
}
