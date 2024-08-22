using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dealer : MonoBehaviour
{
    public GameObject dealerCardPrefab; // Prefab of the DealerCard to be dealt
    public Transform handPosition; // Position where the card is spawned in the dealer's hand
    public Transform playerCardPosition; // Position where the player's cards are placed on the table
    public Transform dealerCardPosition; // Position where the dealer's cards are placed on the table
    public float cardMoveSpeed = 5f; // Speed at which the card moves to the table
    public Quaternion finalCardRotation = Quaternion.Euler(0, 0, 0); // Final rotation of the card on the table

    private List<DealerCard> deck = new List<DealerCard>(); // The deck of cards
    private List<GameObject> dealtCards = new List<GameObject>(); // List to keep track of dealt cards

    private void Start()
    {
        CreateDeck();
        ShuffleDeck();
    }

    // Create a standard 52-card deck
    private void CreateDeck()
    {
        string[] suits = { "Hearts", "Diamonds", "Clubs", "Spades" };
        for (int i = 1; i <= 13; i++) // 1 to 13 for Ace to King
        {
            foreach (string suit in suits)
            {
                DealerCard card = new DealerCard();
                card.suit = suit;
                card.value = i;
                deck.Add(card);
            }
        }
    }

    // Shuffle the deck
    private void ShuffleDeck()
    {
        for (int i = 0; i < deck.Count; i++)
        {
            DealerCard temp = deck[i];
            int randomIndex = Random.Range(0, deck.Count);
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }
    }

    // Deal the top card from the deck
    public void SpawnCardInHand()
    {
        if (deck.Count > 0)
        {
            DealerCard cardData = deck[0]; // Take the top card
            deck.RemoveAt(0); // Remove it from the deck

            // Instantiate the card at the hand position
            GameObject cardObject = Instantiate(dealerCardPrefab, handPosition.position, handPosition.rotation);
            cardObject.transform.SetParent(handPosition);

            // Assign the card data to the DealerCard component
            DealerCard dealerCardComponent = cardObject.GetComponent<DealerCard>();
            dealerCardComponent.suit = cardData.suit;
            dealerCardComponent.value = cardData.value;

            // Update the card visuals
            dealerCardComponent.UpdateCardVisuals();

            dealtCards.Add(cardObject);
        }
    }

    public void MoveLastCardToPlayerPosition()
    {
        if (dealtCards.Count > 0)
        {
            // Get the last card dealt
            GameObject card = dealtCards[dealtCards.Count - 1];
            card.transform.SetParent(null);
            StartCoroutine(MoveCardToPosition(card, GetCardStackPosition(playerCardPosition), finalCardRotation));
        }
    }

    public void MoveLastCardToDealerPosition()
    {
        if (dealtCards.Count > 0)
        {
            // Get the last card dealt
            GameObject card = dealtCards[dealtCards.Count - 1];
            card.transform.SetParent(null);
            StartCoroutine(MoveCardToPosition(card, GetCardStackPosition(dealerCardPosition), finalCardRotation));
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

    private Vector3 GetCardStackPosition(Transform basePosition)
    {
        // Calculate the position for the next card, with a slight offset for stacking
        Vector3 offset = new Vector3(0, 0.5f * dealtCards.Count, 0); // Adjust the offset as needed
        return basePosition.position + offset;
    }
}
