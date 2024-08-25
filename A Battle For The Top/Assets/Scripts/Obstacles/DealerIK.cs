using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BFTT.IK;
using System.Threading;

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

    private CancellationTokenSource _cancellationTokenSource;

    private void Awake()
    {
        _scheduler = GetComponent<IKScheduler>();
        _dealer = GetComponent<Dealer>();
        _animator = GetComponent<Animator>();
        _cancellationTokenSource = new CancellationTokenSource();
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

    public void StartDealingSequence(bool both)
    {
        if (currentState != GameState.GameOver)
        {
            _cancellationTokenSource = new CancellationTokenSource(); // Create a new token source each time we start dealing
            StartCoroutine(DealCardSequence(both, _cancellationTokenSource.Token));
        }
    }

    private IEnumerator DealCardSequence(bool both, CancellationToken cancellationToken)
    {
        isDealing = true;

        hitPlatform.canHit = false;
        standPlatform.canStand = false;
        hitPlatform.canReset = false;
        standPlatform.canReset = false;
        hitPlatform.EnableColliders();
        standPlatform.EnableColliders();

        if (both)
        {
            currentState = GameState.PlayerTurn;
            yield return DealCard(true, cancellationToken);
        }

        if (both)
            yield return new WaitForSeconds(0.1f);

        currentState = GameState.DealerTurn;
        yield return DealCard(false, cancellationToken);

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
        Quaternion leftHandRot, rightHandRot, spineRot;

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

        if (dealerLost && transitionComplete && elapsedTime < maxBangTime)
        {
            neckBangProgress += Time.deltaTime / neckBangDuration;
            elapsedTime += Time.deltaTime;

            float t = Mathf.PingPong(neckBangProgress, 1f);

            Quaternion neckRot = Quaternion.Slerp(neckTargetUp.rotation, neckTargetDown.rotation, t);
            NeckIKPass neckPass = new NeckIKPass(Vector3.zero, neckRot, HumanBodyBones.Neck, 0, 1);
            _scheduler.ApplyNeckIK(neckPass);
        }
        else
        {
            NeckIKPass neckPass = new NeckIKPass(Vector3.zero, neckTargetUp.rotation, HumanBodyBones.Neck, 0, 1);
            _scheduler.ApplyNeckIK(neckPass);
        }
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
    }
}
