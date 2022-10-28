using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class CH_Movement
{
    // Jumping fields
    [Space(4)]
    [Header("Jumping")]
    [Space(4)]
    [Range(1, 10)][SerializeField] private float jumpForce = 4;
    [HideInInspector] public bool jumpTrigger;
    private enum JumpStates { idle, anticipation, waitingRise, onAir, grounding }
    private JumpStates jumpState = JumpStates.idle;
    private CapsuleCollider capsuleColl;
    private bool grounded, anticipationFinished, finishedGrounding, jumpConfirmationTO;


    // -- -- -- For MonoBehaviour -- -- -- 

    void JumpUpdate()
    {
        Jumping_JumpStatesMachine();
    }


    // -- -- -- METHODS -- -- --

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
    private void Jumping_JumpStatesMachine()
    {
        CheckGround();

        // Got to the 'OnAir' state in any ground state if the player is not touching the ground
        if (jumpState != JumpStates.onAir)
        {
            if (!grounded)
            {
                StopCoroutine(JumpComfirmationTimer());
                jumpState = JumpStates.onAir;
                //print($"Jump state: {jumpState}");
                return;
            }
        }

        switch (jumpState)
        {
            case JumpStates.idle:
                if (jumpTrigger)
                {
                    JumpAnticipation();
                    jumpState = JumpStates.anticipation;
                    //print($"Jump state: {jumpState}");
                }
                break;

            case JumpStates.anticipation:
                if (anticipationFinished)
                {
                    anticipationFinished = false;
                    Jump();
                    jumpState = JumpStates.waitingRise;
                    //print($"Jump state: {jumpState}");
                    StartCoroutine(JumpComfirmationTimer());
                }
                break;

            case JumpStates.waitingRise:
                if (jumpConfirmationTO)
                {
                    jumpConfirmationTO = false;
                    jumpState = JumpStates.idle;
                    //print($"Jump state: {jumpState}");
                }
                break;

            case JumpStates.onAir:
                if (grounded)
                {
                    jumpState = JumpStates.grounding;
                    //print($"Jump state: {jumpState}");
                    Grounding();
                }
                break;

            case JumpStates.grounding:
                if (finishedGrounding)
                {
                    finishedGrounding = false;
                    jumpTrigger = false; // this won't allow unwanted jump when landing
                    jumpState = JumpStates.idle;
                    //print($"Jump state: {jumpState}");
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
        grounded = Physics.SphereCast(centerOfSphere1, col.radius, Vector3.down, out RaycastHit hit, 0.3f);
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

}