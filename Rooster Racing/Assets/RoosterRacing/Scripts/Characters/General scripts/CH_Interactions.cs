using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/*  INSTRUCTIONS: 
 *  Last full check: V0.0.2
 *  This class manages the player interaction with other objects in the game.
 */
public class CH_Interactions : MonoBehaviour
{
    //General fields
    private enum Tags { PowerUp, EggSet, EggThrowed, Hole, Teleporter, TackleL, TackleR, None}
    private Tags otherTag;

    //Power Box
    private static GameObject powerBoxPref;
    private static readonly float powerBoxRespawnDelay = 4f;

    //Egg powers fields
    private float eggStunDuration = 1.5f;
    private bool stunned;

    //Tackle power fields
    private enum Sides { left, right }
    [SerializeField] private float pushForce = 1000f;

    //Holes
    [SerializeField] private float holeDelay = 1f;
    private bool falling;
    private GameObject holesContainer;
    private GameObject[] hole = new GameObject[1];
    private Vector3[] afterHolePos = new Vector3[1];
    private float PlayerToAfterHole;

    //Teleport
    private Vector3 startPos;
    private Quaternion startRot;

    //General components fields
    private Rigidbody rb;
    private CapsuleCollider playerColl;
    private CH_Movement moveCode;
    private CH_Powers powerCode;
    private PlayerInputs playerInputs;

    void Awake()
    {
        //find the components needed
        moveCode = GetComponent<CH_Movement>();
        powerCode = GetComponent<CH_Powers>();
        rb = GetComponent<Rigidbody>();
        playerColl = GetComponent<CapsuleCollider>();
        playerInputs = GetComponent<PlayerInputs>();
        startPos = transform.position;
        startRot = transform.rotation;
        powerBoxPref = Resources.Load<GameObject>("Prefabs/InteractiveObjects/PowerBox");
        if(!powerBoxPref)
            print("No power box prefab found!");
        SetHoles();
    }


    //Main interactions functions
    #region Main functions

    private void OnTriggerEnter(Collider other) 
    {
        //Get the tag of the object collided, if its any of the enum then continue
        if(!TagConfirmed(other.gameObject.tag))
            return;

        //Check the guard condition of the transition intented, if its true then continue (SEE DOCUMENTATION)
        if(!GuardConfirmed())
            return;

        //Interactions (SEE DOCUMENTATION)
        switch(otherTag)
        {
            case Tags.EggSet:
                Destroy(other.gameObject);
                StartCoroutine(Stun(eggStunDuration));
            break;

            case Tags.EggThrowed:
                Destroy(other.gameObject);
                StartCoroutine(Stun(eggStunDuration));
            break;

            case Tags.TackleL:
                Pushed(Sides.left);
            break;

            case Tags.TackleR:
                Pushed(Sides.right);
            break;
        
            case Tags.PowerUp:
                CatchPowerBox(other.gameObject);
            break;

            case Tags.Hole:
                try
                {
                    FallInHole(other.gameObject);
                } 
                catch 
                {
                    print("can't tp to the afterHole position");
                    Debug.Break();
                }
            break;

            case Tags.Teleporter:
                transform.position = startPos;
                transform.rotation = startRot;
                moveCode.pathFeatures.WaitingForPathRestart = false;
                break;
        }
    }

    //Try to get the tag of the object collided
    private bool TagConfirmed(string tag)
    {
        try
        {  
            otherTag = (Tags)Enum.Parse(typeof(Tags), tag);
        }
        catch
        {  
            return false; 
        }
        return true; 
    }

    //Check the guard condition of the transition intented
    private bool GuardConfirmed()
    {
        switch(otherTag)
        {
            case Tags.EggSet:
                if(!stunned)
                    return true;
            break;

            case Tags.EggThrowed:
                if(!stunned)
                    return true;
            break;

            case Tags.TackleL:
                return true;

            case Tags.TackleR:
                return true;

            case Tags.PowerUp:
                    return true;

            case Tags.Hole:
                if(!falling)
                    return true;
            break;

            case Tags.Teleporter:
                return true;
        }

        //if the guard did not match the condition, guard wasn't confirmed
        return false;

    }

    #endregion


    #region Powers interactions functions

    //Stun by freezing the rigidbody constraints
    private IEnumerator Stun(float stunDuration)
    {
        //freeze the player physics
        stunned = true;
        rb.constraints = RigidbodyConstraints.FreezeAll;

        //wait a moment and enable the player physics
        yield return new WaitForSecondsRealtime(stunDuration);
        rb.constraints = RigidbodyConstraints.None;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        stunned = false;
    }

    //this is the push received from another player tackling(from his 'tackle collider')
    private void Pushed(Sides side)
    {
        if(side == Sides.right)
        {
            rb.AddForce(transform.right * pushForce, ForceMode.Impulse);
        }
        else
        {
            rb.AddForce(-transform.right * pushForce, ForceMode.Impulse);
        }
    }

    #endregion

    #region Hole interactions functions

    //Look for all the holes in the scene. The holes must use the correct nomenclature
    private void SetHoles()
    {
        holesContainer = GameObject.Find("LevelDev/Holes_");

        //If there is no holes in the scene then just skip this script
        if(holesContainer == null) return;

        //I used the double foreach to find and specific grandchild because the function 'FindChild()' also counts the self object
        int _ = 0;
        foreach(Transform childs in holesContainer.transform)
        {
            foreach(Transform tfs in childs)
            {
                afterHolePos[_] = tfs.position;
            }
            Array.Resize(ref afterHolePos, afterHolePos.Length + 1);
            hole[_] = childs.gameObject;
            Array.Resize(ref hole, hole.Length + 1);
            ++_;
        }
        if(_ > 0)
        {
            Array.Resize(ref hole, hole.Length - 1);
            Array.Resize(ref afterHolePos, afterHolePos.Length - 1);
        }
        //print("Number of holes found : " + _);
    }

    //disable the player move and prepare the transition to the afterHole position
    private void FallInHole(GameObject holeGO)
    {
        falling = true;

        //Don't let the player move or obstruct with something in the transition
        EnableBody(false);

        //Look for the position to teleport the player after the hole stun.
        Vector3 TeleportPos = Vector3.zero;
        for (int i = 0; i < hole.Length; ++i)
        {
            if(holeGO == hole[i])
            {
                TeleportPos = afterHolePos[i];
                break;
            }
        }

        //Transport the player to the position after the hole
        PlayerToAfterHole = Vector3.Distance(rb.transform.position, TeleportPos);
        StartCoroutine(HoleTransition(TeleportPos));
    }

    //disable gravity and script movements
    private void EnableBody(bool enable)
    {
        if(!enable)
            rb.velocity = Vector3.zero;
        playerColl.enabled = enable;
        rb.useGravity = enable;
        moveCode.enabled = enable;
        playerInputs.enabled = enable;
    }

    //Move the player smoothly to the position after the hole
    private IEnumerator HoleTransition(Vector3 TeleportPos)
    {
        //wait a moment falling before starting the transition
        yield return new WaitForSeconds(holeDelay);
        
        //transition of the player
        //I haven't test the performance of this coroutine, it might be laggy
        while(PlayerToAfterHole > 0.3f || PlayerToAfterHole < -0.3f)
        {
            //update player pos relative to the after hole position
            PlayerToAfterHole = Vector3.Distance(rb.transform.position, TeleportPos);

            //go to the position after the hole fall smoothly
            rb.transform.position = Vector3.Lerp(rb.transform.position, TeleportPos, 0.2f);
            yield return new WaitForFixedUpdate();
        }

        //Stop all the Hole debuffs so the player can continue moving
        falling = false;
        EnableBody(true);
    }

    #endregion

    #region Items interactions functions

    ///<summary> Obtain a power, destroy the box just catched and prepare the respawn of that box </summary>
    private void CatchPowerBox(GameObject Box)
    {
        //Destroy the box and call the respawn of the box in the 'Interactables' script
        StartCoroutine(PowerBoxRespawn(Box));

        //if unlimited power then there is no need of a box
        if(powerCode.unlimitedPower)
            return;

        //gather a new random power
        powerCode.currentPower = (CH_Powers.Powers)UnityEngine.Random.Range(0, Enum.GetNames(typeof(CH_Powers.Powers)).Length);

        //if got the tackle power, activate the player detectors needed for the tackle power
        if(powerCode.currentPower == CH_Powers.Powers.tackle)
        {
            powerCode.playerDetectors.SetActive(true);
        }
    }

    private IEnumerator PowerBoxRespawn(GameObject Box)
    {
        Destroy(Box);
        Vector3 BoxPos = Box.transform.position;
        yield return new WaitForSeconds(powerBoxRespawnDelay);
        Instantiate(powerBoxPref, BoxPos, Quaternion.identity);
    }

    #endregion

}