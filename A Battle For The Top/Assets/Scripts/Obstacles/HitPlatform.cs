using System.Collections;
using UnityEngine;
using UnityEngine.UI; // For visual feedback (e.g., progress bar)

public class HitPlatform : MonoBehaviour
{
    public float confirmTime = 3f; // Time required to confirm the action
    private float currentTime = 0f;
    private bool playerOnPlatform = false;
    public Image progressBar; // Optional: Visual feedback for confirmation progress
    [HideInInspector] public bool canHit = true;
    [HideInInspector] public bool canReset = true;

    public BoxCollider[] colliders;

    private DealerIK dealer;

    private void Start()
    {
        dealer = FindObjectOfType<DealerIK>(); // Find the Dealer in the scene
        progressBar.fillAmount = 0f;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the game is over or if hitting is allowed
        if (other.CompareTag("Player") && canHit && dealer.currentState != DealerIK.GameState.GameOver)
        {
            playerOnPlatform = true;
            StartCoroutine(ConfirmAction());
            canHit = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Check if the game is over or if resetting is allowed
        if (other.CompareTag("Player") && canReset && dealer.currentState != DealerIK.GameState.GameOver)
        {
            playerOnPlatform = false;
            currentTime = 0f;
            progressBar.fillAmount = 0f; // Reset visual feedback
            canHit = true;
        }
    }

    public void DisableColliders()
    {
        colliders[1].enabled = false;
    }

    public void EnableColliders()
    {
        colliders[1].enabled = true;
    }

    private IEnumerator ConfirmAction()
    {
        while (playerOnPlatform && currentTime < confirmTime)
        {
            currentTime += Time.deltaTime;
            progressBar.fillAmount = currentTime / confirmTime; // Update visual feedback
            yield return null;
        }

        if (currentTime >= confirmTime)
        {
            PerformHit();
        }
    }

    private void PerformHit()
    {
        // Dealer logic to deal a new card to the player
        dealer.StartDealingSequence(false, false);

        // Reset the platform state
        currentTime = 0f;
        progressBar.fillAmount = 0f;
    }
}
