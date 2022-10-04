using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathCreation;
using System;
using MyBox;

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
public class CH_Movement : MonoBehaviour
{
    // General moving fields
    public enum MovementModes { Auto, Manual }
    private Rigidbody rb;
    private Animator animator;

    // Front movement fields
    [Header("Front movement")]
    [Space(4)]
    public MovementModes accelerationMode;
    [Range(1, 10)] public float frontMaxVel = 6;
    [HideInInspector] public float frontSpeed;
    [HideInInspector] public bool moveTrigger, stopTrigger;
    [Range(1, 10)][SerializeField] private float frontAcceleration = 2;
    
    private readonly float velScale = 5f;
    private bool isRunning;
    [Space(5)]

    // Rotation and side movement fields
    [Header("Rotation and side movement")]
    [Space(4)]
    public MovementModes rotationMode;
    [ConditionalField("rotationMode", true, MovementModes.Manual)] public EndOfPathInstruction endOfPathInstruction;
    [ConditionalField("rotationMode", true, MovementModes.Manual)][Range(1, 10), SerializeField] private float SideVel = 3;
    [HideInInspector] public PathCreator pathCreator;
    [HideInInspector] public VertexPath.CharacterInPathFeatures pathFeatures;
    [HideInInspector] public bool movingRight, movingLeft;
    [SerializeField] private bool showPathLogMessages;
    private readonly int rotation = 120;
    [Space(5)]

    // Jumping fields
    [Header("Jumping")]
    [Space(4)]
    [HideInInspector] public bool jumpTrigger;
    [Range(1, 20)][SerializeField] private float jumpForce = 4;
    private enum States { idle, anticipation, waitingRise, onAir, grounding }
    private States sm = States.idle;
    private CapsuleCollider capsuleColl;
    private bool grounded, anticipationFinished, finishedGrounding, jumpConfirmationTO;
    [Space(5)]

    // Unstucking fields
    [Header("Unstucking")]
    [Space(4)]
    [SerializeField] private GameObject unstuckCheckpointsParent;
    private List<Vector3> unstuckCheckpointsPos;


    // -- -- -- -- MONOBEHAVIOUR -- -- -- --

    void Awake()
    {
        //find the components needed
        rb = GetComponent<Rigidbody>();
        //animator = GetComponent<Animator>();
        capsuleColl = GetComponent<CapsuleCollider>();
        var pathCreatorGO = GameObject.Find("StaticObjects/Path");
        if (pathCreatorGO)
            pathCreator = pathCreatorGO.GetComponent<PathCreator>();
        else
            print("No path found on scene in the given file direction.");
        
        // disable the movement script until loading is complete.
        enabled = false;
    }
    void Start() 
    {
        // Front movement start
        if(pathCreator && rotationMode == MovementModes.Auto)
        {
            pathCreator.path.showLogMessages = showPathLogMessages;
            pathCreator.path.SetCharacterInPathFeatures(ref pathFeatures, transform.position);
            transform.SetPositionAndRotation(pathCreator.path.GetPoint(0), pathCreator.path.GetRotation(pathCreator.path.GetClosestTimeOnPath(transform.position), endOfPathInstruction));
            moveTrigger = true;
        }
        StartCoroutine(RunninSync());

        // Get unstuck check-points
        unstuckCheckpointsPos = new List<Vector3>();
        if (unstuckCheckpointsParent)
        {
            foreach (Transform checkpoint in unstuckCheckpointsParent.GetComponentsInChildren<Transform>())
                unstuckCheckpointsPos.Add(checkpoint.position);
        }
    }
    void Update()
    {
        ForwarsMove_StatesMachine();
        Jumping_StatesMachine();
    }
    void FixedUpdate()
    {
        // Rotation
        PlayerRotation();

        // Front and side movement
        if(moveTrigger)
            Move();
    }


    #region ROTATION & UNSTUCK

    /// <summary>
    /// Set the rotation in both cases: automatic rotation and manual rotation.
    /// </summary>
    private void PlayerRotation()
    {
        if (rotationMode == MovementModes.Auto)
        {
            if (!pathFeatures.WaitingForPathRestart)
                transform.rotation = pathCreator.path.GetRotation(pathCreator.path.GetClosestTimeOnPath(transform.position, ref pathFeatures), endOfPathInstruction);
        }
        else
        {
            if (movingRight)
                transform.Rotate(new Vector3(0, rotation * Time.fixedDeltaTime, 0));
            else if (movingLeft)
                transform.Rotate(new Vector3(0, -rotation * Time.fixedDeltaTime, 0));
        }
    }

    /// <summary>
    /// Un-stuck the player by taking it to the closest un-stuck checkpoint. Also re-organize tha player in the auto-path in case of automatic rotation mode.
    /// </summary>
    public void Unstuck()
    {
        if(unstuckCheckpointsParent)
        {
            transform.position = MyTools.MyMath.CalculateClosestPoint(transform.position, unstuckCheckpointsPos);
            if (accelerationMode == MovementModes.Auto)
                pathCreator.path.SetCharacterInPathFeatures(ref pathFeatures, transform.position);
        }
    }

    #endregion


    #region FORWARD MOVEMENT

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

    private void SetRunningAnimation(bool enable) => animator.SetBool("Running", enable);

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
                sideVel = 50f * SideVel * Time.fixedDeltaTime * velScale * playerTf.right;
            else if (movingLeft)
                sideVel = 50f * SideVel * Time.fixedDeltaTime * velScale * -playerTf.right;
        }

        // Set forward velocity with an accelerated movement
        if ((frontSpeed + (frontAcceleration * velScale * Time.fixedDeltaTime)) < (frontMaxVel * velScale))
            frontSpeed += frontAcceleration * velScale * Time.fixedDeltaTime;
        else
            frontSpeed = frontMaxVel * velScale;
        Vector3 frontVel = playerTf.forward * frontSpeed;

        // Get the jumping velocity to continue it's movement without interrumpting it.
        Vector3 jumpVel = playerTf.up * rb.velocity.y;

        // Apply the movement
        rb.velocity = frontVel + jumpVel + sideVel;
    }

    private void Stop()
    {
        // Note: Might has it's animation and more stuff in further versions.
        frontSpeed = 0;
        SetRunningAnimation(false);
    }

    /// <summary>
    /// Sets the running animation frame rate .
    /// </summary>
    private IEnumerator RunninSync()
    {
        if (animator.GetBool("Running"))
        {
            //Get the actual front speed of the player and map it to the animation speed values
            float forwardSpeed = MyTools.MyMath.Map(frontSpeed, 0.1f, frontMaxVel * velScale, 0, 1f);

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

    #endregion


    #region JUMPING

    /// <summary>
    /// This function is called when the jump anticipation animation is completed.
    /// </summary>
    public void AnticipationAnimDone() => anticipationFinished = true;

    /// <summary>
    /// This function is called when the jump recovery animation is completed
    /// </summary>
    public void JumpFinished() => finishedGrounding = true;

    /// <summary>
    /// For better understanding see documentation.
    /// </summary>
    private void Jumping_StatesMachine()
    {
        CheckGround();

        // Got to the 'OnAir' state in any ground state if the player is not touching the ground
        if (sm != States.onAir)
        {
            if(!grounded)
            {
                StopCoroutine(JumpComfirmationTimer());
                sm = States.onAir;
                //print($"Jump state: {sm}");
                return;
            }
        }

        switch (sm)
        {
            case States.idle:
                if (jumpTrigger)
                {
                    JumpAnticipation();
                    sm = States.anticipation;
                    //print($"Jump state: {sm}");
                }
                break;

            case States.anticipation:
                if (anticipationFinished)
                {
                    anticipationFinished = false;
                    Jump();
                    sm = States.waitingRise;
                    //print($"Jump state: {sm}");
                    StartCoroutine(JumpComfirmationTimer());
                }
                break;

            case States.waitingRise:
                if(jumpConfirmationTO)
                {
                    jumpConfirmationTO = false;
                    sm = States.idle;
                    //print($"Jump state: {sm}");
                }
                break;

            case States.onAir:
                if (grounded)
                {
                    sm = States.grounding;
                    //print($"Jump state: {sm}");
                    Grounding();
                }
                break;

            case States.grounding:
                if (finishedGrounding)
                {
                    finishedGrounding = false;
                    jumpTrigger = false; // this won't allow unwanted jump when landing
                    sm = States.idle;
                    //print($"Jump state: {sm}");
                }
                break;
        }
    }

    /// <summary>
    /// Anticipation animation of the jumping. Calls the 'AnticipationAnimDone()' function when the animation is done.
    /// </summary>
    private void JumpAnticipation()
    {
        animator.SetBool("Grounded", false);
        animator.SetTrigger("JumpTrigger");
    }

    private void Jump()
    {
        int jumpMultiple = 500;
        rb.AddForce(jumpForce * jumpMultiple * transform.up, ForceMode.Impulse);
        animator.Play("Jump");
        animator.SetBool("Grounded", false);
    }

    private void CheckGround()
    {
        var col = capsuleColl;
        Vector3 centerOfSphere1 = transform.position + Vector3.up * col.radius;
        grounded = Physics.SphereCast(centerOfSphere1, col.radius, Vector3.down, out RaycastHit hit, 0.1f);
    }

    /// <summary>
    /// Enables the animation of the grounding. makes true the variable 'finishedGrounding' when the animation is done so it won't be any jumping attempt while the animation is playing.
    /// </summary>
    private void Grounding()
    {
        animator.Play("JumpRec");
        animator.SetBool("Grounded", true);
    }

    /// <summary>
    /// Short timer to know if the jump attemp turn into a propper jump, if this does not happen then we should turn back to the idle state of jumping.
    /// </summary>
    private IEnumerator JumpComfirmationTimer()
    {
        jumpConfirmationTO = false;
        yield return new WaitForSeconds(0.2f);
        yield return null;
        jumpConfirmationTO = true;
    }

    #endregion

}