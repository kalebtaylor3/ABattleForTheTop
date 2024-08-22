using System.Collections;
using UnityEngine;
using UnityEngine.UI; // For visual feedback (e.g., progress bar)

public class StandPlatform : MonoBehaviour
{
    public float confirmTime = 3f; // Time required to confirm the action
    private float currentTime = 0f;
    private bool playerOnPlatform = false;
    public Image progressBar; // Optional: Visual feedback for confirmation progress

    private DealerIK dealer;

    private void Start()
    {
        dealer = FindObjectOfType<DealerIK>(); // Find the Dealer in the scene
        progressBar.fillAmount = 0f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerOnPlatform = true;
            StartCoroutine(ConfirmAction());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerOnPlatform = false;
            currentTime = 0f;
            progressBar.fillAmount = 0f; // Reset visual feedback
        }
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
            PerformStand();
        }
    }

    private void PerformStand()
    {
        // Logic for standing (dealer does not deal any more cards to the player)
        // This could involve ending the player's turn or triggering dealer logic for their turn

        // Reset the platform state
        currentTime = 0f;
        progressBar.fillAmount = 0f;
    }
}
