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
        // Ensure there's a valid list of active cards before proceeding
        List<int> activeIndices = new List<int>();
        for (int i = 0; i < Cards.Length; i++)
        {
            if (Cards[i] != null)
            {
                activeIndices.Add(i);
            }
        }

        if (activeIndices.Count == 0) return;

        if (_controller._scheduler.characterActions.NextCard && currentCard.ReadyToExit())
        {
            currentCard.StopCombat();
            if (currentCard.abilityProp)
                currentCard.abilityProp.SetActive(false);

            // Move to the next active card
            do
            {
                currentIndex = (currentIndex + 1) % activeIndices.Count;
            } while (Cards[activeIndices[currentIndex]] == null);

            currentCard = Cards[activeIndices[currentIndex]];
            if (currentCard != null && currentCard.abilityProp)
                currentCard.abilityProp.SetActive(true);

            UpdateCardUI();
        }

        if (_controller._scheduler.characterActions.PreviousCard && currentCard.ReadyToExit())
        {
            currentCard.StopCombat();
            if (currentCard.abilityProp)
                currentCard.abilityProp.SetActive(false);

            // Move to the previous active card
            do
            {
                currentIndex = (currentIndex - 1 + activeIndices.Count) % activeIndices.Count;
            } while (Cards[activeIndices[currentIndex]] == null);

            currentCard = Cards[activeIndices[currentIndex]];
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
        List<RectTransform> activeCardRects = new List<RectTransform>();

        // Collect all active (non-null) card rects
        for (int i = 0; i < Cards.Length; i++)
        {
            if (Cards[i] != null)
            {
                if (i >= cardRects.Count || cardRects[i] == null)
                {
                    // Instantiate UI for the current card if it doesn't already exist
                    GameObject cardObj = Instantiate(cardPrefab, cardParent);
                    RectTransform cardRect = cardObj.GetComponent<RectTransform>();

                    if (i < cardRects.Count)
                    {
                        cardRects[i] = cardRect;
                    }
                    else
                    {
                        cardRects.Add(cardRect);
                    }

                    // Assign cardIcon image
                    Image cardIcon = cardRect.GetComponent<Image>();
                    cardIcon.sprite = Cards[i].cardIcon;

                    // Set initial state
                    cardRect.localScale = deselectedScale * 0.8f;
                    cardRect.GetComponent<CanvasGroup>().alpha = 0;
                    cardRect.gameObject.SetActive(false);
                }
                activeCardRects.Add(cardRects[i]);
            }
            else if (i < cardRects.Count && cardRects[i] != null)
            {
                // If the card is null but the UI exists, remove the UI element
                Destroy(cardRects[i].gameObject);
                cardRects[i] = null;
            }
        }

        // No need to continue if there are no active cards
        if (activeCardRects.Count == 0)
        {
            if (currentCard != null)
            {
                if(currentCard.abilityProp)
                    currentCard.abilityProp.SetActive(false);
                currentCard.StopCombat();
            }
            currentCard = null;
            return;
        }

        // Correct the current index if it's out of bounds
        if (currentIndex >= activeCardRects.Count)
        {
            currentIndex = activeCardRects.Count - 1;
        }

        float centerPositionX = (scrollRect.rect.width + (activeCardRects.Count * cardWidth)) / 2 + cardWidth / 2 - 150;

        // Update positions and visibility for all active cards
        for (int i = 0; i < activeCardRects.Count; i++)
        {
            RectTransform cardRect = activeCardRects[i];

            int offsetIndex = (i - currentIndex + activeCardRects.Count) % activeCardRects.Count;
            float newPositionX = centerPositionX + (offsetIndex - activeCardRects.Count / 2) * cardWidth;

            if (offsetIndex > activeCardRects.Count / 2)
            {
                newPositionX -= activeCardRects.Count * cardWidth;
            }

            bool isNeighbor = offsetIndex == 1 || offsetIndex == activeCardRects.Count - 1;

            if (i == currentIndex || isNeighbor)
            {
                cardRect.DOScale(i == currentIndex ? selectedScale : deselectedScale, tweenDuration);
                cardRect.DOAnchorPos3DZ(i == currentIndex ? selectedZPosition : deselectedZPosition, tweenDuration);
                cardRect.GetComponent<CanvasGroup>().DOFade(1, tweenDuration);
                cardRect.gameObject.SetActive(true);
            }
            else
            {
                cardRect.DOScale(deselectedScale * 0.8f, tweenDuration);
                cardRect.GetComponent<CanvasGroup>().DOFade(0, tweenDuration);
            }

            cardRect.DOAnchorPosX(newPositionX, tweenDuration).SetEase(Ease.OutQuad);
        }

        // Ensure currentCard is correctly set to a valid card
        if (currentCard == null || !System.Array.Exists(Cards, card => card == currentCard))
        {
            if (activeCardRects.Count > 0)
            {
                if (currentCard != null)
                {
                    if (currentCard.abilityProp)
                        currentCard.abilityProp.SetActive(false);
                    currentCard.StopCombat();
                }
                currentCard = Cards[System.Array.IndexOf(cardRects.ToArray(), activeCardRects[0])];
                currentIndex = 0; // Reset to the first active card
            }
            else
            {
                currentCard = null;
            }
        }

        // Activate current card's ability property if it exists
        if (currentCard != null && currentCard.abilityProp != null)
        {
            currentCard.abilityProp.SetActive(true);
        }
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