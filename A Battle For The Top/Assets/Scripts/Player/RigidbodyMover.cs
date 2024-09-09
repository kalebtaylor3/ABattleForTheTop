using UnityEngine;
using UnityEngine.InputSystem;

namespace BFTT.Components
{
    public class RigidbodyMover : MonoBehaviour, IMover, ICapsule
    {
        [Header("Player")]
        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;
        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;
        [Tooltip("Distance that should cast for ground")]
        public float GroundedCheckDistance = 0.14f;
        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;
        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Gravity")]
        [Tooltip("Changes the engine default value at awake")]
        [SerializeField] private float Gravity = -15.0f;

        [Header("Sliding")]
        [Tooltip("Sliding force applied on ice platforms")]
        [SerializeField] private float slidingForce = 1f;

        [Header("No Clip")]
        [Tooltip("Toggle No Clip mode")]
        public bool NoClipEnabled = false;

        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _initialCapsuleHeight = 2f;
        private float _initialCapsuleRadius = 0.28f;
        private bool _wasNoClipEnabled = false;

        private bool _useRootMotion = false;
        private Vector3 _rootMotionMultiplier = Vector3.one;
        private bool _useRotationRootMotion = false;

        private int _animIDSpeed;
        private int _animIDMotionSpeed;

        [HideInInspector] public Animator _animator;
        private Rigidbody _rigidbody;
        private CapsuleCollider _capsule;
        private GameObject _mainCamera;
        private bool grappling = false;

        private bool _hasAnimator;
        private bool _isOnIce = false;

        // Lasso-specific fields
        private bool isSwinging = false;
        private Vector3 swingPoint; // The point where the lasso is attached
        private float ropeLength; // The maximum length of the lasso
        private Vector2 playerInput; // Store player movement input for swing control
        private float swingForceMultiplier = 30f; // Adjust this to tweak the swing force
        [HideInInspector] public bool isJumping = false; // Track if the player is jumping
        private float jumpCooldown = 0.9f; // Cooldown time to ensure jump finishes before swinging

        [HideInInspector] public bool lassoSwing = false;


        private void Awake()
        {
            _mainCamera = Camera.main.gameObject;
            _rigidbody = GetComponent<Rigidbody>();
            _capsule = GetComponent<CapsuleCollider>();

            _initialCapsuleHeight = _capsule.height;
            _initialCapsuleRadius = _capsule.radius;

            Physics.gravity = new Vector3(0, Gravity, 0);
        }

        private void Start()
        {
            _hasAnimator = TryGetComponent(out _animator);

            if (!_hasAnimator)
            {
                _animator = GetComponentInChildren<Animator>();
                _hasAnimator = true;
            }

            AssignAnimationIDs();
        }

        private void OnEnable()
        {
            AssignAnimationIDs();
        }

        private void FixedUpdate()
        {
            // Manage collision and gravity only if there's a change in NoClip status

            if (isJumping)
            {
                jumpCooldown -= Time.deltaTime;
                if (jumpCooldown <= 0)
                {
                    isJumping = false;
                }
            }

            if (NoClipEnabled != _wasNoClipEnabled)
            {
                if (NoClipEnabled)
                {
                    DisableCollision();
                    DisableGravity();
                }
                else
                {
                    EnableCollision();
                    if(!isSwinging)
                        EnableGravity();
                }
                _wasNoClipEnabled = NoClipEnabled;
            }

            if (!NoClipEnabled && !isSwinging)
            {
                GroundedCheck();
                GravityControl();
                HandleSliding();
            }

            if (isSwinging)
            {
                ApplySwinging();
                GroundedCheck();
            }
        }

        public void StartJump()
        {
            isJumping = true;
            jumpCooldown = 0.5f; // Reset the jump cooldown
        }

        private void ApplySwinging()
        {
            if (!Grounded && !isJumping)
            {
                // Calculate the direction from the swing point to the player
                Vector3 toSwingPoint = swingPoint - transform.position;
                float currentDistance = toSwingPoint.magnitude;

                // Adjust rope length to control hanging
                float adjustedRopeLength = ropeLength * 0.6f;

                // Get the normalized direction toward the swing point
                Vector3 swingDirection = toSwingPoint.normalized;

                // Correct the position if the player exceeds the rope length
                if (currentDistance > adjustedRopeLength)
                {
                    Vector3 correction = (currentDistance - adjustedRopeLength) * swingDirection;
                    _rigidbody.position -= correction;
                }

                // Calculate velocity along the rope direction and tangential velocity
                Vector3 velocityAlongRope = Vector3.Dot(_rigidbody.velocity, swingDirection) * swingDirection;
                Vector3 tangentialVelocity = _rigidbody.velocity - velocityAlongRope;

                if (isSwinging)
                {
                    // Apply gravity along the rope direction when no input is given
                    Vector3 gravityForce = Physics.gravity * (playerInput == Vector2.zero ? 2.0f : 0.2f);

                    // Convert player input into the player's local space to ensure it affects the swing directionally
                    Vector3 localInputDirection = _mainCamera.transform.TransformDirection(new Vector3(playerInput.x, 0, playerInput.y)).normalized;

                    // Project the local input onto the plane perpendicular to the swing direction
                    Vector3 tangentialInputForce = Vector3.ProjectOnPlane(localInputDirection, swingDirection) * swingForceMultiplier;

                    // Apply the total force (gravity and input) tangentially to the rope
                    Vector3 totalForce = gravityForce + tangentialInputForce;
                    _rigidbody.AddForce(totalForce - velocityAlongRope, ForceMode.Force);
                }

                // Limit tangential speed to avoid excessive swinging
                float maxSpeed = 250f;
                if (tangentialVelocity.magnitude > maxSpeed)
                {
                    tangentialVelocity = tangentialVelocity.normalized * maxSpeed;
                    _rigidbody.velocity = tangentialVelocity + velocityAlongRope;
                }

                // Apply damping to reduce velocity gradually when no input is given
                float dampingFactor = playerInput == Vector2.zero ? 0.999f : 0.998f;
                _rigidbody.velocity *= dampingFactor;

                // Constrain the player to the rope length
                Vector3 correctedPosition = swingPoint + (transform.position - swingPoint).normalized * adjustedRopeLength;
                _rigidbody.position = correctedPosition;
            }
        }




        public void UpdateSwingInput(Vector2 input)
        {
            playerInput = input;
        }

        private void OnAnimatorMove()
        {
            if (!_useRootMotion) return;

            Vector3 velocity = Vector3.Scale(_animator.deltaPosition / Time.deltaTime, _rootMotionMultiplier);
            if (_rigidbody.useGravity)
                velocity.y = _rigidbody.velocity.y;

            _rigidbody.velocity = velocity;
            transform.rotation *= _animator.deltaRotation;
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDMotionSpeed = Animator.StringToHash("Motion Speed");
        }

        public void StartSwing(Vector3 lassoPoint, float maxRopeLength)
        {
            isSwinging = true;
            swingPoint = lassoPoint;
            ropeLength = maxRopeLength;
        }

        // Disable swinging mode
        // Disable swinging mode and handle transition to grounded state
        public void StopSwing()
        {
            isSwinging = false;
            EnableGravity(); // Re-enable gravity when not swinging
        }


        private void GroundedCheck()
        {
            Vector3 spherePosition = transform.position + Vector3.up * GroundedRadius * 2;
            RaycastHit groundHit;
            Grounded = Physics.SphereCast(spherePosition, GroundedRadius, Vector3.down, out groundHit,
                GroundedCheckDistance + GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
        }

        private void HandleSliding()
        {
            if (Grounded && IsOnIce())
            {
                Vector3 slidingDirection = new Vector3(_rigidbody.velocity.x, 0, _rigidbody.velocity.z).normalized;
                if (slidingDirection == Vector3.zero)
                {
                    slidingDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
                }
                _rigidbody.AddForce(slidingDirection * slidingForce, ForceMode.Acceleration);
            }
        }

        private bool IsOnIce()
        {
            Vector3 spherePosition = transform.position + Vector3.up * GroundedRadius * 2;
            RaycastHit hit;
            if (Physics.SphereCast(spherePosition, GroundedRadius, Vector3.down, out hit, GroundedCheckDistance + GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.CompareTag("Ice"))
                {
                    return true;
                }
            }
            return false;
        }

        public Collider GetGroundCollider()
        {
            if (!Grounded) return null;

            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedCheckDistance, transform.position.z);
            Collider[] grounds = Physics.OverlapSphere(spherePosition, _capsule.radius, GroundLayers, QueryTriggerInteraction.Ignore);

            if (grounds.Length > 0)
                return grounds[0];

            return null;
        }

        public void Move(Vector2 moveInput, float targetSpeed, bool rotateCharacter = true)
        {
            Move(moveInput, targetSpeed, _mainCamera.transform.rotation, rotateCharacter);
        }

        public void Move(Vector2 moveInput, float targetSpeed, Quaternion cameraRotation, bool rotateCharacter = true)
        {
            if (NoClipEnabled)
            {
                NoClipMove(moveInput, targetSpeed, cameraRotation);
                return;
            }

            if (moveInput == Vector2.zero) targetSpeed = 0.0f;

            float currentHorizontalSpeed = new Vector3(_rigidbody.velocity.x, 0.0f, _rigidbody.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = moveInput.magnitude;

            if (inputMagnitude > 1)
                inputMagnitude = 1f;

            if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }
            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);

            Vector3 inputDirection = new Vector3(moveInput.x, 0.0f, moveInput.y).normalized;

            if (moveInput != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + cameraRotation.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);

                if (rotateCharacter && !_useRotationRootMotion)
                    transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }

            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }

            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            if (!_useRootMotion)
            {
                Vector3 velocity = targetDirection.normalized * _speed;
                velocity.y = _rigidbody.velocity.y;

                _rigidbody.velocity = velocity;
            }
        }

        private void NoClipMove(Vector2 moveInput, float targetSpeed, Quaternion cameraRotation)
        {
            float inputMagnitude = moveInput.magnitude;

            Vector3 inputDirection = new Vector3(moveInput.x, 0.0f, moveInput.y).normalized;
            Vector3 forwardMovement = cameraRotation * Vector3.forward * moveInput.y;
            Vector3 strafeMovement = cameraRotation * Vector3.right * moveInput.x;

            // Handle vertical movement only when the camera is angled up or down
            Vector3 cameraForward = cameraRotation * Vector3.forward;
            float cameraAngle = Vector3.Angle(cameraForward, Vector3.up);
            Vector3 verticalMovement = Vector3.zero;

            if (cameraAngle < 90) // Looking up
            {
                verticalMovement = Vector3.up * inputMagnitude * Mathf.Cos(cameraAngle * Mathf.Deg2Rad);
            }
            else if (cameraAngle > 90) // Looking down
            {
                verticalMovement = Vector3.down * inputMagnitude * Mathf.Cos((180 - cameraAngle) * Mathf.Deg2Rad);
            }

            Vector3 moveDirection = (forwardMovement + strafeMovement + verticalMovement).normalized;
            Vector3 velocity = moveDirection * (targetSpeed * 2);

            _rigidbody.velocity = velocity;
        }


        public void Move(Vector3 velocity)
        {
            if (_rigidbody.useGravity && !NoClipEnabled)
                velocity.y = _rigidbody.velocity.y;

            _rigidbody.velocity = velocity;
        }

        private void GravityControl()
        {
            if (_rigidbody.useGravity)
            {
                if (Grounded)
                {
                    if (_rigidbody.velocity.y < 0.0f)
                    {
                        Vector3 velocity = _rigidbody.velocity;
                        velocity.y = Mathf.Clamp(velocity.y, -2, 0);
                        _rigidbody.velocity = velocity;
                    }
                }
            }
        }

        public Quaternion GetRotationFromDirection(Vector3 direction)
        {
            float yaw = Mathf.Atan2(direction.x, direction.z);
            return Quaternion.Euler(0, yaw * Mathf.Rad2Deg, 0);
        }

        public void SetPosition(Vector3 newPosition)
        {
            _rigidbody.position = newPosition + _rigidbody.velocity * Time.fixedDeltaTime;
        }

        public void SetRotation(Quaternion newRotation)
        {
            _rigidbody.rotation = newRotation;
        }

        public void DisableCollision()
        {
            _capsule.enabled = false;
        }

        public void EnableCollision()
        {
            _capsule.enabled = true;
        }

        public void SetCapsuleSize(float newHeight, float newRadius)
        {
            _capsule.height = newHeight;
            _capsule.center = new Vector3(0, newHeight * 0.5f, 0);

            if (newRadius > newHeight * 0.5f)
                newRadius = newHeight * 0.5f;

            _capsule.radius = newRadius;
        }

        public void ResetCapsuleSize()
        {
            SetCapsuleSize(_initialCapsuleHeight, _initialCapsuleRadius);
        }

        public void SetVelocity(Vector3 velocity)
        {
            _rigidbody.velocity = velocity;
        }

        public Vector3 GetVelocity()
        {
            return _rigidbody.velocity;
        }

        public float GetGravity()
        {
            return Gravity;
        }

        public void ApplyRootMotion(Vector3 multiplier, bool applyRotation = false)
        {
            _useRootMotion = true;
            _rootMotionMultiplier = multiplier;
            _useRotationRootMotion = applyRotation;
        }

        public void StopRootMotion()
        {
            _useRootMotion = false;
            _useRotationRootMotion = false;
        }

        public float GetCapsuleHeight()
        {
            return _capsule.height;
        }

        public float GetCapsuleRadius()
        {
            return _capsule.radius;
        }

        public void EnableGravity()
        {
            _rigidbody.useGravity = true;
        }

        public void DisableGravity()
        {
            _rigidbody.useGravity = false;
        }

        bool IMover.IsGrounded()
        {
            return Grounded;
        }

        public void StopMovement()
        {
            _rigidbody.velocity = Vector3.zero;
            _speed = 0;

            _animator.SetFloat(_animIDSpeed, 0);
            _animator.SetFloat(_animIDMotionSpeed, 0);
        }

        public Vector3 GetRelativeInput(Vector2 input)
        {
            Vector3 relative = _mainCamera.transform.right * input.x +
                   Vector3.Scale(_mainCamera.transform.forward, new Vector3(1, 0, 1)) * input.y;

            return relative;
        }

        public bool IsGrappling()
        {
            return grappling;
        }

        public void SetIsGrappling(bool value)
        {
            grappling = value;
        }

        public void SetNoClip()
        {
            StopMovement();
            Grounded = true; 
            _rigidbody.velocity = Vector3.zero;
            NoClipEnabled = !NoClipEnabled;
        }
    }
}
