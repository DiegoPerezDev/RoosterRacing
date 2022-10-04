using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;

/*  INSTRUCTIONS:
 * Last full check: V0 .3
 * This class manages all of the inputs of the user.
 * This input manager works with Unitys input system, the one that uses 'input actions'.
 * This code separates the inputs actions between menu inputs and player inputs so we can disable them separately.
 * Set the players input callbacks on a dedicated 'playerInputs' script.
 */
public sealed class InputsManager
{
    public delegate void InputsDelegates();
    public static InputsDelegates OnMenuBackInput;
    public static @PlayerInputActions input;
    public static PlayerInputs playerInputsCode;


    // -- -- -- -- GENERAL MANAGEMENT -- -- -- --

    /// <summary>
    /// Start function for this class, kind of a constructor
    /// </summary>
    public static void SetInputManager()
    {
        // Set input action system if there is not set already
        if (input != null)
            return;
        input = new @PlayerInputActions();
        SetInputMask();
        SetMenuInputsCallbacks();

        // Set code functions for other codes delegates
        GameManager.OnLoseGame             += ChangePlayerActionMap;
        GameManager.OnPauseByTime          += DisablePlayerActionMap;
        GameManager.OnLevelPauseByFreezing += DisablePlayerActionMap;
        GameManager.OnLevelStart           += DisableMenuActionMap;
    }
    /// <summary>
    /// Set input mask, the one that tells the game which device we are using for inputs, like an xbox controller for example
    /// </summary>
    private static void SetInputMask()
    {
#if UNITY_STANDALONE
        input.bindingMask = InputBinding.MaskByGroup("Keyboard");
#endif

#if UNITY_ANDROID
        input.bindingMask = InputBinding.MaskByGroup("android");
#endif
    }


    // -- -- -- -- MENU ACTION MAP -- -- -- --

    public static void EnableMenuActionMap() => input.MenuActionMap.Enable();
    private static void DisableMenuActionMap() => input.MenuActionMap.Disable();
    /// <summary>
    /// Set the call backs of the input action maps for the menu management, this mean that we tell what to do when performing the inputs for the menu.
    /// </summary>
    private static void SetMenuInputsCallbacks() => input.MenuActionMap.goBack.performed += ctx => OnMenuBackInput?.Invoke();


    // -- -- -- -- PLAYER ACTION MAP -- -- -- --

    /// <summary>
    /// Change the action maps between the player and the menu
    /// </summary>
    public static void EnablePlayerActionMap(bool enablePlayer)
    {
        if (!enablePlayer)
        {
            input.PlayerActionMap.Disable();
            input.MenuActionMap.Enable();
        }
        else
        {
            input.MenuActionMap.Disable();
            input.PlayerActionMap.Enable();
        }
    }
    private static void DisablePlayerActionMap(bool disablePlayer) => EnablePlayerActionMap(!disablePlayer);
    private static void ChangePlayerActionMap() => EnablePlayerActionMap(!GameManager.inPause);

}