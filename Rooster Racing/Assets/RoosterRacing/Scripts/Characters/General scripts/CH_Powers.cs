using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/*  INSTRUCTIONS:
 *  Last full check: V0.0.2
 *  The powers are activatable with the 'powerTrigger' bool, but it also needs the 'withPower' or 'unlimitedPower' bool to be true
 *  A power finishes when using the method 'powerUsed'. for some powers this method is called as an animation event in the last part of that power animation.
 *  For the tackle: the player nearby data is gotten in the 'PlayerDetector' script attached some grandchildrens of the player game object.
 */
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CH_Movement))]
public class CH_Powers : MonoBehaviour
{
    //Powers in general fields
    public delegate void PowerDelegateWithParam(Powers power);
    public delegate void PowerDelegate();
    public PowerDelegate OnPowerAcquired;
    public static PowerDelegateWithParam OnPowerAcquiredByPlayer;
    public static PowerDelegate OnPowerUsedByPlayer;
    public enum Powers { none, eggSet, eggThrow, tackle }
    [HideInInspector] public Powers currentPower;
    [HideInInspector] public bool unlimitedPower = false;
    private PowerDelegate OnPowerUsed;
    private bool usingPower;
    
    //Egg set fields
    private Vector3 eggSet_IniDir = new Vector3(-4f, 0.3f, -4f);

    //Egg Throw fields
    [Range(1,100)] [SerializeField] private int eggTrow_Force = 50;
    private Vector3 eggThrow_IniDir = new Vector3(-4f, 3f, -4f);
    private float eggLifetime = 10f;
    
    //Tackle fields
    public int tackleForce = 60;
    private float tackleTO = 0.5f;
    public enum Sides { left, right}
    private Sides tackleSide;
    [HideInInspector] public bool alwaysTackle, enemyOnRightSide, enemyOnLeftSide;

    //Components fields
    private GameObject eggThrow_Pref, eggThrow, eggSetPref, tacklers;
    [HideInInspector] public GameObject playerDetectors;
    private Rigidbody rb;
    private CH_Movement moveCode;
    private Animator animator;
    private CH_Data playerData;


    #region Monobehaviour

    void Awake()
    {
        //find the components needed
        playerData = GetComponent<CH_Data>();
        moveCode   = GetComponent<CH_Movement>();
        rb         = GetComponent<Rigidbody>();
        animator   = GetComponent<Animator>();

        //find the game objects needed
        eggThrow_Pref   = Resources.Load<GameObject>("Prefabs/InteractiveObjects/EggThrowed");
        eggSetPref      = Resources.Load<GameObject>("Prefabs/InteractiveObjects/EggSet");
        tacklers        = transform.Find("Tacklers").gameObject;
        playerDetectors = transform.Find("PlayerDetectors").gameObject;
    }
    void OnDestroy()
    {
        if (playerData.imPlayer)
        {
            OnPowerAcquiredByPlayer -= NewPower;
            //OnPowerUsedByPlayer     -= PowerUsed;
        }
        else
        {
            OnPowerAcquired -= NewPower;
            OnPowerUsed     -= PowerUsed;
        }
    }
    
    void Start()
    {
        if (playerData.imPlayer)
        {
            OnPowerAcquiredByPlayer += NewPower;
            //OnPowerUsedByPlayer += PowerUsed;
        }
        else
        {
            OnPowerAcquired += NewPower;
            OnPowerUsed += PowerUsed;
        }

        //Tackle colliders disabled at the beginning of the game
        tacklers.SetActive(false);
        if(!unlimitedPower)
            playerDetectors.SetActive(false);
    }

    #endregion

    #region general Functions

    public void ActivatePower()
    {
        if (usingPower)
            return;
        usingPower = true;

        switch (currentPower)
        {
            default:
                usingPower = false;
                break;

            case Powers.eggThrow:
                EggThrowAnticipation();
                break;

            case Powers.tackle:
                TackleAttempt();
                break;
            
            case Powers.eggSet:
                EggSet();
                break;
        }

        if(currentPower != Powers.eggThrow)
            OnPowerUsed?.Invoke();
    }

    private void NewPower()
    {
        var power = (Powers)UnityEngine.Random.Range(0, Enum.GetNames(typeof(Powers)).Length);
        NewPower(power);
    }

    private void NewPower(Powers power)
    {
        currentPower = power;

        // if got the tackle power, activate the player detectors needed for the tackle power
        if (currentPower == Powers.tackle)
            playerDetectors.SetActive(true);
    }

    private void PowerUsed()
    {
        usingPower = false;
        if (unlimitedPower)
            return;
        currentPower = Powers.none;

        if (playerData.imPlayer)
            OnPowerUsedByPlayer?.Invoke();
    }
    #endregion

    #region Egg Set Functions

    private void EggSet()
    {
        //animation
        animator.SetInteger("Power", 1);
        animator.SetTrigger("PowerTrigger");

        //power
        Vector3 eggSetInitialPos = Vector3.Scale(eggSet_IniDir, transform.forward) + new Vector3(0, eggSet_IniDir.y, 0);
        Instantiate(eggSetPref, transform.position + eggSetInitialPos, Quaternion.identity);
    }

    #endregion

    #region Egg Throw Functions

    private void EggThrowAnticipation()
    {
        animator.SetInteger("Power", 2);
        animator.SetTrigger("PowerTrigger");
    }

    // NOTE: This function is called in an animation event after the anticipation animation is done
    private void EggThrow()
    {
        //create an egg behind the rooster 
        Vector3 eggInitialPos = Vector3.Scale(eggThrow_IniDir, transform.forward) + new Vector3(0, eggThrow_IniDir.y, 0);
        eggThrow = Instantiate(eggThrow_Pref, transform.position + eggInitialPos, Quaternion.identity);

        //throw that egg backward
        eggThrow.GetComponent<Rigidbody>().AddForce(-transform.forward * eggTrow_Force, ForceMode.VelocityChange);

        //Destroy the egg afther some time
        Destroy(eggThrow, eggLifetime);
    }

    #endregion

    #region Tackle Functions

    //See if there is a player nearby to tackle, if there is then start the tackle anticipation animation.
    private void TackleAttempt()
    {
        //Check if it can tackle based on the playersNearby, if not then return to the idle state
        if( !enemyOnRightSide && !enemyOnLeftSide )
            usingPower = true;
        else
            TackleAnticipation();
    }

    //Set the tackle side and play the tackle anticipation animation
    private void TackleAnticipation()
    {
        //Set the side to tackle based on the playersNearby
        if( enemyOnRightSide && !enemyOnLeftSide)
            tackleSide = Sides.right;
        else if ( !enemyOnRightSide && enemyOnLeftSide)
            tackleSide = Sides.left;
        else
        {
            if(moveCode.movingRight)
                tackleSide = Sides.right;
            else if(moveCode.movingLeft)
                tackleSide = Sides.left;
        }

        //Play anticipation animation
        if(tackleSide == Sides.right)
        {
            //animation
            animator.SetInteger("Power", 4);
            animator.SetTrigger("PowerTrigger");
        }
        else
        {
            //animation
            animator.SetInteger("Power", 3);
            animator.SetTrigger("PowerTrigger");
        }
    }

    //Do the tackle, fix colliders and start the timer of the tackle
    private void Tackle()
    {
        // Open the tackle colliders that push and close the ones that detects the players nearby
        tacklers.SetActive(true);
        if(!unlimitedPower)
            playerDetectors.SetActive(false);

        // Add the tackle force
        if(tackleSide == Sides.right)
            rb.AddForce(transform.right * tackleForce, ForceMode.Impulse);
        else
            rb.AddForce(-transform.right * tackleForce, ForceMode.Impulse);

        // Play a timer for the tackle ending
        Invoke("FinishTackling", tackleTO);
    }

    private void FinishTackling()
    {
        //close the tackle colliders
        tacklers.SetActive(false);

        //change the animation parameter for entering the recovery animation
        if(tackleSide == Sides.right)
            animator.Play("TackleR_Rec");
        else
            animator.Play("TackleL_Rec");
    }

    #endregion

}