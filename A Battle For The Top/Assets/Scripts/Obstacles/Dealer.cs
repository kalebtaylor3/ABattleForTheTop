using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dealer : MonoBehaviour
{
    public GameObject cardPrefab; // Prefab of the card to be dealt
    public Transform handPosition; // Position where the card is spawned in the dealer's hand
    public Transform tableCenter; // The center position on the table where cards are placed
    public float cardMoveSpeed = 5f; // Speed at which the card moves to the table
    public Quaternion finalCardRotation = Quaternion.Euler(0, 0, 0); // Final rotation of the card on the table

    private List<GameObject> dealtCards = new List<GameObject>(); // List to keep track of dealt cards

    public void SpawnCardInHand()
    {
        // Instantiate the card at the hand position
        GameObject card = Instantiate(cardPrefab, handPosition.position, handPosition.rotation);
        card.transform.SetParent(handPosition);
        dealtCards.Add(card);
    }

    public void MoveLastCardToTable()
    {
        if (dealtCards.Count > 0)
        {
            // Get the last card dealt
            GameObject card = dealtCards[dealtCards.Count - 1];
            card.transform.SetParent(null);
            StartCoroutine(MoveCardToPosition(card, GetCardStackPosition(), finalCardRotation));
        }
    }

    private IEnumerator MoveCardToPosition(GameObject card, Vector3 targetPosition, Quaternion targetRotation)
    {
        while (Vector3.Distance(card.transform.position, targetPosition) > 0.01f ||
               Quaternion.Angle(card.transform.rotation, targetRotation) > 0.01f)
        {
            card.transform.position = Vector3.MoveTowards(card.transform.position, targetPosition, cardMoveSpeed * Time.deltaTime);
            card.transform.rotation = Quaternion.RotateTowards(card.transform.rotation, targetRotation, cardMoveSpeed * Time.deltaTime * 10f); // Adjust rotation speed if necessary
            yield return null;
        }
    }

    private Vector3 GetCardStackPosition()
    {
        // Calculate the position for the next card, with a slight offset for stacking
        Vector3 offset = new Vector3(0, 0.01f * dealtCards.Count, 0); // Adjust the offset as needed
        return tableCenter.position + offset;
    }
}
