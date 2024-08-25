using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dealer : MonoBehaviour
{
    public GameObject dealerCardPrefab;
    public Transform handPosition;
    public Transform playerCardPosition;
    public Transform dealerCardPosition;
    public float cardMoveSpeed = 5f;
    public Quaternion finalCardRotation = Quaternion.Euler(0, 0, 0);

    private List<DealerCard> deck = new List<DealerCard>();
    private List<GameObject> dealtCards = new List<GameObject>();
    private List<DealerCard> playerHand = new List<DealerCard>();
    private List<DealerCard> dealerHand = new List<DealerCard>();

    private DealerIK _dealer;

    public float ResetTime; 

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
        if (_dealer.currentState == DealerIK.GameState.GameOver) return;
        _dealer.StartDealingSequence(true);
    }

    private void CreateDeck()
    {
        string[] suits = { "Hearts", "Diamonds", "Clubs", "Spades" };
        for (int i = 1; i <= 13; i++)
        {
            foreach (string suit in suits)
            {
                DealerCard card = new DealerCard
                {
                    suit = suit,
                    value = i
                };
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

    public void ResetDeckAndHands()
    {
        // Clear all cards on the table
        foreach (var card in dealtCards)
        {
            Destroy(card);
        }

        dealtCards.Clear();
        playerHand.Clear();
        dealerHand.Clear();

        // Reshuffle the deck
        ShuffleDeck();

        // Set the game state back to PlayerTurn
        _dealer.currentState = DealerIK.GameState.PlayerTurn;
        StartCoroutine(DealDelay());
    }

    IEnumerator DealDelay()
    {
        yield return new WaitForSeconds(2);
        _dealer.StartDealingSequence(true);
    }

    public void StandAndDraw()
    {
        if (_dealer.currentState == DealerIK.GameState.GameOver) return;
        StartCoroutine(DealerTurnRoutine());
    }

    private IEnumerator DealerTurnRoutine()
    {
        _dealer.currentState = DealerIK.GameState.DealerTurn;

        while (CalculateHandValue(dealerHand) < 17)
        {
            _dealer.StartDealingSequence(false);
            yield return new WaitForSeconds(4f);
        }

        int playerValue = CalculateHandValue(playerHand);
        int dealerValue = CalculateHandValue(dealerHand);

        if (dealerValue > 21 || playerValue > dealerValue)
        {
            EndGame(true);
        }
        else
        {
            EndGame(false);
        }
    }

    public void SpawnCardInHand()
    {
        if (_dealer.currentState == DealerIK.GameState.GameOver) return;

        if (deck.Count > 0)
        {
            DealerCard cardData = deck[0];
            deck.RemoveAt(0);

            GameObject cardObject = Instantiate(dealerCardPrefab, handPosition.position, handPosition.rotation);
            cardObject.transform.SetParent(handPosition);

            DealerCard dealerCardComponent = cardObject.GetComponent<DealerCard>();
            dealerCardComponent.suit = cardData.suit;
            dealerCardComponent.value = cardData.value;

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
            StartCoroutine(MoveCardToPosition(card, GetCardStackPosition(playerCardPosition), GetCardStackRotation(playerCardPosition)));
        }
    }

    public void MoveLastCardToDealerPosition()
    {
        if (dealtCards.Count > 0)
        {
            GameObject card = dealtCards[dealtCards.Count - 1];
            card.transform.SetParent(null);
            StartCoroutine(MoveCardToPosition(card, GetCardStackPosition(dealerCardPosition), GetCardStackRotation(dealerCardPosition)));
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
        Vector3 offset = new Vector3(0, 0.5f * dealtCards.Count, 0);
        return basePosition.position + offset;
    }

    private Quaternion GetCardStackRotation(Transform basePosition)
    {
        // No rotation offset for the first card
        if (dealtCards.Count == 1 || dealtCards.Count == 2)
        {
            return basePosition.rotation;
        }

        // Rotate each subsequent card slightly around the Y-axis based on its stack position
        float rotationAngle = -5f * (dealtCards.Count - 1); // Start applying rotation from the second card onward

        // Apply rotation around the Y-axis
        return Quaternion.Euler(0, rotationAngle, 0) * basePosition.rotation;
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
        Debug.Log("Player: " + playerValue);

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
        if (playerWon)
        {
            Debug.Log("Player Wins!");
            _dealer.DealerLose();
            StartCoroutine(WaitForReset(false));
        }
        else
        {
            Debug.Log("Dealer Wins!");
            _dealer.DealerWin();
            StartCoroutine(WaitForReset(true));
        }
    }

    IEnumerator WaitForReset(bool dealerWin)
    {
        yield return new WaitForSeconds(ResetTime);
        _dealer.ResetGame(dealerWin);
    }

    private int CalculateHandValue(List<DealerCard> hand)
    {
        int value = 0;
        int aces = 0;

        foreach (var card in hand)
        {
            if (card.value > 10)
            {
                value += 10;
            }
            else if (card.value == 1)
            {
                aces++;
                value += 11;
            }
            else
            {
                value += card.value;
            }
        }

        while (value > 21 && aces > 0)
        {
            value -= 10;
            aces--;
        }
        return value;
    }
}
