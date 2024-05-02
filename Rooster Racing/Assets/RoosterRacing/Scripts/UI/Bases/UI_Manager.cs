using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*  INSTRUCTIONS:
 * Last full check: V0 .3
 * Base class of the menu management of the scenes in the game.
 * Get assets and data used between all UI scripts in this one.
 * There is a class that derives from 'UI_Manager' for the levels and another for the main menu or other different scene type.
 */
public class UI_Manager : UI_Menu
{
    public virtual void Awake()
    {
        openedMenus = new List<GameObject>();
        buttonAudioClip = Resources.Load<AudioClip>($"Audio/UI_SFX/button");
    }
    public virtual void OnEnable()  => InputsManager.OnMenuBackInput += CheckForMenuClosing;
    public virtual void OnDisable() => InputsManager.OnMenuBackInput -= CheckForMenuClosing;

    /// <summary>
    /// When called, close the current menu. For Unpausing there is a overrided method derived of this class for the scene where it should happen.
    /// </summary>
    public virtual void CheckForMenuClosing()
    {
        if (GameManager.inMenu)
            CloseMenu(true);
    }

}