using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*  INSTRUCTIONS:
 *  Last full check: V0.3
 *  The main UI script for the Level selection scene. This should be attached in the parent UI gameObject.
 */
public class UI_LevelSelection : UI_Manager
{
    /// <summary>
    /// <para>Check if we need to return to the previous menu or close the current menu and unpause the game.</para>
    /// <para>This overrided method is called in the awake via base class.</para>
    /// </summary>
    public override void CheckForMenuClosing()
    {
        if (openedMenus.Count > 0)
        {
            CloseMenu(false);
            GameManager.OnPauseByTime?.Invoke(false);
        }
        else
            base.CheckForMenuClosing();
    }
}