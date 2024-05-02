using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*  INSTRUCTIONS:
 *  Last full check: V0.3
 *  This code manages what happens in the pause menu canvas.
 */
public class UI_PauseMenu : UI_Menu
{
    [SerializeField] private GameObject pauseMenu, settingsMenu, inputsMenu, exitMenu;
    private CH_Movement playerMoveCode;
    

    void OnEnable() =>
        GameManager.OnPauseByTime += EnablePauseMenu;
    void OnDisable() =>
        GameManager.OnPauseByTime -= EnablePauseMenu;
    void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if(player)
            playerMoveCode = player.GetComponent<CH_Movement>();
        
    }

    public void Buttons_PauseMenu(string buttonName)
    {
        FixButtonInputName(ref buttonName);

        switch (buttonName)
        {
            case "continue":
                CloseMenu(false);
                GameManager.OnPauseByTime?.Invoke(false);
                break;

            case "unstuck":
                playerMoveCode.Unstuck();
                goto case "continue";

            case "restart":
                GameManager.EnterScene();
                break;

            case "mainmenu":
                GameManager.EnterScene(0);
                break;

            case "settings":
                OpenMenu(settingsMenu, false);
                break;

            case "exit":
                OpenMenu(exitMenu, true);
                break;

            default:
                print($"{buttonNotFoundMessage}'{buttonName}'");
                break;
        }
    }
    public void Buttons_SettingsMenu(string buttonName)
    {
        FixButtonInputName(ref buttonName);

        switch (buttonName)
        {
            case "inputs":
                OpenMenu(inputsMenu, false);
                break;

            default:
                print($"{buttonNotFoundMessage}'{buttonName}'");
                break;
        }
    }
    private void EnablePauseMenu(bool enable)
    {
        if (enable && openedMenus.Count < 1)
            OpenMenu(pauseMenu, enable);
    }

}