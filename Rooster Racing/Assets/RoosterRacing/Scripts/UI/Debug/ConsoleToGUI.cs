using UnityEngine;

/*  INSTRUCTIONS:
 *  Last full check: V0.3
 *  This code re-creates the console on the screen of the game for build testing.
 *  This code is originally from: "bboisyl" from the Unity forum not from us.
 *  Source: https://answers.unity.com/questions/125049/is-there-any-way-to-view-the-console-in-a-build.html
 */
public class ConsoleToGUI : MonoBehaviour
{
    // Mods fields
    [SerializeField] bool exportLogReport;
    // End mods fields
    [SerializeField][Range(10, 1000)] private int windowWidth = 450, windowHeight = 200;
    [SerializeField][Range(10, 1000)] private int offsetX = 10, offsetY = 550;
    string myLog = "*begin log";
    string filename = "";
    readonly int kChars = 700;


    /* From the original code but not used
     * 
    bool doShow = true;
    void OnEnable() { Application.logMessageReceived += Log; }
    void OnDisable() { Application.logMessageReceived -= Log; }
    void Update() { if (Input.GetKeyDown(KeyCode.Space)) { doShow = !doShow; } }
    void OnGUI()
    {
        if (!doShow) { return; }
        GUI.matrix = Matrix4x4.TRS(new Vector3(offsetX, offsetY, 0f), Quaternion.identity,
          new Vector3(Screen.width / 1200.0f, Screen.height / 800.0f, 1.0f));
        GUI.TextArea(new Rect(10, 10, 540, 370), myLog);
    }

    // End of 'From the original code'
    */


    // -- -- -- From the original code
    public void Log(string logString, string stackTrace, LogType type)
    {
        // for onscreen...
        myLog = myLog + "\n" + logString;
        if (myLog.Length > kChars) { myLog = myLog.Substring(myLog.Length - kChars); }

        // for the file ...
        if(exportLogReport && this.enabled)
        {
            if (filename == "")
            {
                string d = System.Environment.GetFolderPath(
                   System.Environment.SpecialFolder.Desktop) + "/YOUR_LOGS";
                System.IO.Directory.CreateDirectory(d);
                string r = Random.Range(1000, 9999).ToString();
                filename = d + "/log-" + r + ".txt";
            }
            try { System.IO.File.AppendAllText(filename, logString + "\n"); }
            catch { }
        }
    }


    // -- -- -- Mods functions
    void Awake()             => Application.logMessageReceived += Log;
    void OnApplicationQuit() => Application.logMessageReceived -= Log;
    void OnGUI()             => GUI.TextArea(new Rect(offsetX, offsetY, windowWidth, windowHeight), myLog);

}