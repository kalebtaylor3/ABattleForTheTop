using System.Collections;
using UnityEngine;
using UnityEngine.UI; // For visual feedback (e.g., progress bar)

public class StandPlatform : MonoBehaviour
{
    public float confirmTime = 3f; // Time required to confirm the action
    private float currentTime = 0f;
    private bool playerOnPlatform = false;
    public Image progressBar; // Optional: Visual feedback for confirmation progress

    private DealerIK dealerIK;
    private Dealer dealer;
    [HideInInspector] public bool canStand = true;
    [HideInInspector] public bool canReset = true;

    private void Start()
    {
        dealerIK = FindObjectOfType<DealerIK>(); // Find the DealerIK in the scene
        dealer = FindObjectOfType<Dealer>(); // Get the Dealer component from DealerIK
        progressBar.fillAmount = 0f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && canStand)
        {
            playerOnPlatform = true;
            StartCoroutine(ConfirmAction());
            canStand = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && canReset)
        {
            playerOnPlatform = false;
            currentTime = 0f;
            progressBar.fillAmount = 0f; // Reset visual feedback
            canStand = true;
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
        // Dealer's turn to draw cards
        dealer.StandAndDraw();

        // Reset the platform state
        currentTime = 0f;
        progressBar.fillAmount = 0f;
    }
}
