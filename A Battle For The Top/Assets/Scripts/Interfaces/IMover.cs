using UnityEngine;

namespace BFTT.Components
{
    public interface IMover
    {
        void Move(Vector2 moveInput, float targetSpeed, bool rotateCharacter = true);

        void Move(Vector2 moveInput, float targetSpeed, Quaternion cameraRotation, bool rotateCharacter = true);

        void Move(Vector3 velocity);
        void StopMovement();
        void SetVelocity(Vector3 velocity);
        Vector3 GetVelocity();
        float GetGravity();
        void EnableGravity();
        void DisableGravity();

        void SetPosition(Vector3 newPosition);
        void SetRotation(Quaternion newRotation);
        Quaternion GetRotationFromDirection(Vector3 direction);

        bool IsGrounded();
        void SetIsGrappling(bool value);
        bool IsGrappling();
        Collider GetGroundCollider();
        void ApplyRootMotion(Vector3 multiplier, bool applyRotation = false);
        void StopRootMotion();
        Vector3 GetRelativeInput(Vector2 input);
        void SetNoClip();
    }
}