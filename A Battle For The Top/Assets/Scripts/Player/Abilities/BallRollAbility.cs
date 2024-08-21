using UnityEngine;
using BFTT.Components;

namespace BFTT.Abilities
{
    public class BallRollAbility : AbstractAbility
    {
        [SerializeField] private float rollSpeed = 10f; // Speed of the ball when rolling
        [SerializeField] private float jumpForce = 8f; // Force applied for jumping
        [SerializeField] private float maxSpeed = 15f; // Maximum speed of the ball
        [SerializeField] private float accelerationRate = 5f; // Rate of acceleration to reach max speed
        [SerializeField] private float decelerationRate = 7f; // Rate of deceleration when not moving
        private Rigidbody _rigidbody;
        private bool _isRolling = false;
        private RigidbodyMover _mover;
        CardManager _manager;
        public BallRollCard _card;
        public GameObject playerModel;
        public GameObject tumbleWeedModel;

        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;
        [Tooltip("Distance that should cast for ground")]
        public float GroundedCheckDistance = 0.24f;
        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.38f;
        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

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

        private void GroundedCheck()
        {
            Vector3 spherePosition = transform.position + Vector3.up * GroundedRadius * 2;
            RaycastHit groundHit;
            Grounded = Physics.SphereCast(spherePosition, GroundedRadius, Vector3.down, out groundHit,
                GroundedCheckDistance + GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
        }

        public override void UpdateAbility()
        {
            if (_manager.currentCard != _card)
                StopAbility();

            if (_isRolling && canMove)
            {

                GroundedCheck();
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

                // Apply force with acceleration control
                ApplyMovement(movement);

                // Rotate the model based on movement
                RotateModel();

                // Handle jump if grounded
                if (_action.jump && Grounded)
                {
                    Jump();
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
            _rigidbody.AddForce(transform.forward * 10, ForceMode.VelocityChange);
        }

        private void Jump()
        {
            _rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        private void ApplyMovement(Vector3 movement)
        {
            // Calculate current speed
            float currentSpeed = _rigidbody.velocity.magnitude;

            // Adjust the applied force based on current speed
            if (currentSpeed < maxSpeed)
            {
                _rigidbody.AddForce(movement * rollSpeed * (accelerationRate / maxSpeed), ForceMode.Acceleration);
            }
            else
            {
                // Apply deceleration if over max speed
                _rigidbody.velocity = Vector3.Lerp(_rigidbody.velocity, movement.normalized * maxSpeed, decelerationRate * Time.deltaTime);
            }
        }

        private void RotateModel()
        {
            Vector3 velocity = _rigidbody.velocity;

            if (velocity != Vector3.zero)
            {
                // Calculate the rotation axis, which is perpendicular to the velocity vector
                Vector3 rotationAxis = Vector3.Cross(Vector3.up, velocity).normalized;

                // Calculate the rotation amount based on the ball's velocity
                // Increase the rotation speed by multiplying by a larger factor if needed
                float rotationAmount = (velocity.magnitude / tumbleWeedModel.transform.localScale.x) * Mathf.Rad2Deg * Time.deltaTime;

                // Apply the rotation to the model
                tumbleWeedModel.transform.Rotate(rotationAxis, rotationAmount, Space.World);
            }
        }
    }
}
