using System.Collections;
using UnityEngine;

namespace BFTT.Components
{
    public class HorseMover : MonoBehaviour, IMover, ICapsule
    {
        [Header("Player")]
        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 1.0f)]
        public float RotationSmoothTime = 0.6f; // Increased for slower rotation
        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 100.0f;

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

        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _initialCapsuleHeight = 2f;
        private float _initialCapsuleRadius = 0.28f;

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
        private bool _isTurning = false;

        private enum Gait { Idle, Trot, Gallop }
        private Gait _currentGait = Gait.Idle;

        public float _trotSpeed = 2.5f;
        public float _gallopSpeed = 7f;
        public float _acceleration = 5f;
        public float _decelerationDuringTurn = 2f; // Deceleration rate during a turn

        bool signifigantTurning = false;

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
            GroundedCheck();
            GravityControl();
            HandleSliding();

            if (_currentGait == Gait.Idle)
            {
                _rigidbody.velocity = Vector3.Lerp(_rigidbody.velocity, Vector3.zero, Time.fixedDeltaTime * _acceleration);
            }

            // Debug the grounded state
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
            if (Grounded)
                Move(moveInput, targetSpeed, _mainCamera.transform.rotation, rotateCharacter);
        }

        public void Move(Vector2 moveInput, float targetSpeed, Quaternion cameraRotation, bool rotateCharacter = true)
        {
            if (moveInput == Vector2.zero)
            {
                _currentGait = Gait.Idle;
                _rigidbody.velocity = Vector3.Lerp(_rigidbody.velocity, Vector3.zero, Time.deltaTime * _acceleration);
                if (_hasAnimator)
                {
                    _animator.SetFloat(_animIDSpeed, 0);
                    _animator.SetFloat(_animIDMotionSpeed, 0);
                    _animator.SetBool("leftTurn", false);
                    _animator.SetBool("rightTurn", false);
                }
                _isTurning = false;
                _animator.SetLayerWeight(2, Mathf.Lerp(_animator.GetLayerWeight(2), 0, 0.01f));
                return;
            }

            _currentGait = DetermineGait(targetSpeed);
            float gaitSpeed = GetGaitSpeed();

            float currentHorizontalSpeed = new Vector3(_rigidbody.velocity.x, 0.0f, _rigidbody.velocity.z).magnitude;
            float speedOffset = 0.1f;
            float inputMagnitude = moveInput.magnitude;

            if (inputMagnitude > 1) inputMagnitude = 1f;

            if (currentHorizontalSpeed < gaitSpeed - speedOffset || currentHorizontalSpeed > gaitSpeed + speedOffset)
            {
                _speed = Mathf.Lerp(currentHorizontalSpeed, gaitSpeed * inputMagnitude, Time.deltaTime * _acceleration);
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = gaitSpeed;
            }

            Vector3 inputDirection = new Vector3(moveInput.x, 0.0f, moveInput.y).normalized;

            bool isRotating = false;
            if (moveInput != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);

                if (rotateCharacter && !_useRotationRootMotion)
                {
                    transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
                    isRotating = Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.y, _targetRotation)) > 110.0f; // Significant rotation threshold
                    float angleDifference = Mathf.DeltaAngle(transform.eulerAngles.y, _targetRotation);

                    // Trigger turn animation if significant rotation is detected and not already turning
                    if (isRotating && !_isTurning)
                    {
                        _animator.CrossFadeInFixedTime("Turn", 0.1f);
                        _animator.SetLayerWeight(2, 0);
                        _isTurning = true;
                        signifigantTurning = true;
                        StartCoroutine(WaitForTurn());
                    } 
                    else if(!signifigantTurning)
                        _animator.SetLayerWeight(2, Mathf.Lerp(_animator.GetLayerWeight(2), 1, 0.01f));


                    if (!signifigantTurning)
                    {
                        if (Mathf.Abs(angleDifference) > 10) // Threshold of 5 degrees to determine "not facing forward"
                        {
                            _animator.SetBool("leftTurn", angleDifference > 0);
                            _animator.SetBool("rightTurn", angleDifference < 0);
                        }
                        else
                        {
                            _animator.SetBool("leftTurn", false);
                            _animator.SetBool("rightTurn", false);
                        }
                    }
                }
            }

            if (!isRotating)
            {
                _isTurning = false;
                Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;
                Vector3 velocity = targetDirection.normalized * _speed;
                velocity.y = _rigidbody.velocity.y;

                if (!_useRootMotion)
                {
                    _rigidbody.velocity = velocity;
                }
            }
            else
            {
                // Gradually reduce forward movement during significant rotation
                Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;
                Vector3 velocity = Vector3.zero; //targetDirection.normalized * _speed * (1 - Time.deltaTime * _decelerationDuringTurn);
                velocity.y = _rigidbody.velocity.y;

                if (!_useRootMotion)
                {
                    _rigidbody.velocity = velocity;
                }

                // Gradually reduce speed
                _speed = Mathf.Lerp(_speed, 0, Time.deltaTime * _decelerationDuringTurn);
            }

            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _speed);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

        IEnumerator WaitForTurn()
        {
            yield return new WaitForSeconds(1.5f);
            signifigantTurning = false;
        }

        private Gait DetermineGait(float targetSpeed)
        {
            if (targetSpeed >= _gallopSpeed)
                return Gait.Gallop;
            else if (targetSpeed > 0)
                return Gait.Trot;
            else
                return Gait.Idle;
        }

        private float GetGaitSpeed()
        {
            switch (_currentGait)
            {
                case Gait.Gallop: return _gallopSpeed;
                case Gait.Trot: return _trotSpeed;
                case Gait.Idle:
                default: return 0f;
            }
        }

        public void Move(Vector3 velocity)
        {
            if (_rigidbody.useGravity)
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

            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, 0);
                _animator.SetFloat(_animIDMotionSpeed, 0);
            }
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
    }
}
