using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rope : MonoBehaviour
{
    public Transform attachPoint;
    public Rigidbody ropeRigidbody;

    [Header("Swing Limits")]
    [SerializeField] private bool limitSwing = true;
    [SerializeField] private float maxSwingAngle = 95f;
    [SerializeField] private float limitContactDistance = 2f;

    private void Awake()
    {
        ApplyHingeLimits();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
            ApplyHingeLimits();
    }
#endif

    private void ApplyHingeLimits()
    {
        Transform searchRoot = GetSearchRoot();
        HingeJoint[] hingeJoints = searchRoot.GetComponentsInChildren<HingeJoint>(true);

        foreach (HingeJoint hingeJoint in hingeJoints)
        {
            hingeJoint.useLimits = limitSwing;

            JointLimits limits = hingeJoint.limits;
            limits.min = -maxSwingAngle;
            limits.max = maxSwingAngle;
            limits.bounciness = 0f;
            limits.contactDistance = limitContactDistance;
            hingeJoint.limits = limits;
        }
    }

    private Transform GetSearchRoot()
    {
        Transform current = transform;

        while (current.parent != null)
            current = current.parent;

        return current;
    }
}
