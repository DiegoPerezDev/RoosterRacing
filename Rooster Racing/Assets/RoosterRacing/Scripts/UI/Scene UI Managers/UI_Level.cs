using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*  INSTRUCTIONS:
 *  Last full check: V0.3
 *  The main UI script for every level scene. This should be attached in the parent UI gameObject.
 */
public class UI_Level : UI_MainMenuManager
{
    [SerializeField] private GameObject HUD;
    //[HideInInspector] public enum UI_AudioNames { lose, pause, unPause }
    //public static AudioClip[] UI_Clips = new AudioClip[Enum.GetNames(typeof(UI_AudioNames)).Length];

    public override void OnEnable()
    {
        InputsManager.OnMenuBackInput += CheckForMenuClosing;
        GameManager.OnLoseGame        += CloseHUD;
        GameManager.OnWinGame         += CloseHUD;
    }
    public override void OnDisable()
    {
        InputsManager.OnMenuBackInput -= CheckForMenuClosing;
        GameManager.OnLoseGame        -= CloseHUD;
        GameManager.OnWinGame         -= CloseHUD;
    }
    void Start()
    {
        // Get components
        //foreach (UI_AudioNames audioClip in Enum.GetValues(typeof(UI_AudioNames)))
        //    UI_Clips[(int)audioClip] = Resources.Load<AudioClip>($"Audio/UI_SFX/{uiClipsPaths[(int)audioClip]}");
    }
    

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

    private void CloseHUD() => CloseMenu(HUD);

}