using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public partial class CH_Movement
{
    // General fields
    [Space(4)]
    [Header("Player only")]
    [Space(4)]

    // Unstucking fields
    [SerializeField] private GameObject unstuckCheckpointsParent;
    [SerializeField] private bool showPathLogMessages;
    private List<Vector3> unstuckCheckpointsPos;

    // Restarting pos
    private Vector3 startPos;
    private Quaternion startRot;


    // -- -- -- For MonoBehaviour -- -- -- 

    private void PositioningStart()
    {
        // For the restarting pos
        startPos = transform.position;
        startRot = transform.localRotation;

        // For debuging the path movement system.
        if (dataCode.imPlayer)
        {
            if(pathCreator)
                pathCreator.path.showLogMessages = showPathLogMessages;
        }

        // For the unstucking of the player
        if (dataCode.imPlayer)
        {
            unstuckCheckpointsPos = new List<Vector3>();
            if (unstuckCheckpointsParent)
            {
                foreach (Transform checkpoint in unstuckCheckpointsParent.GetComponentsInChildren<Transform>())
                    unstuckCheckpointsPos.Add(checkpoint.position);
            }
        }
    }

    void Positioning_FixedUpdate()
    {
        timeOnPath = pathCreator.path.GetClosestTimeOnPath(transform.position, ref pathFeatures);
    }


    // -- -- -- METHODS -- -- --

    /// <summary>
    /// Un-stuck the player by taking it to the closest un-stuck checkpoint. Also re-organize tha player in the auto-path in case of automatic rotation mode.
    /// </summary>
    public void Unstuck()
    {
        if (!dataCode.imPlayer)
            return;
        if (unstuckCheckpointsParent)
        {
            transform.position = MyTools.MyMath.CalculateClosestPoint(transform.position, unstuckCheckpointsPos);
            RestartPathFeatures();
        }
    }

    /// <summary>
    /// Player gets back to the position it has at the start of the scene.
    /// </summary>
    public void RestartPos()
    {
        transform.position = startPos;
        transform.rotation = startRot;
        pathFeatures.WaitingForPathRestart = false;
    }

}