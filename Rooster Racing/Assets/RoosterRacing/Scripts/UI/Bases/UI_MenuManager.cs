using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/*  INSTRUCTIONS:
 * Last full check: V0 .3
 * Base class of the menus of the game.
 * All the UI scripts for each canvas GameObject should derive from this class for the opening and closing of the menus.
 */
public class UI_MenuManager : MonoBehaviour
{
    protected static List<GameObject> openedMenus;
    protected static AudioClip buttonAudioClip;
    protected static string buttonNotFoundMessage = "button attempt failed with the button of name: ";


    // - - - - - MENU MANAGEMENT METHODS - - - - -

    /// <summary>
    /// Open a menu, canvas or panel, and list it in an opened menu list for returning to previous menus easily.
    /// </summary>
    /// <param name="menuToOpen">GameObject of the menu to set active.</param>
    /// <param name="closeCurrentMenu">Should we disable the current opened panel?</param>
    protected static void OpenMenu(GameObject menuToOpen, bool closeCurrentMenu)
    {
        // Check if menu available
        if (menuToOpen == null)
        {
            print("GameObject of the menu to open was not found.");
            return;
        }

        // Open menu
        GameManager.inMenu = true;
        if (!menuToOpen.activeInHierarchy)
            menuToOpen.SetActive(true);

        // Disable current menu if wanted
        if (closeCurrentMenu && openedMenus.Count > 0 && (openedMenus.Last() != null))
            openedMenus.Last().SetActive(false);

        // Add new menu in the list of opened menus
        openedMenus.Add(menuToOpen);
    }
    /// <summary>
    /// Close a menu, canvas or panel, and remove it in the list of opened menu for easy management.
    /// </summary>
    /// <param name="ReopenLastClosedMenu">Enables the last opened menu of the list of opened menus.</param>
    protected static void CloseMenu(bool ReopenLastClosedMenu)
    {
        // Check if there is any menu opened
        if (openedMenus.Count < 1)
        {
            print("There was no menu to close");
            return;
        }

        // Close current menu
        openedMenus.Last().SetActive(false);
        openedMenus.Remove(openedMenus.Last());

        if (openedMenus.Count > 0)
        {
            // Re-open last closed menu if its the case
            if (ReopenLastClosedMenu)
                openedMenus.Last().SetActive(true);
        }
        else
            GameManager.inMenu = false;
    }
    /// <summary>
    /// Close an specific menu, canvas or panel, and remove it in the list of opened menu for easy management.
    /// </summary>
    /// <param name="menuToClose">The specific menu to close if its active.</param>
    protected static void CloseMenu(GameObject menuToClose)
    {
        // Check if menu available
        if (menuToClose == null)
        {
            print("GameObject of the menu to close was not found.");
            return;
        }

        // Disable menu
        if (menuToClose.activeInHierarchy)
            menuToClose.SetActive(false);

        // Remove menu from the list of opened menus if it's the case
        if (openedMenus.Count > 0)
        {
            if (openedMenus.Last() == menuToClose)
                openedMenus.Remove(openedMenus.Last());
        }
        else
            GameManager.inMenu = false;
    }


    // - - - - - BUTTON METHODS - - - - -
    public void Buttons_FrecuentButtons(string buttonName)
    {
        FixButtonInputName(ref buttonName);

        switch (buttonName)
        {
            case "goback":
                CloseMenu(true);
                break;

            case "quit":
                print("Quitting application");
                Application.Quit();
                break;

            default:
                print($"{buttonNotFoundMessage}'{buttonName}'");
                break;
        }
    }
    protected void FixButtonInputName(ref string buttonName)
    {
        buttonName = buttonName.ToLower();
        buttonName = buttonName.Trim();
        buttonName = buttonName.Replace(" ", "");
    }

}