using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*  INSTRUCTIONS:
 *  Last full check: V0.3
 *  This code changes the main camera between two cameras attached in the player.
 *  The switching activation happens in the 'Player Inputs' code.
 *  There if a front camera and a back camera.
*/
public class CameraManager : MonoBehaviour
{
    private static Camera frontCamera = null, backCamera = null;


    void Awake()
    {
        // Find the components needed
        var frontCameraGO = gameObject.transform.Find("FrontCamera");
        if(frontCameraGO)
            frontCamera = frontCameraGO.GetComponent<Camera>();
        var backCameraGO = gameObject.transform.Find("BackCamera");
        if (backCameraGO)
            backCamera = backCameraGO.GetComponent<Camera>();

        // Disable back camera at the beginning
        backCamera.enabled = false;
    }

    /// <summary>
    /// Changes the main camera between two cameras attached in the player, the front and the back one.
    /// </summary>
    /// <param name="displayBackCamera">Enable back camera if true, enable front camera if false.</param>
    public static void ChangeCameraDisplaying(bool displayBackCamera)
    {
        frontCamera.enabled = !displayBackCamera;
        backCamera.enabled = displayBackCamera;
    }
    
}