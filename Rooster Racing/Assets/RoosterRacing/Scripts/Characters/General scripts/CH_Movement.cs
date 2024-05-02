using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathCreation;
using System;
using MyBox;
using UnityEngine.SceneManagement;

/*  INSTRUCTIONS:
 *  Last full check: V0.3
 *  The movement is enabled in the player inputs script with the respective trigger fields.
 *  The player can move forwards automatically or manually, there are serialized fields for this changing. Also depends on the racing mode.
 *  The player can move sideways when rotating automatically in the track path or just rotate manually without side movement, there are serialized fields for this changing. This also depends on the racing mode.
 *  The jumping needs some methods to be called at the end of some of the animations of the jumping for propper sync.
 */
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public partial class CH_Movement : MonoBehaviour
{
    // General moving fields
    public enum MovementModes { Auto, Manual }
    private Rigidbody rb;
    private Animator animator;
    private CH_Data dataCode;

    // General Path field
    [HideInInspector] public float timeOnPath;
    private PathCreator pathCreator;
    private VertexPath.CharacterInPathFeatures pathFeatures;

    // Front movement fields
    [Space(4)]
    [Header("Front movement")] [Space(4)]
    public MovementModes accelerationMode;
    [Range(5, 50)] public float frontMaxVel = 30;
    [HideInInspector] public float frontSpeed;
    [HideInInspector] public bool moveTrigger = false, stopTrigger;
    [Range(5, 50)][SerializeField] private float frontAcceleration = 10;
    private bool isRunning;
    [Space(5)]

    // Rotation and side movement fields
    [Header("Rotation and side movement")] [Space(4)]
    public MovementModes rotationMode; 
    [HideInInspector] public bool movingRight, movingLeft;
    [ConditionalField("rotationMode", true, MovementModes.Manual)] public EndOfPathInstruction endOfPathInstruction;
    [ConditionalField("rotationMode", true, MovementModes.Manual)][Range(5, 30), SerializeField] private float SideVel = 3;


    // -- -- -- -- MONOBEHAVIOUR -- -- -- --

    void Awake()
    {
        // Find the components needed
        rb          = GetComponent<Rigidbody>();
        animator    = GetComponent<Animator>();
        capsuleColl = GetComponent<CapsuleCollider>();
        dataCode    = GetComponent<CH_Data>();
        var pathCreatorGO = GameObject.Find("StaticObjects/Path");
        if (pathCreatorGO)
            pathCreator = pathCreatorGO.GetComponent<PathCreator>();
    }
    void OnDestroy() => GameManager.OnRaceStart -= LevelStart;
    private void LevelStart() => enabled = true;
    void Start() 
    {
        // For race placement management
        SetPlayerRacePlacingData();

        PositioningStart();
        RestartPathFeatures();
        StartCoroutine(RunninSync());
        moveTrigger = (accelerationMode == MovementModes.Auto);
        if (SceneManager.GetActiveScene().buildIndex != 1)
            enabled = false; // disable the movement script until loading is complete.
        if (SceneManager.GetActiveScene().buildIndex > 1)
            GameManager.OnRaceStart += LevelStart;
    }
    void Update()
    {
        ForwarsMove_StatesMachine();
        JumpUpdate();
    }
    void FixedUpdate()
    {
        if(pathCreator)
            Positioning_FixedUpdate();
        PlayerRotation();
        if(moveTrigger)
            Move();
    }


    // -- -- -- --  MAIN MOVEMENT FUNCTIONS  -- -- -- --

    /// <summary>
    /// Update trigger value if acceleration on manual mode.
    /// </summary>
    public void SetMoveTrigger(bool enabled)
    {
        if (accelerationMode == MovementModes.Manual)
            moveTrigger = enabled;
    }

    /// <summary>
    /// State machine of the front movement. See documentation for more information.
    /// </summary>
    private void ForwarsMove_StatesMachine()
    {
        if (isRunning)
        {
            if (stopTrigger || !moveTrigger)
            {
                stopTrigger = isRunning = false;
                Stop();
            }
        }
        else
        {
            if (moveTrigger && !stopTrigger)
            {
                isRunning = true;
                SetRunningAnimation(true);
            }
        }
    }

    /// <summary> 
    /// Applies all the movements for the player except for the rotation and the jumping that has their own function; Applies the forward and side movement withount interrumpting the jumping movement.
    /// </summary>
    private void Move()
    {
        Transform playerTf = transform;

        // Set side velocity, but only apply it in case of automatic rotation.
        Vector3 sideVel = Vector3.zero;

        if (rotationMode == MovementModes.Auto)
        {
            if (movingRight)
                sideVel = 50f * SideVel * Time.fixedDeltaTime * playerTf.right;
            else if (movingLeft)
                sideVel = 50f * SideVel * Time.fixedDeltaTime * -playerTf.right;
        }

        // Set forward velocity with an accelerated movement
        if ((frontSpeed + (frontAcceleration * Time.fixedDeltaTime)) < frontMaxVel)
            frontSpeed += frontAcceleration * Time.fixedDeltaTime;
        else
            frontSpeed = frontMaxVel;
        Vector3 frontVel = playerTf.forward * frontSpeed;

        // Get the jumping velocity to continue it's movement without interrumpting it.
        Vector3 jumpVel = playerTf.up * rb.velocity.y;

        // Apply the movement
        rb.velocity = frontVel + jumpVel + sideVel;
    }

    public void Stop()
    {
        // Note: Might has it's animation and more stuff in further versions.
        moveTrigger = false;
        frontSpeed = 0;
        SetRunningAnimation(false);
    }

    /// <summary>
    /// Set the rotation in both cases: automatic rotation and manual rotation.
    /// </summary>
    private void PlayerRotation()
    {
        if (rotationMode == MovementModes.Auto)
        {
            if (!pathFeatures.WaitingForPathRestart)
                transform.rotation = pathCreator.path.GetRotation(timeOnPath, endOfPathInstruction);
        }
        else
        {
            if (movingRight)
                transform.Rotate(new Vector3(0, 120 * Time.fixedDeltaTime, 0));
            else if (movingLeft)
                transform.Rotate(new Vector3(0, -120 * Time.fixedDeltaTime, 0));
        }
    }


    // -- -- -- -- ANIMATION MANAGEMENT -- -- -- --

    private void SetRunningAnimation(bool enable) => animator.SetBool("Running", enable);

    /// <summary>
    /// Sets the running animation frame rate .
    /// </summary>
    private IEnumerator RunninSync()
    {
        if (animator.GetBool("Running"))
        {
            //Get the actual front speed of the player and map it to the animation speed values
            float forwardSpeed = MyTools.MyMath.Map(frontSpeed, 0.1f, frontMaxVel, 0, 1f);

            //limit the maxim vel if surpassed the max animation speed values, but this should never happen.
            if (forwardSpeed > 1)
            {
                forwardSpeed = 1;
                print("forward speed greater than animation max speed. Check this out");
            }

            //sync the speed of the animation
            animator.SetFloat("RunMultiplier", forwardSpeed);
        }

        // Make a delay for better performance and restart the animation sync.
        var delay = 0.3f;
        var timer = 0f;
        while (timer < delay)
        {
            yield return null;
            timer += Time.deltaTime;
        }
        StartCoroutine(RunninSync());
    }


    // -- -- -- -- PATH AND RACE PLACING RELATED -- -- -- --

    public void RestartPathFeatures()
    {
        if (accelerationMode == MovementModes.Auto)
            pathCreator.path.SetCharacterInPath(ref pathFeatures, transform.position);
    }

    private void SetPlayerRacePlacingData()
    {
        RaceManager raceManager = null;
        var raceManagerGO = GameObject.Find("Race");
        if (raceManagerGO)
            raceManager = raceManagerGO.GetComponent<RaceManager>();
            
        if (raceManager)
        {
            var chData = GetComponent<CH_Data>();
            if (chData == null)
                return;

            var playerName = chData.playerName;
            playerName = playerName.Trim();

            if (!dataCode.imPlayer)
            {
                if (playerName != "")
                    raceManager.competitors.Add(new RaceManager.Competitor(rb, playerName, this));
                else
                    raceManager.competitors.Add(new RaceManager.Competitor(rb, $"Bot #{raceManager.competitors.Count + 1}", this));
                dataCode.playerNumber = raceManager.competitors.Count;
            }
            else
            {
                if (playerName != "")
                    raceManager.competitors.Insert(0, new RaceManager.Competitor(rb, playerName, this));
                else
                    raceManager.competitors.Insert(0, new RaceManager.Competitor(rb, "Player", this));
            }
        }
    }

}