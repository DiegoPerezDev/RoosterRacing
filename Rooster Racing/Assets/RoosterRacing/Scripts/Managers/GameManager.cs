using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/*  INSTRUCTIONS:
 *  Last full check: V0.3
 *  This code manages the main aspects of the game like: 
 *   - The scenes transitions.
 *   - The inputs initializing.
 *   - The data saving and loading.
 *   - The pausing.
 *   - The level general management.
 *  There is also a manager for the level scenes where we manage the win and lose behaviour and every other things that only happens in level scenes. 
 */
public class GameManager : MonoBehaviour
{
    // General use
    public delegate void PauseDelegate(bool pausing);
    public static PauseDelegate OnPauseByTime;
    public static GameManager instance;
    public static bool inMenu, inPause;

    // Scenes transition
    public delegate void SceneTransitionDelegate();
    public static SceneTransitionDelegate OnSceneLoaded;
    public static bool inLoadingScene = true;
    private static bool firstGameLoadSinceExecution = true;
    private static IEnumerator loadingCorroutine;
    [SerializeField] private bool printTransitionStates;

    // Level management
    public delegate void LevelDelegate();
    public delegate void LevelPauseDelegate(bool pausing);
    public static LevelDelegate OnLevelStart, OnRaceStart, OnLoseGame, OnWinGame;
    public static LevelPauseDelegate OnLevelPauseByFreezing;

    // Data saving
    public delegate void SavingDataDelegate();
    public static SavingDataDelegate OnSaving, OnLoading;


    // -- -- -- -- MonoBehaviour -- -- -- --
    void Awake()
    {
        //Set singleton
        if (instance != null)
            Destroy(transform.gameObject);
        else
        {
            instance = this;
            DontDestroyOnLoad(transform.gameObject);
        }
    }
    void OnEnable()
    {
        InputsManager.SetInputManager();
        OnLoseGame    += LoseGame;
        OnWinGame     += WinGame;
        OnPauseByTime += PauseByTime;
    }
    private void OnApplicationQuit()
    {
        OnLoseGame    -= LoseGame;
        OnWinGame     -= WinGame;
        OnPauseByTime -= PauseByTime;
    }
#if !UNITY_EDITOR
    void OnApplicationPause(bool pauseStatus)
    {
        // Pause the game when minimizing.
        if (!pauseStatus || inPause)
            return;
        OnPauseByTime?.Invoke(!inPause);
    }
#endif
    void Start()
    { 
        EnterScene(); // Call this at start for calling the respective delegates of the scene start.
    }

    
    // -- -- -- -- Scene transitioning -- -- -- --

    /// <summary>
    /// Restart current scene. For loading a specific scene used the overload instead.
    /// </summary>
    public static void EnterScene() => EnterScene(SceneManager.GetActiveScene().buildIndex);
    /// <summary>
    /// Go to a specific scene. '0' should be the main menu and each other number are the respective number of the levels.
    /// </summary>
    public static void EnterScene(int sceneIndex)
    {
        if (loadingCorroutine == null)
        {
            inLoadingScene = true;
            //AudioManager.StopLevelSong();
            instance.StartCoroutine(loadingCorroutine = StartScene(sceneIndex));
        }
        else
            print("trying to enter a scene but already loading one");
    }
    /// <summary>
    /// Go to specific scene after a given delay.
    /// </summary>
    private static void EnterScene(int sceneIndex, float delay) => instance.StartCoroutine(EnterSceneAfterDelay(sceneIndex, delay));
    private static IEnumerator EnterSceneAfterDelay(int sceneIndex, float delay)
    {
        delay = delay > 0 ? 1 : delay;
        yield return new WaitForSecondsRealtime(delay);
        EnterScene(sceneIndex);
    }
    private static IEnumerator StartScene(int sceneIndex)
    {
        float minDelay = 0.2f, timer = 0f;
        PauseByTime(false); // Time should always work when using the Time Class

        // First time entering the scene:
        //  - Dont re-load the scene if we are just opening the game.
        //  - It also helps to keep the game in the scene we are about to test when we are using the editor.
        if (firstGameLoadSinceExecution)
        {
            // Start the input system
            firstGameLoadSinceExecution = false;
            while (timer < minDelay)
            {
                yield return null;
                timer += Time.deltaTime;
            }
            goto LoadedScene;
        }

        // Load scene
        if (instance.printTransitionStates)
            print("Loading scene... Entering desired scene. (1/3)");
        AsyncOperation loadingOperation = SceneManager.LoadSceneAsync(sceneIndex);
        timer = 0f;
        while (!loadingOperation.isDone && (timer < minDelay))
        {
            yield return null;
            timer += Time.deltaTime;
        }
        if (instance.printTransitionStates)
            print("Loading completed. Scene started! (2/3)");

        // Start the scene
    LoadedScene:
        timer = 0f;
        while (timer < minDelay)
        {
            yield return null;
            timer += Time.deltaTime;
        }
        if (instance.printTransitionStates && !firstGameLoadSinceExecution)
            print("Scene started! (3/3)");
        OnSceneLoaded?.Invoke();

        // Start level
        if (SceneManager.GetActiveScene().buildIndex != 0)
            OnLevelStart?.Invoke();
        //InputsManager.EnableMenuActionMaps();

        // Finish loading settings
        inLoadingScene = false;
        loadingCorroutine = null;
    }


    // -- -- -- -- Pause -- -- -- -- 
    private static void PauseByTime(bool pausing)
    {
        inPause = pausing;
        if (inPause)
            Time.timeScale = 0;
        else
            Time.timeScale = 1;
    }


    // -- -- -- -- Level events -- -- -- --
    private static void LoseGame()
    {
        OnLevelPauseByFreezing?.Invoke(true);
        instance.StopAllCoroutines();
        //SaveHighScore();
    }
    private static void WinGame()
    {
        OnLevelPauseByFreezing?.Invoke(true);
        instance.StopAllCoroutines();
        //SaveHighScore();
    }

}