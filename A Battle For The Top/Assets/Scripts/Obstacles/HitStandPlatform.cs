using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitStandPlatform : MonoBehaviour
{
    public enum PlatformType { Hit, Stand }
    public PlatformType platformType;
    public float timeToConfirm = 3.0f;  // Time the player needs to stand on the platform to confirm

    private bool isPlayerOnPlatform = false;
    private float elapsedTime = 0f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerOnPlatform = true;
            elapsedTime = 0f; // Reset elapsed time
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerOnPlatform = false;
            elapsedTime = 0f; // Reset elapsed time
        }
    }

    private void Update()
    {
        if (isPlayerOnPlatform)
        {
            elapsedTime += Time.deltaTime;

            if (elapsedTime >= timeToConfirm)
            {
                ConfirmAction();
                isPlayerOnPlatform = false;
                elapsedTime = 0f; // Reset after confirming
            }
        }
    }

    private void ConfirmAction()
    {
        switch (platformType)
        {
            case PlatformType.Hit:
                Debug.Log("Player chose to Hit!");
                // Implement logic to deal a new card to the player
                // For example:
                //FindObjectOfType<Dealer>().DealCard();
                break;

            case PlatformType.Stand:
                Debug.Log("Player chose to Stand!");
                // Implement logic to end the player's turn and proceed with the dealer's turn
                break;
        }
    }
}
