using System.Collections.Generic;
using UnityEngine;

namespace BFTT.IK
{
    public class IKScheduler : MonoBehaviour
    {
        private Animator _animator = null;
        private List<IKPass> _ikPassList = new List<IKPass>();
        private List<SpineIKPass> _spineIkPassList = new List<SpineIKPass>();
        private List<NeckIKPass> _neckIkPassList = new List<NeckIKPass>();

        [SerializeField] private float IKSmoothTime = 0.12f;

        public bool _applyIK = true;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        private void Update()
        {
            // Update weight for IK
            foreach (IKPass currentIK in _ikPassList)
            {
                currentIK.UpdateWeight(IKSmoothTime);
            }

            // Update weight for Spine IK
            foreach (SpineIKPass currentIK in _spineIkPassList)
            {
                currentIK.UpdateWeight(IKSmoothTime);
            }

            // Update weight for Neck IK
            foreach (NeckIKPass currentIK in _neckIkPassList)
            {
                currentIK.UpdateWeight(IKSmoothTime);
            }
        }

        private void LateUpdate()
        {
            // Apply Spine IK in LateUpdate to avoid Animator override issues
            foreach (SpineIKPass currentIK in _spineIkPassList)
            {
                if (currentIK.weight < 0.1f) continue;

                Transform boneTransform = _animator.GetBoneTransform(currentIK.bone);
                if (boneTransform != null)
                {
                    boneTransform.localRotation = Quaternion.Slerp(boneTransform.localRotation, currentIK.rotation, currentIK.weight * currentIK.rotationWeight);
                }
            }

            // Apply Neck IK in LateUpdate to avoid Animator override issues
            foreach (NeckIKPass currentIK in _neckIkPassList)
            {
                if (currentIK.weight < 0.1f) continue;

                Transform boneTransform = _animator.GetBoneTransform(currentIK.bone);
                if (boneTransform != null)
                {
                    boneTransform.localRotation = Quaternion.Slerp(boneTransform.localRotation, currentIK.rotation, currentIK.weight * currentIK.rotationWeight);
                }
            }
        }

        private void OnAnimatorIK(int layerIndex)
        {
            // Only apply IK if it was asked to apply
            if ((_ikPassList.Count == 0 && _spineIkPassList.Count == 0 && _neckIkPassList.Count == 0) || !_applyIK) return;

            // Only apply IK on base layer
            if (layerIndex != 0) return;

            foreach (IKPass currentIK in _ikPassList)
            {
                if (currentIK.weight < 0.1f) continue;

                _animator.SetIKPositionWeight(currentIK.ikGoal, currentIK.weight * currentIK.positionWeight);
                _animator.SetIKRotationWeight(currentIK.ikGoal, currentIK.weight * currentIK.rotationWeight);

                _animator.SetIKPosition(currentIK.ikGoal, currentIK.position);
                _animator.SetIKRotation(currentIK.ikGoal, currentIK.rotation);
            }
        }

        public void ApplyIK(IKPass ikPass)
        {
            // Check if this IK is already in the list
            IKPass currentPass = _ikPassList.Find(x => x.ikGoal == ikPass.ikGoal);
            if (currentPass == null)
            {
                currentPass = new IKPass(ikPass);

                // Add current pass to the list
                _ikPassList.Add(currentPass);
            }
            else
                currentPass.CopyParameters(ikPass);

            currentPass.targetWeight = 1;
        }

        public void ApplySpineIK(SpineIKPass ikPass)
        {
            // Check if this IK is already in the list
            SpineIKPass currentPass = _spineIkPassList.Find(x => x.bone == ikPass.bone);
            if (currentPass == null)
            {
                currentPass = new SpineIKPass(ikPass.position, ikPass.rotation, ikPass.bone, ikPass.positionWeight, ikPass.rotationWeight);

                // Add current pass to the list
                _spineIkPassList.Add(currentPass);
            }
            else
            {
                currentPass.position = ikPass.position;
                currentPass.rotation = ikPass.rotation;
                currentPass.positionWeight = ikPass.positionWeight;
                currentPass.rotationWeight = ikPass.rotationWeight;
            }

            currentPass.targetWeight = 1;
        }

        public void ApplyNeckIK(NeckIKPass ikPass)
        {
            // Check if this IK is already in the list
            NeckIKPass currentPass = _neckIkPassList.Find(x => x.bone == ikPass.bone);
            if (currentPass == null)
            {
                currentPass = new NeckIKPass(ikPass.position, ikPass.rotation, ikPass.bone, ikPass.positionWeight, ikPass.rotationWeight);

                // Add current pass to the list
                _neckIkPassList.Add(currentPass);
            }
            else
            {
                currentPass.position = ikPass.position;
                currentPass.rotation = ikPass.rotation;
                currentPass.positionWeight = ikPass.positionWeight;
                currentPass.rotationWeight = ikPass.rotationWeight;
            }

            currentPass.targetWeight = 1;
        }

        public void StopIK(AvatarIKGoal goal)
        {
            IKPass currentPass = _ikPassList.Find(x => x.ikGoal == goal);

            if (currentPass == null) return;

            currentPass.targetWeight = 0;
        }

        public void StopSpineIK(HumanBodyBones bone)
        {
            SpineIKPass currentPass = _spineIkPassList.Find(x => x.bone == bone);

            if (currentPass == null) return;

            currentPass.targetWeight = 0;
        }

        public void StopNeckIK(HumanBodyBones bone)
        {
            NeckIKPass currentPass = _neckIkPassList.Find(x => x.bone == bone);

            if (currentPass == null) return;

            currentPass.targetWeight = 0;
        }
    }

    public class IKPass
    {
        public Vector3 position;
        public Quaternion rotation;
        public AvatarIKGoal ikGoal;
        public float weight;
        public float positionWeight;
        public float rotationWeight;

        private float _vel;
        public float targetWeight;

        public IKPass(Vector3 targetPosition, Quaternion targetRotation, AvatarIKGoal goal, float positionWeight, float rotationWeight)
        {
            position = targetPosition;
            rotation = targetRotation;
            ikGoal = goal;
            this.positionWeight = positionWeight;
            this.rotationWeight = rotationWeight;

            weight = 0;
        }

        public IKPass(IKPass reference)
        {
            position = reference.position;
            rotation = reference.rotation;
            ikGoal = reference.ikGoal;
            positionWeight = reference.positionWeight;
            rotationWeight = reference.rotationWeight;

            weight = 0;
        }

        public void CopyParameters(IKPass instanceToCopy)
        {
            position = instanceToCopy.position;
            rotation = instanceToCopy.rotation;
            positionWeight = instanceToCopy.positionWeight;
            rotationWeight = instanceToCopy.rotationWeight;
        }

        public void UpdateWeight(float smoothTime)
        {
            weight = Mathf.SmoothDamp(weight, targetWeight, ref _vel, smoothTime);
        }
    }

    public class SpineIKPass
    {
        public Vector3 position;
        public Quaternion rotation;
        public HumanBodyBones bone;
        public float weight;
        public float positionWeight;
        public float rotationWeight;

        private float _vel;
        public float targetWeight;

        public SpineIKPass(Vector3 targetPosition, Quaternion targetRotation, HumanBodyBones bone, float positionWeight, float rotationWeight)
        {
            position = targetPosition;
            rotation = targetRotation;
            this.bone = bone;
            this.positionWeight = positionWeight;
            this.rotationWeight = rotationWeight;

            weight = 0;
        }

        public void UpdateWeight(float smoothTime)
        {
            weight = Mathf.SmoothDamp(weight, targetWeight, ref _vel, smoothTime);
        }
    }

    public class NeckIKPass
    {
        public Vector3 position;
        public Quaternion rotation;
        public HumanBodyBones bone;
        public float weight;
        public float positionWeight;
        public float rotationWeight;

        private float _vel;
        public float targetWeight;

        public NeckIKPass(Vector3 targetPosition, Quaternion targetRotation, HumanBodyBones bone, float positionWeight, float rotationWeight)
        {
            position = targetPosition;
            rotation = targetRotation;
            this.bone = bone;
            this.positionWeight = positionWeight;
            this.rotationWeight = rotationWeight;

            weight = 0;
        }

        public void UpdateWeight(float smoothTime)
        {
            weight = Mathf.SmoothDamp(weight, targetWeight, ref _vel, smoothTime);
        }
    }
}
