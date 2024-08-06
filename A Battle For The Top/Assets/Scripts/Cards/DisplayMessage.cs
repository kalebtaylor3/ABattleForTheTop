using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisplayMessage : MonoBehaviour
{
    private string message;
    private string cardName;
    public GUIStyle guiStyle = new GUIStyle();

    private bool showMessage = false;

    void Start()
    {
        // Customize the GUIStyle if needed
        guiStyle.fontSize = 24;
        guiStyle.normal.textColor = Color.white;
        guiStyle.alignment = TextAnchor.MiddleCenter; // Center the text
    }

    void OnGUI()
    {
        if (showMessage)
        {
            // Calculate the position for the top middle of the screen
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            Vector2 messageSize = guiStyle.CalcSize(new GUIContent(cardName + " " + message));
            float messageX = (screenWidth - messageSize.x) / 2;
            float messageY = 150; // Adjust the Y position to your preference

            // Draw the message on the screen
            GUI.Label(new Rect(messageX, messageY, messageSize.x, messageSize.y), cardName + " " + message, guiStyle);
        }
    }

    // Method to show or hide the message
    public void SetShowMessage(bool show, string CardName, string Message)
    {
        showMessage = show;
        cardName = CardName;
        message = Message;
    }
}
