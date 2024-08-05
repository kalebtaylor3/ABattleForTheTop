using BFTT.Combat;
using BFTT.Controller;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class CardManager : MonoBehaviour
{
    public AbstractCombat[] Cards;
    public AbstractCombat currentCard;
    public PlayerController _controller;

    public RectTransform cardParent; // Parent RectTransform to hold card UI elements
    public GameObject cardPrefab; // Prefab for card UI elements
    public float tweenDuration = 0.5f;
    public Vector3 selectedScale = new Vector3(1.2f, 1.2f, 1.2f); // Scale of the selected card
    public Vector3 deselectedScale = Vector3.one; // Scale of the deselected cards
    public float selectedZPosition = -100f; // Z position for the selected card
    public float deselectedZPosition = 0f; // Z position for the deselected cards

    private List<RectTransform> cardRects = new List<RectTransform>(); // List to hold RectTransforms of the cards
    private int currentIndex = 0;

    void Start()
    {
        currentCard = Cards[0];
        if (currentCard.abilityProp)
            currentCard.abilityProp.SetActive(true);
        InitializeCardUI();
        UpdateCardUI();
    }

    private void OnEnable()
    {
        FlameWand.OnWandBreak += RemoveCard;
        GrappleBeam.OnGrappleBreak += RemoveCard;
    }

    private void OnDisable()
    {
        FlameWand.OnWandBreak -= RemoveCard;
        GrappleBeam.OnGrappleBreak -= RemoveCard;
    }

    void Update()
    {
        if (_controller._scheduler.characterActions.NextCard && currentCard.ReadyToExit())
        {
            currentCard.StopCombat();
            if(currentCard.abilityProp)
                currentCard.abilityProp.SetActive(false);
            currentIndex = (currentIndex + 1) % Cards.Length;
            currentCard = Cards[currentIndex];
            if (currentCard.abilityProp)
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
            if (currentCard.abilityProp)
                currentCard.abilityProp.SetActive(true);
            UpdateCardUI();
        }
    }

    void InitializeCardUI()
    {
        for (int i = 0; i < Cards.Length; i++)
        {
            GameObject cardObj = Instantiate(cardPrefab, cardParent);
            RectTransform cardRect = cardObj.GetComponent<RectTransform>();
            cardRects.Add(cardRect);

            // Assign cardIcon image
            Image cardIcon = cardRect.GetComponent<Image>();
            cardIcon.sprite = Cards[i].cardIcon; // Assuming cardIcon is a Sprite property in AbstractCombat
        }
    }

    void UpdateCardUI()
    {
        for (int i = 0; i < cardRects.Count; i++)
        {
            if (i == currentIndex)
            {
                // Tween the selected card
                cardRects[i].DOScale(selectedScale, tweenDuration);
                cardRects[i].DOAnchorPos3DZ(selectedZPosition, tweenDuration);
                cardRects[i].gameObject.SetActive(true); // Make the card visible
            }
            else if (i == (currentIndex + 1) % cardRects.Count || i == (currentIndex - 1 + cardRects.Count) % cardRects.Count)
            {
                // Tween the adjacent cards
                cardRects[i].DOScale(deselectedScale, tweenDuration);
                cardRects[i].DOAnchorPos3DZ(deselectedZPosition, tweenDuration);
                cardRects[i].gameObject.SetActive(true); // Make the card visible
            }
            else
            {
                // Hide the cards that are not in the visible range
                cardRects[i].gameObject.SetActive(false);
            }
        }
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
            List<AbstractCombat> cardsList = new List<AbstractCombat>(Cards);
            cardsList.RemoveAt(index);
            Cards = cardsList.ToArray();

            // Destroy the card UI
            Destroy(cardRects[index].gameObject);
            cardRects.RemoveAt(index);

            // Adjust currentIndex if necessary
            if (currentIndex >= Cards.Length)
            {
                currentIndex = 0;
            }

            if(Cards.Length > 0)
                currentCard = Cards[currentIndex];
            else
                currentCard = null;
            UpdateCardUI();

            if (currentCard != null)
                if(currentCard.abilityProp)
                    currentCard.abilityProp.SetActive(true);
        }
    }
}
