using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public Card[] inHand = new Card[4];
    public Card[] deck = new Card[20];

    // accessor stuff for UI to use
    public enum CardList
    {
        InHand,
        Deck
    };

    public Card[] GetCharms(CardList list)
    {
        switch (list)
        {
            case CardList.InHand: return inHand;
            case CardList.Deck: return deck;
            default: return null;
        }
    }
}
