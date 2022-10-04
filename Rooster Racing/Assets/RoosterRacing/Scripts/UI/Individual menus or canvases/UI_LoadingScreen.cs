using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*  INSTRUCTIONS:
 *  Last full check: V0.3
 *  This code manages what happens in the loading screen canvas for all scenes.
 */
public class UI_LoadingScreen : MonoBehaviour
{
    void Awake() => GameManager.OnSceneLoaded += CloseLoadingScreen;
    void OnDestroy() => GameManager.OnSceneLoaded -= CloseLoadingScreen;

    private void CloseLoadingScreen() => gameObject.SetActive(false);
        
}