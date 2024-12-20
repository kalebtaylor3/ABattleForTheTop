using UnityEngine;
using BFTT.Components;

namespace BFTT.Abilities
{
    public class Roll : AbstractAbility
    {
        [SerializeField] private float rollSpeed = 7f;
        [SerializeField] private float capsuleHeightOnRoll = 1f;

        private IMover _mover = null;
        private ICapsule _capsule = null;

        // direction and rotation
        private Vector3 _rollDirection = Vector3.forward;
        private float _targetRotation = 0;
        private Transform _camera = null;

        [HideInInspector] public CharacterAudioPlayer _audioPlayer;

        public AudioClip rollClip;
        public AudioClip rollVoice;

        public RollCall rollCard;

        private void Awake()
        {
            _mover = GetComponent<IMover>();
            _capsule = GetComponent<ICapsule>();
            _audioPlayer = GetComponent<CharacterAudioPlayer>();
            _camera = Camera.main.transform;
        }

        public override bool ReadyToRun()
        {
            return rollCard.CombatReadyToRun() && _mover.IsGrounded() && _action.UseCard;
        }

        public override void OnStartAbility()
        {
            _animator.CrossFadeInFixedTime("Roll", 0.1f);
            _capsule.SetCapsuleSize(capsuleHeightOnRoll, _capsule.GetCapsuleRadius());

            if (_audioPlayer)
            {
                _audioPlayer.PlayEffect(rollClip);
                _audioPlayer.PlayVoice(rollVoice);
            }

            _rollDirection = transform.forward;
            _targetRotation = transform.eulerAngles.y;
            if(_action.move != Vector2.zero)
            {
                // normalise input direction
                Vector3 inputDirection = new Vector3(_action.move.x, 0.0f, _action.move.y).normalized;
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + _camera.transform.eulerAngles.y;
                _rollDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;
            }

            _rollDirection.Normalize();
        }

        public override void UpdateAbility()
        {
            _mover.Move(_rollDirection * rollSpeed);
            // smooth rotate character
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, _targetRotation, 0), 0.1f);

            if (_animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.95f && !_animator.IsInTransition(0))
                StopAbility();

        }

        public override void OnStopAbility()
        {
            _capsule.ResetCapsuleSize();
        }
    }
}