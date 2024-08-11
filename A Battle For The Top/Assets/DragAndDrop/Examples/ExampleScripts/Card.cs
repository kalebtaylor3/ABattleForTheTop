using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BFTT.Combat;

[CreateAssetMenu(fileName = "Card", menuName = "Card")]
public class Card : ScriptableObject {


    public enum CardType
    {
        All = 127 // combination of all types
    }

    public CardType cardType = CardType.All;
    public Sprite icon;
    public string CardDesciption;
    public string cardID;

    public string GetDescription()
    {
        // name in bold, plus type
        string desc = "<b>" + name + "</b> (" + CardDesciption + ")";

        return desc;

    }
}
