using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public partial class CH_Movement
{
    // General fields
    [Space(4)]
    [Header("Positioning")]
    [Space(4)]
    private CH_Interactions playerInteractions;

    // Unstucking fields
    [SerializeField] private bool showPathLogMessages;
    private bool apparentlyStuck;
    private uint secondsStuck;
    private const uint secondsBeforeUnstucking = 3;

    // Restarting pos
    private Vector3 startPos;
    private Quaternion startRot;

    // Player stays on track system
    [SerializeField] private MeshFilter trackMesh;
    private float trackLowestPointHeight;


    // -- -- -- For MonoBehaviour -- -- -- 

    private void PositioningStart()
    {
        // For the restarting pos
        startPos = transform.position;
        startRot = transform.localRotation;

        // For debuging the path movement system.
        if(pathCreator)
            pathCreator.path.showLogMessages = showPathLogMessages;

        // For the unstucking
        playerInteractions = GetComponent<CH_Interactions>();
        StartCoroutine(CheckStuckingCorroutine());

        // For checking that the player stays on the track
        StartPlayerOnTrackSystem();
    }

    void Positioning_FixedUpdate()
    {
        timeOnPath = pathCreator.path.GetClosestTimeOnPath(transform.position, ref pathFeatures);
    }


    // -- -- -- METHODS -- -- --

    /// <summary>
    /// Player gets back to the position it has at the start of the scene.
    /// </summary>
    public void RestartPos()
    {
        transform.position = startPos;
        transform.rotation = startRot;
        pathFeatures.WaitingForPathRestart = false;
    }


    #region Unstuck

    /// <summary>
    /// Un-stuck the player by taking it to the closest un-stuck checkpoint. Also re-organize tha player in the auto-path in case of automatic rotation mode.
    /// </summary>
    public void Unstuck()
    {
        if (!dataCode.imPlayer)
            return;
        transform.position = pathCreator.path.GetPoint(pathCreator.path.GetPlayerCurrentPointOnPath(transform.position));
        RestartPathFeatures();
    }

    private IEnumerator CheckStuckingCorroutine()
    {
        // Check if player stuck, count the seconds it's been stuck
        if(moveTrigger && !playerInteractions.invulnerable)
        {
            if(rb.velocity.magnitude < 0.1f)
            {
                if(!apparentlyStuck)
                    apparentlyStuck = true;
                else
                    secondsStuck++;

                if (secondsStuck >= secondsBeforeUnstucking)
                    Unstuck();
                else
                    goto Delay;
            }
        }
        // Not stucked, restart variables
        apparentlyStuck = false;
        secondsStuck = 0;

        // Delay
        Delay:
        float timer = 0f;
        float delay = 1f;
        while(timer < delay)
        {
            yield return null;
            timer += Time.deltaTime;
        }

        // Restart checking
        StartCoroutine(CheckStuckingCorroutine());
    }

    #endregion


    #region Prevention of going outside of the level

    /// <summary>
    /// Start the prevention of the player getting outside of the map.
    /// </summary>
    private void StartPlayerOnTrackSystem()
    {
        // Get track lowest point of the up axis so later we can check if the player 
        if (trackMesh == null)
        {
            print("No track mesh detected for checking when the player falls from the track");
            return;
        }
        trackLowestPointHeight = trackMesh.mesh.bounds.min.y;

        // Start loop of checking players height compared to track lowest height
        StartCoroutine(PreventPlayerOutsideTrackCorLoop());
    }

    /// <summary>
    /// Compare player pos to track lowest point to know if the player got outside of the level. Restart player pos to its closest point on path if so.
    /// </summary>
    private IEnumerator PreventPlayerOutsideTrackCorLoop()
    {
        // Compare player pos to track and restart pos if its the case
        if (rb.position.y < trackLowestPointHeight)
        {
            transform.position = pathCreator.path.GetPoint(pathCreator.path.GetPlayerCurrentPointOnPath(transform.position));
            RestartPathFeatures();
        }

        // Restart coroutine
        var delay = 2f;
        var timer = 0f;
        while (timer < delay)
        {
            yield return null;
            timer += Time.deltaTime;
        }
        StartCoroutine(PreventPlayerOutsideTrackCorLoop());
    }

    #endregion

}