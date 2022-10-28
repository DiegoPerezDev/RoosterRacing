using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/*  INSTRUCTIONS:
 *  Last full check: V0.3
 *  This script enables and disables pop-up messages in the game, also can display specific messages in them.
 *  For now it's used for showing the "In Development" message for the not implemented levels in the level selection.
 */
public class UI_PopUps : UI_MenuManager
{
    [SerializeField] private TextMeshProUGUI screenMessage;
    private static UI_PopUps instance;

    void Awake() => instance = this;    

    /// <summary>
    /// Open a canvas for quick messages purposes and displays an specific message in it. 
    /// </summary>
    public static void ShowMessage(string message)
    {
        OpenMenu(instance.gameObject.transform.GetChild(0).gameObject, false);
        instance.screenMessage.text = message;
    }

    /// <summary>
    /// Close the pop-up message canvas.
    /// </summary>
    public static void HideMessage() => CloseMenu(instance.gameObject.transform.GetChild(0).gameObject);
}