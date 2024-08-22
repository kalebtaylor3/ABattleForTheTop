using UnityEngine;
using TMPro; // Import TextMeshPro namespace
using UnityEngine.UI; // Import Unity UI namespace

public class DealerCard : MonoBehaviour
{
    public string suit;  // e.g., "Hearts", "Diamonds", "Clubs", "Spades"
    public int value;    // 1 to 13, where 1 is Ace, 11 is Jack, 12 is Queen, 13 is King

    public TextMeshProUGUI valueText; // Reference to the TextMeshProUGUI component for displaying the value
    public Image[] suitImage;           // Reference to the Image component for displaying the suit

    public Sprite heartsSprite;  // Sprite for Hearts suit
    public Sprite diamondsSprite; // Sprite for Diamonds suit
    public Sprite clubsSprite;    // Sprite for Clubs suit
    public Sprite spadesSprite;   // Sprite for Spades suit

    // Method to update the card's visual elements
    public void UpdateCardVisuals()
    {
        // Update the text based on the value
        if (value == 1)
        {
            valueText.text = "A"; // Ace
        }
        else if (value == 11)
        {
            valueText.text = "J"; // Jack
        }
        else if (value == 12)
        {
            valueText.text = "Q"; // Queen
        }
        else if (value == 13)
        {
            valueText.text = "K"; // King
        }
        else
        {
            valueText.text = value.ToString(); // Number cards
        }

        // Update the suit image based on the suit string
        switch (suit)
        {
            case "Hearts":
                for(int i = 0; i < suitImage.Length; i++)
                    suitImage[i].sprite = heartsSprite;
                break;
            case "Diamonds":
                for (int i = 0; i < suitImage.Length; i++)
                    suitImage[i].sprite = diamondsSprite;
                break;
            case "Clubs":
                for (int i = 0; i < suitImage.Length; i++)
                    suitImage[i].sprite = clubsSprite;
                break;
            case "Spades":
                for (int i = 0; i < suitImage.Length; i++)
                    suitImage[i].sprite = spadesSprite;
                break;
        }
    }
}
