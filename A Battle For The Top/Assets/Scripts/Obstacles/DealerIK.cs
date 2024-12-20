using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BFTT.IK;
using System.Threading;
using Cinemachine;

public class DealerIK : MonoBehaviour
{
    public Transform leftHandTargetIdle;
    public Transform rightHandTargetIdle;
    public Transform spineTargetIdle;

    public Transform leftHandTargetMiddle;
    public Transform rightHandTargetMiddle;
    public Transform spineTargetMiddle;

    public Transform leftHandTargetLost;
    public Transform rightHandTargetLost;
    public Transform spineTargetLost;

    public Transform neckTargetDown;
    public Transform neckTargetUp;

    public Transform rightHandTargetDeal;
    public Transform rightHandTargetToss;

    public Transform tableSlamIK;

    private IKScheduler _scheduler;
    private Dealer _dealer;
    private bool dealerLost = false;
    private bool dealerWon = false; 
    private bool transitionComplete = false;
    private bool returningToIdle = false;
    private bool isDealing = false;
    private float transitionProgress = 0f;
    private float neckBangProgress = 0f;
    private float elapsedTime = 0f;

    [SerializeField] private float transitionDuration = 1.0f;
    [SerializeField] private float returnToIdleDuration = 0.5f;
    [SerializeField] private float neckBangDuration = 0.5f;
    [SerializeField] private float maxBangTime = 3.0f;
    [SerializeField] private float handMoveToDealDuration = 0.7f;
    [SerializeField] private float handMoveToTossDuration = 0.5f;
    [SerializeField] private float holdDealPoseDuration = 0.5f;
    [SerializeField] private float handReturnDuration = 0.5f;

    public enum GameState { PlayerTurn, DealerTurn, GameOver }
    public GameState currentState;

    public HitPlatform hitPlatform;
    public StandPlatform standPlatform;
    private Animator _animator;
    public CinemachineVirtualCamera virtualCamera;
    private CinemachineBasicMultiChannelPerlin cameraNoise;

    private CancellationTokenSource _cancellationTokenSource;

    bool happenOnce = false;

    private void Awake()
    {
        _scheduler = GetComponent<IKScheduler>();
        _dealer = GetComponent<Dealer>();
        _animator = GetComponent<Animator>();
        _cancellationTokenSource = new CancellationTokenSource();
        cameraNoise = virtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        cameraNoise.m_AmplitudeGain = 0f;
        cameraNoise.m_FrequencyGain = 0f;
    }

    private void Start()
    {
        // Initial setup if needed
    }

    private void Update()
    {
        if (isDealing)
        {
            HandleDealAnimation();
        }
        else
        {
            HandleTransitions();
        }
    }

    private void HandleDealAnimation()
    {
        // Additional IK handling during the dealing sequence if necessary
    }

    public void StartDealingSequence(bool initialDeal, bool dealToBoth)
    {
        if (currentState != GameState.GameOver)
        {
            _cancellationTokenSource = new CancellationTokenSource(); // Create a new token source each time we start dealing
            StartCoroutine(DealCardSequence(initialDeal, dealToBoth, _cancellationTokenSource.Token));
        }
    }

    private IEnumerator DealCardSequence(bool initialDeal, bool dealToBoth, CancellationToken cancellationToken)
    {
        isDealing = true;

        // Disable player actions while dealing cards
        hitPlatform.canHit = false;
        standPlatform.canStand = false;
        hitPlatform.canReset = false;
        standPlatform.canReset = false;
        hitPlatform.EnableColliders();
        standPlatform.EnableColliders();

        if (initialDeal)
        {
            // Deal two cards to player first
            currentState = GameState.PlayerTurn;
            yield return DealCard(true, cancellationToken);
            yield return new WaitForSeconds(0.1f);
            yield return DealCard(true, cancellationToken);

            // Deal two cards to dealer
            currentState = GameState.DealerTurn;
            yield return DealCard(false, cancellationToken);
            yield return new WaitForSeconds(0.1f);
            yield return DealCard(false, cancellationToken);

            // Back to PlayerTurn for player actions
                if(currentState != GameState.GameOver)
                    currentState = GameState.PlayerTurn;
        }
        else if (dealToBoth)
        {
            // Deal one card to player
            currentState = GameState.PlayerTurn;
            yield return DealCard(true, cancellationToken);

            // Deal one card to dealer
            yield return new WaitForSeconds(0.1f);
            currentState = GameState.DealerTurn;
            yield return DealCard(false, cancellationToken);
        }
        else
        {
            // Handle dealing only to the player
            if (currentState == GameState.PlayerTurn)
            {
                yield return DealCard(true, cancellationToken);
            }
            // Handle dealing only to the dealer
            else if (currentState == GameState.DealerTurn)
            {
                yield return DealCard(false, cancellationToken);
            }
        }

        // Re-enable player actions after dealing
        if (currentState != GameState.GameOver)
        {
            hitPlatform.canReset = true;
            standPlatform.canReset = true;
        }

        isDealing = false;
    }


    private IEnumerator DealCard(bool dealToPlayer, CancellationToken cancellationToken)
    {
        yield return MoveHandToPosition(rightHandTargetIdle, rightHandTargetDeal, handMoveToDealDuration, cancellationToken);

        if (cancellationToken.IsCancellationRequested) yield break;

        _dealer.SpawnCardInHand();

        yield return new WaitForSeconds(holdDealPoseDuration);

        if (cancellationToken.IsCancellationRequested) yield break;

        yield return MoveHandToPosition(rightHandTargetDeal, rightHandTargetToss, handMoveToTossDuration, cancellationToken);

        if (cancellationToken.IsCancellationRequested) yield break;

        if (dealToPlayer)
        {
            _dealer.MoveLastCardToPlayerPosition();
            DealerCard lastCard = _dealer.GetLastCard();
            _dealer.AddCardToPlayerHand(lastCard);
        }
        else
        {
            _dealer.MoveLastCardToDealerPosition();
            DealerCard lastCard = _dealer.GetLastCard();
            _dealer.AddCardToDealerHand(lastCard);
        }

        if (cancellationToken.IsCancellationRequested) yield break;

        yield return MoveHandToPosition(rightHandTargetToss, rightHandTargetIdle, handReturnDuration, cancellationToken);
    }

    private IEnumerator MoveHandToPosition(Transform start, Transform target, float duration, CancellationToken cancellationToken)
    {
        Vector3 startPos = start.position;
        Quaternion startRot = start.rotation;
        Vector3 endPos = target.position;
        Quaternion endRot = target.rotation;

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            if (cancellationToken.IsCancellationRequested) yield break;

            float t = elapsedTime / duration;
            Vector3 currentPos = Vector3.Lerp(startPos, endPos, t);
            Quaternion currentRot = Quaternion.Slerp(startRot, endRot, t);

            IKPass rightHandPass = new IKPass(currentPos, currentRot, AvatarIKGoal.RightHand, 1, 1);
            _scheduler.ApplyIK(rightHandPass);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (cancellationToken.IsCancellationRequested) yield break;

        IKPass finalPass = new IKPass(endPos, endRot, AvatarIKGoal.RightHand, 1, 1);
        _scheduler.ApplyIK(finalPass);
    }

    private void HandleTransitions()
    {
        if (dealerLost && !returningToIdle && transitionProgress < 1f)
        {
            transitionProgress += Time.deltaTime / transitionDuration;
        }
        else if (returningToIdle && transitionProgress > 0f)
        {
            transitionProgress -= Time.deltaTime / returnToIdleDuration;
        }

        transitionComplete = transitionProgress >= 1f;

        Vector3 leftHandPos, rightHandPos;
        Quaternion leftHandRot, rightHandRot, spineRot, neckRot;

        if (!returningToIdle && transitionProgress < 0.5f)
        {
            float t = transitionProgress * 2f;
            leftHandPos = Vector3.Lerp(leftHandTargetIdle.position, leftHandTargetMiddle.position, t);
            leftHandRot = Quaternion.Slerp(leftHandTargetIdle.rotation, leftHandTargetMiddle.rotation, t);
            rightHandPos = Vector3.Lerp(rightHandTargetIdle.position, rightHandTargetMiddle.position, t);
            rightHandRot = Quaternion.Slerp(rightHandTargetIdle.rotation, rightHandTargetMiddle.rotation, t);
            spineRot = Quaternion.Slerp(spineTargetIdle.rotation, spineTargetMiddle.rotation, t);
        }
        else if (!returningToIdle)
        {
            float t = (transitionProgress - 0.5f) * 2f;
            leftHandPos = Vector3.Lerp(leftHandTargetMiddle.position, leftHandTargetLost.position, t);
            leftHandRot = Quaternion.Slerp(leftHandTargetMiddle.rotation, leftHandTargetLost.rotation, t);
            rightHandPos = Vector3.Lerp(rightHandTargetMiddle.position, rightHandTargetLost.position, t);
            rightHandRot = Quaternion.Slerp(rightHandTargetMiddle.rotation, rightHandTargetLost.rotation, t);
            spineRot = Quaternion.Slerp(spineTargetMiddle.rotation, spineTargetLost.rotation, t);

            if (t >= 1.0f)  // When the hands reach the lost positions
            {
                if (!happenOnce)
                {
                    _dealer.TriggerCardPhysics();
                    ApplyCameraShake(2f, 2f, 0.5f); // Shake with amplitude 2, frequency 2, for 0.5 seconds
                    happenOnce = true;
                }
            }
        }
        else
        {
            float t = 1f - transitionProgress;
            leftHandPos = Vector3.Lerp(leftHandTargetLost.position, leftHandTargetIdle.position, t);
            leftHandRot = Quaternion.Slerp(leftHandTargetLost.rotation, leftHandTargetIdle.rotation, t);
            rightHandPos = Vector3.Lerp(rightHandTargetLost.position, rightHandTargetIdle.position, t);
            rightHandRot = Quaternion.Slerp(rightHandTargetLost.rotation, rightHandTargetIdle.rotation, t);
            spineRot = Quaternion.Slerp(spineTargetLost.rotation, spineTargetIdle.rotation, t);
        }

        if (!dealerWon)
        {
            IKPass rightHandPass = new IKPass(rightHandPos, rightHandRot, AvatarIKGoal.RightHand, 1, 1);
            _scheduler.ApplyIK(rightHandPass);

            IKPass leftHandPass = new IKPass(leftHandPos, leftHandRot, AvatarIKGoal.LeftHand, 1, 1);
            _scheduler.ApplyIK(leftHandPass);
        }

        SpineIKPass spinePass = new SpineIKPass(Vector3.zero, spineRot, HumanBodyBones.Spine, 0, 1);
        _scheduler.ApplySpineIK(spinePass);

        // Smooth transition for neck position when returning to idle
        if (dealerLost && transitionComplete && elapsedTime < maxBangTime)
        {
            neckBangProgress += Time.deltaTime / neckBangDuration;
            elapsedTime += Time.deltaTime;

            float t = Mathf.PingPong(neckBangProgress, 1f);
            neckRot = Quaternion.Slerp(neckTargetUp.rotation, neckTargetDown.rotation, t);
        }
        else
        {
            // Smooth return to idle position
            float t = Time.deltaTime / neckBangDuration;
            neckRot = Quaternion.Slerp(Quaternion.identity, neckTargetUp.rotation, t);
        }

        NeckIKPass neckPass = new NeckIKPass(Vector3.zero, neckRot, HumanBodyBones.Neck, 0, 1);
        _scheduler.ApplyNeckIK(neckPass);
    }


    public void DealerLose()
    {
        if (currentState == GameState.GameOver) return;

        hitPlatform.DisableColliders();
        standPlatform.DisableColliders();
        _cancellationTokenSource.Cancel();
        dealerLost = true;
        dealerWon = false;
        transitionProgress = 0f;
        neckBangProgress = 0f;
        elapsedTime = 0f;
        returningToIdle = false;
        hitPlatform.canHit = false;
        currentState = GameState.GameOver;
    }

    public void DealerWin()
    {
        if (currentState == GameState.GameOver) return;

        hitPlatform.DisableColliders();
        standPlatform.DisableColliders();
        _cancellationTokenSource.Cancel();
        dealerLost = false;
        dealerWon = true;
        returningToIdle = false;
        transitionProgress = 0f;
        _scheduler.StopIK(AvatarIKGoal.RightHand);
        _scheduler.StopIK(AvatarIKGoal.LeftHand);
        _animator.CrossFade("WinState", 0.1f);
        currentState = GameState.GameOver;
    }

    public void ResetToIdle(bool dealerWin)
    {
        dealerLost = false;
        dealerWon = false;
        if (!dealerWin)
        {
            returningToIdle = true;
            transitionProgress = 1f;
        }
        _animator.CrossFade("Grounded", 0.1f);
        currentState = GameState.PlayerTurn; // Or reset to a neutral state if applicable
    }

    public void ResetGame(bool dealerWin)
    {
        ResetToIdle(dealerWin);
        _dealer.ResetDeckAndHands();
        happenOnce = false;
    }

    public void ApplyCameraShake(float amplitude, float frequency, float duration)
    {
        if (cameraNoise == null) return;

        StartCoroutine(ShakeCameraRoutine(amplitude, frequency, duration));
    }

    private IEnumerator ShakeCameraRoutine(float amplitude, float frequency, float duration)
    {
        if (cameraNoise != null)
        {
            cameraNoise.m_AmplitudeGain = amplitude;
            cameraNoise.m_FrequencyGain = frequency;

            yield return new WaitForSeconds(duration);

            cameraNoise.m_AmplitudeGain = 0f;
            cameraNoise.m_FrequencyGain = 0f;
        }
    }

    public void SlamIK()
    {
        IKPass rightHandPass = new IKPass(tableSlamIK.position, tableSlamIK.rotation, AvatarIKGoal.RightHand, 1, 1);
        _scheduler.ApplyIK(rightHandPass);
    }

    public void RestSlamIK()
    {
        _scheduler.StopIK(AvatarIKGoal.RightHand);
    }
}
