using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/*  INSTRUCTIONS:
 *  Last full check: V...
 *  This script ...
 */
public class UI_RaceTransitions : UI_MenuManager
{
    [SerializeField] private GameObject startCanvas, endCanvas;
    [SerializeField] private TextMeshProUGUI startCountDownText, winLoseText, positionsText;
    private List<int> descendingRaceOrder;
    private static UI_RaceTransitions instance;


    // -- -- -- -- MonoBehaviour -- -- -- --

    void Awake() => instance = this;
    void OnEnable()
    {
        GameManager.OnLevelStart += StartRaceCountDown;
        GameManager.OnRaceStart  += RaceStarted;
        GameManager.OnWinGame    += RaceWon;
        GameManager.OnLoseGame   += RaceLost;
    }
    void OnDisable()
    {
        GameManager.OnLevelStart -= StartRaceCountDown;
        GameManager.OnRaceStart  -= RaceStarted;
        GameManager.OnWinGame    -= RaceWon;
        GameManager.OnLoseGame   -= RaceLost;
    }


    // -- -- -- -- Methods -- -- -- --

    private void StartRaceCountDown()
    {
        OpenMenu(startCanvas.gameObject, false);
        StartCoroutine(RaceCountdown());
    }

    private void RaceStarted() => CloseMenu(startCanvas.gameObject);

    private void RaceWon() => RaceEnd(true);

    private void RaceLost() => RaceEnd(false);

    public void Button_Continue()
    {
        GameManager.EnterScene(0);
    }

    private void RaceEnd(bool playerWon)
    {
        OpenMenu(endCanvas.gameObject, false);
        winLoseText.text = $"{(playerWon? "YOU WON" : "YOU LOST")} ";
        Invoke("SetPlacingInfo", 0.05f);
    }

    public static void UpdatePlacingInfo(List<int> descendingRaceOrder) => instance.descendingRaceOrder = descendingRaceOrder;

    private void SetPlacingInfo()
    {
        positionsText.text = "";
        for (int i = 1; i < descendingRaceOrder.Count + 1; i++)
        {
            if (i > 1)
                positionsText.text += '\n';
            positionsText.text += $"{i}{(i > 3 ? "th" : (i > 2 ? "rd" : (i > 1 ? "nd" : "st")))} place: {RaceManager.playerName[descendingRaceOrder[i - 1]]}";
        }
    }

    private IEnumerator RaceCountdown()
    {
        var secondsOfCountdown = 3;
        for (int i = secondsOfCountdown; i > 0; i--)
        {
            startCountDownText.text = i.ToString();
            yield return new WaitForSeconds(1);
        }
        GameManager.OnRaceStart?.Invoke();
    }

}