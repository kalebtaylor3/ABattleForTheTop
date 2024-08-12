using BFTT.Combat;
using BFTT.Controller;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using DragAndDrop;

public class CardManager : MonoBehaviour
{
    public AbstractCombat[] Cards; // Fixed size array with 5 elements
    public AbstractCombat currentCard;
    public PlayerController _controller;

    public RectTransform cardParent;
    public GameObject cardPrefab;
    public float tweenDuration = 0.5f;
    public Vector3 selectedScale = new Vector3(1.2f, 1.2f, 1.2f);
    public Vector3 deselectedScale = Vector3.one;
    public float selectedZPosition = -100f;
    public float deselectedZPosition = 0f;

    private List<RectTransform> cardRects = new List<RectTransform>();
    private int currentIndex = 0;

    public RectTransform scrollRect;

    public AbstractCombat[] deck = new AbstractCombat[20];

    public CardsArrayUI deckUI;
    public CardsArrayUI handUI;

    public enum CardList
    {
        InHand,
        Deck
    };

    public AbstractCombat[] GetCards(CardList list)
    {
        switch (list)
        {
            case CardList.InHand: return Cards;
            case CardList.Deck: return deck;
            default: return null;
        }
    }

    void Start()
    {
        currentCard = Cards[0];
        if (currentCard != null && currentCard.abilityProp)
            currentCard.abilityProp.SetActive(true);
        CenterInitialCard();
        InitializeCardUI();
        UpdateCardUI();
    }

    private void OnEnable()
    {
        FlameWand.OnWandBreak += RemoveCard;
        GrappleBeam.OnGrappleBreak += RemoveCard;
        Draggable.OnClick += SwapCard;
    }

    private void OnDisable()
    {
        FlameWand.OnWandBreak -= RemoveCard;
        GrappleBeam.OnGrappleBreak -= RemoveCard;
    }

    void SwapCard(AbstractCombat card)
    {
        int cardIndexInHand = -1;
        int cardIndexInDeck = -1;

        // Find the index of the card in the hand
        for (int i = 0; i < Cards.Length; i++)
        {
            if (Cards[i] == card)
            {
                cardIndexInHand = i;
                break;
            }
        }

        // Find the index of the card in the deck
        for (int i = 0; i < deck.Length; i++)
        {
            if (deck[i] == card)
            {
                cardIndexInDeck = i;
                break;
            }
        }

        if (cardIndexInHand >= 0) // The card is in the hand
        {
            // Find the first empty slot in the deck
            for (int i = 0; i < deck.Length; i++)
            {
                if (deck[i] == null)
                {
                    // Move the card to this empty slot in the deck
                    deck[i] = Cards[cardIndexInHand];
                    Cards[cardIndexInHand] = null;

                    // Update the UI
                    UpdateCardUI();
                    handUI.UpdateUI();
                    deckUI.UpdateUI();
                    return;
                }
            }
        }
        else if (cardIndexInDeck >= 0) // The card is in the deck
        {
            // Find the first empty slot in the hand
            for (int i = 0; i < Cards.Length; i++)
            {
                if (Cards[i] == null)
                {
                    // Move the card to this empty slot in the hand
                    Cards[i] = deck[cardIndexInDeck];
                    deck[cardIndexInDeck] = null;

                    // Update the UI
                    UpdateCardUI();
                    handUI.UpdateUI();
                    deckUI.UpdateUI();
                    return;
                }
            }
        }
    }


    void CenterInitialCard()
    {
        float initialOffset = (cardParent.rect.width / 2) - (currentIndex * 160f + 80f);
        cardParent.anchoredPosition = new Vector2(initialOffset, cardParent.anchoredPosition.y);
    }

    void Update()
    {
        if (_controller._scheduler.characterActions.NextCard && currentCard.ReadyToExit())
        {
            currentCard.StopCombat();
            if (currentCard.abilityProp)
                currentCard.abilityProp.SetActive(false);
            currentIndex = (currentIndex + 1) % Cards.Length;
            currentCard = Cards[currentIndex];
            if (currentCard != null && currentCard.abilityProp)
                currentCard.abilityProp.SetActive(true);

            UpdateCardUI();
        }

        if (_controller._scheduler.characterActions.PreviousCard && currentCard.ReadyToExit())
        {
            currentCard.StopCombat();
            if (currentCard.abilityProp)
                currentCard.abilityProp.SetActive(false);
            currentIndex = (currentIndex - 1 + Cards.Length) % Cards.Length;
            currentCard = Cards[currentIndex];
            if (currentCard != null && currentCard.abilityProp)
                currentCard.abilityProp.SetActive(true);

            UpdateCardUI();
        }
    }

    void InitializeCardUI()
    {
        for (int i = 0; i < Cards.Length; i++)
        {
            if (Cards[i] != null) // Check if the slot is not empty
            {
                GameObject cardObj = Instantiate(cardPrefab, cardParent);
                RectTransform cardRect = cardObj.GetComponent<RectTransform>();
                cardRects.Add(cardRect);

                // Assign cardIcon image
                Image cardIcon = cardRect.GetComponent<Image>();
                cardIcon.sprite = Cards[i].cardIcon;
            }
            else
            {
                cardRects.Add(null); // Maintain the list structure, but add null to represent an empty slot
            }
        }
    }

    public void UpdateCardUI()
    {
        float cardWidth = 140f; // Width of the card.
        float centerPositionX = (scrollRect.rect.width + (cardRects.Count * cardWidth)) / 2 + cardWidth / 2 - 150;

        for (int i = 0; i < cardRects.Count; i++)
        {
            // Handle cases where a new card is assigned to a previously null slot
            if (cardRects[i] == null && Cards[i] != null)
            {
                // Instantiate the UI for the new card
                GameObject cardObj = Instantiate(cardPrefab, cardParent);
                RectTransform cardRect = cardObj.GetComponent<RectTransform>();
                cardRects[i] = cardRect;

                // Assign cardIcon image
                Image cardIcon1 = cardRect.GetComponent<Image>();
                cardIcon1.sprite = Cards[i].cardIcon;

                // Set the initial state of the card UI
                cardRect.localScale = deselectedScale * 0.8f;
                cardRect.GetComponent<CanvasGroup>().alpha = 0;
                cardRect.gameObject.SetActive(false);
            }

            if (cardRects[i] == null) // Skip null entries
                continue;

            // Compute the offset index considering the current index as the center.
            int offsetIndex = (i - currentIndex + cardRects.Count) % cardRects.Count;
            float newPositionX = centerPositionX + (offsetIndex - cardRects.Count / 2) * cardWidth;

            Image cardIcon = cardRects[i].GetComponent<Image>();
            if (Cards[i] != null) // Check if the slot is not empty
            {
                cardIcon.sprite = Cards[i].cardIcon;
            }

            if (offsetIndex > cardRects.Count / 2)
            {
                newPositionX -= cardRects.Count * cardWidth;
            }

            bool isNeighbor = offsetIndex == 1 || offsetIndex == cardRects.Count - 1;

            if (i == currentIndex || isNeighbor)
            {
                cardRects[i].DOScale(i == currentIndex ? selectedScale : deselectedScale, tweenDuration);
                cardRects[i].DOAnchorPos3DZ(i == currentIndex ? selectedZPosition : deselectedZPosition, tweenDuration);
                cardRects[i].GetComponent<CanvasGroup>().DOFade(1, tweenDuration);
                cardRects[i].gameObject.SetActive(true);
            }
            else
            {
                cardRects[i].DOScale(deselectedScale * 0.8f, tweenDuration);
                cardRects[i].GetComponent<CanvasGroup>().DOFade(0, tweenDuration);
            }

            cardRects[i].DOAnchorPosX(newPositionX, tweenDuration).SetEase(Ease.OutQuad);
        }

        if (currentCard == null)
            currentCard = Cards[0];

        if (currentCard != null && currentCard.abilityProp != null)
            currentCard.abilityProp.SetActive(true);
    }



    public void AddCard(AbstractCombat newCard)
    {
        // Resize the Cards array
        AbstractCombat[] newCardsArray = new AbstractCombat[Cards.Length + 1];
        Cards.CopyTo(newCardsArray, 0);
        newCardsArray[Cards.Length] = newCard;
        Cards = newCardsArray;

        // Instantiate the new card UI
        GameObject cardObj = Instantiate(cardPrefab, cardParent);
        RectTransform cardRect = cardObj.GetComponent<RectTransform>();
        cardRects.Add(cardRect);

        // Assign cardIcon image
        Image cardIcon = cardRect.GetComponent<Image>();
        cardIcon.sprite = newCard.cardIcon;

        UpdateCardUI();
    }

    public void RemoveCard(AbstractCombat cardToRemove)
    {
        int index = System.Array.IndexOf(Cards, cardToRemove);
        if (index >= 0)
        {
            // Remove the card from the Cards array
            Cards[index] = null;

            // Remove the corresponding UI element
            RectTransform cardRect = cardRects[index];
            cardRects[index] = null; // Clear the UI reference
            Destroy(cardRect.gameObject); // Destroy the UI GameObject

            // If the removed card was the current card, move to the next available card
            if (currentIndex == index)
            {
                currentIndex = (currentIndex + 1) % Cards.Length;
                currentCard = Cards[currentIndex];
            }

            // If the current card is still null, find the next available card
            if (currentCard == null)
            {
                for (int i = 0; i < Cards.Length; i++)
                {
                    if (Cards[i] != null)
                    {
                        currentCard = Cards[i];
                        currentIndex = i;
                        break;
                    }
                }
            }

            // Update the deck if necessary
            for (int i = 0; i < deck.Length; i++)
            {
                if (deck[i] == cardToRemove)
                {
                    deck[i] = null; // Clear the reference in the deck
                    break;
                }
            }

            // Update the UI to reflect the changes
            UpdateCardUI();
            handUI.UpdateUI();
            deckUI.UpdateUI();

            // Activate the current card's ability, if applicable
            if (currentCard != null && currentCard.abilityProp)
            {
                currentCard.abilityProp.SetActive(true);
            }
        }
    }

}
