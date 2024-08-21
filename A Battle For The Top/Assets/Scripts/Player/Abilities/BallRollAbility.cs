using UnityEngine;
using BFTT.Components;

namespace BFTT.Abilities
{
    public class BallRollAbility : AbstractAbility
    {
        [SerializeField] private float rollSpeed = 10f; // Speed of the ball when rolling
        [SerializeField] private float jumpForce = 8f; // Force applied for jumping
        private Rigidbody _rigidbody;
        private bool _isRolling = false;
        private RigidbodyMover _mover;
        CardManager _manager;
        public BallRollCard _card;
        public GameObject playerModel;
        public GameObject tumbleWeedModel;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _mover = GetComponent<RigidbodyMover>();
            _manager = GetComponent<CardManager>();
        }

        public override bool ReadyToRun()
        {
            // Check if the ability can be started (e.g., if grounded)
            return !_isRolling && _manager.currentCard == _card && _card.activated;
        }

        public override void OnStartAbility()
        {
            _isRolling = true;
            Debug.Log("Rolling");
            // Start the rolling movement
            playerModel.SetActive(false);
            tumbleWeedModel.SetActive(true);
            StartRolling();
            _animator.CrossFadeInFixedTime("Ball", 0.1f);
        }

        public override void UpdateAbility()
        {
            if (_manager.currentCard != _card)
                StopAbility();

            if (_isRolling && canMove)
            {
                // Calculate movement relative to the camera's direction
                Vector3 forward = Camera.main.transform.forward;
                Vector3 right = Camera.main.transform.right;

                // Flatten the vectors so the ball only moves on the XZ plane
                forward.y = 0;
                right.y = 0;
                forward.Normalize();
                right.Normalize();

                // Combine the forward and right vectors with the input
                Vector3 movement = forward * _action.move.y + right * _action.move.x;
                _rigidbody.AddForce(movement * rollSpeed, ForceMode.Acceleration);

                // Rotate the model based on movement
                RotateModel(movement);

                // Handle jump if grounded
                if (_action.jump && _mover.Grounded)
                {
                    PerformJump();
                }
            }
        }

        public override void OnStopAbility()
        {
            playerModel.SetActive(true);
            tumbleWeedModel.SetActive(false);
            _isRolling = false;
            // Stop rolling movement
            _rigidbody.velocity = Vector3.zero;
            _card.activated = false;
        }

        private void StartRolling()
        {
            // Initial rolling movement if needed
            _rigidbody.AddForce(transform.forward * rollSpeed, ForceMode.VelocityChange);
        }

        private void PerformJump()
        {
            Debug.Log("Jumped as a ball");
            Vector3 velocity = _mover.GetVelocity();
            velocity.y = Mathf.Sqrt(jumpForce * -2f * _mover.GetGravity());

            _mover.SetVelocity(velocity);
        }

        private void RotateModel(Vector3 movement)
        {
            // Calculate the rotation based on the movement
            Vector3 rotationAxis = Vector3.Cross(Vector3.up, movement).normalized;
            float rotationAmount = movement.magnitude * rollSpeed * Time.deltaTime;

            // Rotate the model around the calculated axis
            tumbleWeedModel.transform.Rotate(rotationAxis, rotationAmount, Space.World);
        }
    }
}
