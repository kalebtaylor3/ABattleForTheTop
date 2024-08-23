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
    private List<DealerCard> playerHand = new List<DealerCard>(); // Player's hand
    private List<DealerCard> dealerHand = new List<DealerCard>(); // Dealer's hand

    private DealerIK _dealer;

    private void Awake()
    {
        _dealer = GetComponent<DealerIK>();
    }

    private void Start()
    {
        CreateDeck();
        ShuffleDeck();
    }

    public void StartGame()
    {
        _dealer.StartDealingSequence(true);
    }

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

    public void StandAndDraw()
    {
        StartCoroutine(DealerTurnRoutine());
    }

    private IEnumerator DealerTurnRoutine()
    {
        _dealer.currentState = DealerIK.GameState.DealerTurn;

        // Dealer must keep drawing until they reach at least 17
        while (CalculateHandValue(dealerHand) < 17)
        {
            _dealer.StartDealingSequence(false);
            yield return new WaitForSeconds(4f); // Optional: Add a short delay between each card dealt
        }

        // Once dealer reaches 17 or higher, compare hands
        int playerValue = CalculateHandValue(playerHand);
        int dealerValue = CalculateHandValue(dealerHand);

        if (dealerValue > 21 || playerValue > dealerValue)
        {
            EndGame(true); // Player wins
        }
        else
        {
            EndGame(false); // Dealer wins
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
            GameObject card = dealtCards[dealtCards.Count - 1];
            card.transform.SetParent(null);
            StartCoroutine(MoveCardToPosition(card, GetCardStackPosition(playerCardPosition), finalCardRotation));
        }
    }

    public void MoveLastCardToDealerPosition()
    {
        if (dealtCards.Count > 0)
        {
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
            card.transform.rotation = Quaternion.RotateTowards(card.transform.rotation, targetRotation, cardMoveSpeed * Time.deltaTime * 10f);
            yield return null;
        }
    }

    private Vector3 GetCardStackPosition(Transform basePosition)
    {
        Vector3 offset = new Vector3(0, 0.5f * dealtCards.Count, 0); // Adjust the offset as needed
        return basePosition.position + offset;
    }

    public DealerCard GetLastCard()
    {
        if (dealtCards.Count > 0)
        {
            return dealtCards[dealtCards.Count - 1].GetComponent<DealerCard>();
        }
        return null;
    }

    public void AddCardToPlayerHand(DealerCard card)
    {
        playerHand.Add(card);
        CheckPlayerState();
    }

    public void AddCardToDealerHand(DealerCard card)
    {
        dealerHand.Add(card);
        if (_dealer.currentState == DealerIK.GameState.DealerTurn)
        {
            CheckDealerState();
        }
    }

    private void CheckPlayerState()
    {
        int playerValue = CalculateHandValue(playerHand);
        Debug.Log("Player" + playerValue);

        if (playerValue > 21)
        {
            EndGame(false);
        }
        else if (playerValue == 21)
        {
            EndGame(true);
        }
    }

    private void CheckDealerState()
    {
        int dealerValue = CalculateHandValue(dealerHand);
        Debug.Log("Dealer: " + dealerValue);

        if (dealerValue > 21)
        {
            EndGame(true);
        }
        
    }

    private void EndGame(bool playerWon)
    {
        _dealer.currentState = DealerIK.GameState.GameOver;

        if (playerWon)
        {
            Debug.Log("Player Wins!");
            _dealer.DealerLose();
            //setup logic to reset
        }
        else
        {
            Debug.Log("Dealer Wins!");
            _dealer.DealerLose();
        }

        // Reset hands and start a new game if needed
        //playerHand.Clear();
        //dealerHand.Clear();
        //StartGame();
    }

    private int CalculateHandValue(List<DealerCard> hand)
    {
        int value = 0;
        int aces = 0;

        foreach (var card in hand)
        {
            if (card.value > 10)  // 10, J, Q, K are worth 10 points
            {
                value += 10;
            }
            else if (card.value == 1)  // Aces are worth 11 initially
            {
                aces++;
                value += 11;
            }
            else  // Cards 2 through 10 are worth their face value
            {
                value += card.value;
            }
        }

        // Adjust for aces if the total value exceeds 21
        while (value > 21 && aces > 0)
        {
            value -= 10;  // Count ace as 1 instead of 11
            aces--;
        }
        return value;
    }

}
