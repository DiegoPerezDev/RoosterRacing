using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*  INSTRUCTIONS:
 *  Last full check: V0.3
 *  This code manages what happens in the main menu canvas, for the whole scene UI management there is another script called 'UI_LevelSelection'.
 */
public class UI_MainMenu : UI_Menu
{
    [SerializeField] private GameObject mainMenu, settingsMenu, exitMenu, inputsMenu;

    void OnEnable()
    {
        openedMenus = new List<GameObject>();
        buttonAudioClip = Resources.Load<AudioClip>($"Audio/UI_SFX/button");
    }

    void Start() => openedMenus.Insert(0,mainMenu);

    public void Buttons_MainMenu(string buttonName)
    {
        FixButtonInputName(ref buttonName);

        switch (buttonName)
        {
            case "play":
                //AudioManager.PlayAudio(AudioManager.UI_AudioSource, buttonAudioClip);
                CloseMenu(false);
                GameManager.OnLevelStart?.Invoke();
                GameManager.OnPauseByTime?.Invoke(false);
                var player = GameObject.FindGameObjectWithTag("Player");
                if(player)
                {
                    var playerMoveCode = player.GetComponent<CH_Movement>();
                    if (playerMoveCode)
                        playerMoveCode.enabled = true;
                }
                break;

            case "tutorial":
                
                break;

            case "settings":
                OpenMenu(settingsMenu, true);
                break;

            case "exit":
                OpenMenu(exitMenu, true);
                break;

            default:
                Debug.Log($"{buttonNotFoundMessage}'{buttonName}'");
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

}