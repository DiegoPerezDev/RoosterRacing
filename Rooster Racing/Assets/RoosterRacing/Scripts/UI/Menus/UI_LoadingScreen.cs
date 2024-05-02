using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*  INSTRUCTIONS:
 *  Last full check: V0.3
 *  This code manages what happens in the loading screen canvas for all scenes.
 */
public class UI_LoadingScreen : MonoBehaviour
{
    //[SerializeField] private GameObject blackScreen, loadingScreen;

    void OnEnable() => GameManager.OnSceneLoaded += CloseLoadingScreen;

    void OnDisable() => GameManager.OnSceneLoaded -= CloseLoadingScreen;

    private void CloseLoadingScreen() => gameObject.SetActive(false);
        
}