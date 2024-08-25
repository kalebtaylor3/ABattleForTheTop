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

    public BoxCollider[] colliders;


    private void Start()
    {
        dealerIK = FindObjectOfType<DealerIK>(); // Find the DealerIK in the scene
        dealer = FindObjectOfType<Dealer>(); // Get the Dealer component from DealerIK
        progressBar.fillAmount = 0f;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the game is over or if standing is allowed
        if (other.CompareTag("Player") && canStand && dealerIK.currentState != DealerIK.GameState.GameOver)
        {
            playerOnPlatform = true;
            StartCoroutine(ConfirmAction());
            canStand = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Check if the game is over or if resetting is allowed
        if (other.CompareTag("Player") && canReset && dealerIK.currentState != DealerIK.GameState.GameOver)
        {
            playerOnPlatform = false;
            currentTime = 0f;
            progressBar.fillAmount = 0f; // Reset visual feedback
            canStand = true;
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
