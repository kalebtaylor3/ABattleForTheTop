using BFTT.Combat;
using BFTT.Controller;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardManager : MonoBehaviour
{

    public AbstractCombat[] Cards;
    public AbstractCombat currentCard;
    public PlayerController _controller;


    // Start is called before the first frame update
    void Start()
    {
        currentCard = Cards[0];
    }

    // Update is called once per frame
    void Update()
    {
        if(_controller.NextCard)
        {
            currentCard.StopCombat();
            currentCard = Cards[1];
        }

        if (_controller.PreviousCard)
        {
            currentCard.StopCombat();
            currentCard = Cards[0];
        }
    }
}
