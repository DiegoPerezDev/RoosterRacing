using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/*  INSTRUCTIONS: 
 *  Last full check: V0.0.2
 *  This class manages the player interaction with other objects in the game.
 */
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class CH_Interactions : MonoBehaviour
{
    //General fields
    private enum Tags { Untagged, none, PowerUp, EggSet, EggThrowed, Hole, Teleporter, TackleL, TackleR, Wall, RaceEnd}
    private bool invulnerable;
    private bool isPlayer;

    //Powers
    private GameObject powerBoxPref;
    private enum Sides { left, right }
    private readonly float eggStunDuration = 1.5f;

    //Obstacles
    private List<GameObject> holesGO, wallsGO;
    private List<Vector3> afterHolePos, afterWallPos;
    private IEnumerator ObstacleCoroutine;

    //General components fields
    private Rigidbody rb;
    private CapsuleCollider playerColl;
    private CH_Movement moveCode;
    private CH_Powers powerCode;
    private PlayerInputs playerInputs;
    private CH_Data playerData;


    void Awake()
    {
        //find the components needed
        rb           = GetComponent<Rigidbody>();
        playerColl   = GetComponent<CapsuleCollider>();
        playerInputs = GetComponent<PlayerInputs>();
        moveCode     = GetComponent<CH_Movement>();
        powerCode    = GetComponent<CH_Powers>();
        playerData   = GetComponent<CH_Data>();
        powerBoxPref = Resources.Load<GameObject>("Prefabs/InteractiveObjects/PowerBox");
        if(!powerBoxPref)
            print("No power box prefab found!");
        isPlayer = CompareTag("Player");
    }

    void Start()
    {
        SetHolesTpPos();
        SetWallsTpPos();
    }


    private void OnTriggerEnter(Collider other) 
    {
        Tags otherTag = ParseStringToEnumTag(other.gameObject.tag);

        switch(otherTag)
        {
            case Tags.EggSet:
                if (invulnerable)
                    break;
                ObstacleCoroutine = Stun(eggStunDuration);
                StartCoroutine(ObstacleCoroutine);
                break;

            case Tags.EggThrowed:
                if (invulnerable)
                    break;
                Destroy(other.gameObject);
                ObstacleCoroutine = Stun(eggStunDuration);
                StartCoroutine(ObstacleCoroutine);
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
                if(invulnerable)
                    break;
                FallInHole(other.gameObject);
                break;

            case Tags.Teleporter:
                moveCode.RestartPos();
                break;

            case Tags.RaceEnd:
                // Tell the race manager that we ended the race, store that data in the final score list
                RaceManager.playersFinalScoresAscending.Add(playerData.playerNumber);

                // End game if the player is the one finishing the race.
                if (CompareTag("Player"))
                {
                    var finalPlace = RaceManager.playersFinalScoresAscending.Count;
                    moveCode.Stop();
                    moveCode.enabled = false;
                    if (finalPlace == 1)
                        GameManager.OnWinGame?.Invoke();
                    else
                        GameManager.OnLoseGame?.Invoke();
                }
                else
                    Destroy(gameObject);
                break;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Tags otherTag = ParseStringToEnumTag(collision.gameObject.tag);

        if(otherTag == Tags.Wall)
        {
            if (invulnerable)
                return;
            WallHit(collision.gameObject);
        }   
    }

    private Tags ParseStringToEnumTag(string tag)
    {
        Tags enumTag;
        try{
            enumTag = (Tags)Enum.Parse(typeof(Tags), tag);
        }
        catch{
            enumTag = Tags.none; 
        }
        return enumTag; 
    }


    #region Powers interactions functions

    //Stun by freezing the rigidbody constraints
    private IEnumerator Stun(float stunDuration)
    {
        //freeze the player physics
        invulnerable = true;
        rb.constraints = RigidbodyConstraints.FreezeAll;

        //wait a moment and enable the player physics
        yield return new WaitForSecondsRealtime(stunDuration);
        rb.constraints = RigidbodyConstraints.None;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        invulnerable = false;
    }

    //this is the push received from another player tackling(from his 'tackle collider')
    private void Pushed(Sides side)
    {
        float pushForce = 1000f;
        if (side == Sides.right)
            rb.AddForce(transform.right * pushForce, ForceMode.Impulse);
        else
            rb.AddForce(-transform.right * pushForce, ForceMode.Impulse);
    }

    #endregion

    #region Obstacles interactions functions

    private void SetHolesTpPos()
    {
        holesGO = new List<GameObject>();
        afterHolePos = new List<Vector3>();
        var container = GameObject.Find("InteractiveObjects/Holes");
        if (container == null)
            return;

        foreach(Transform child in container.transform)
        {
            afterHolePos.Add(child.GetChild(0).transform.position);
            holesGO.Add(child.gameObject);
        }
    }

    private void SetWallsTpPos()
    {
        wallsGO = new List<GameObject>();
        afterWallPos = new List<Vector3>();
        var container = GameObject.Find("InteractiveObjects/Walls");
        if (container == null)
            return;

        foreach (Transform child in container.transform)
        {
            afterWallPos.Add(child.GetChild(0).transform.position);
            wallsGO.Add(child.gameObject);
        }
        var wallHalfHeigh = 0f;
        if (wallsGO.Count > 0)
            wallHalfHeigh = wallsGO[0].transform.localScale.y / 2;
        for (int i = 0; i < afterWallPos.Count; i++)
            afterWallPos[i] -= new Vector3(0, wallHalfHeigh, 0);
    }

    private void FallInHole(GameObject holeGO)
    {
        //Look for the position to teleport the player after the hole fall.
        var index = holesGO.FindIndex(x => x.Equals(holeGO));
        Vector3 TeleportPos = afterHolePos[index];
        TriggerObstacle(TeleportPos, true);
    }

    private void WallHit(GameObject wallGO)
    {
        //Look for the position to teleport the player after the wall hit.
        var index = wallsGO.FindIndex(x => x.Equals(wallGO));
        Vector3 TeleportPos = afterWallPos[index];
        TriggerObstacle(TeleportPos, true);
    }

    private void TriggerObstacle(Vector3 teleportPos, bool withDelay)
    {
        invulnerable = true;

        //Don't let the player move or obstruct with something in the transition
        EnableBody(false);

        //Transport the player to the desired position
        StartCoroutine(AfterObstacleTransition(teleportPos, withDelay));
    }

    private void EnableBody(bool enable)
    {
        if(!enable)
            rb.velocity = Vector3.zero;
        playerColl.enabled = enable;
        rb.useGravity = enable;
        moveCode.enabled = enable;
        if(isPlayer)
            playerInputs.enabled = enable;
    }

    /// <summary> Move the player smoothly to the desired position </summary>
    private IEnumerator AfterObstacleTransition(Vector3 TeleportPos, bool withDelay)
    {
        //wait a moment before starting the transition
        if(withDelay)
            yield return new WaitForSeconds(0.3f);

        //transition of the player
        float PlayerToNextPos = Vector3.Distance(rb.transform.position, TeleportPos);
        while (PlayerToNextPos > 0.3f || PlayerToNextPos < -0.3f)
        {
            //update player pos relative to the next desired position
            PlayerToNextPos = Vector3.Distance(rb.transform.position, TeleportPos);

            //go to the next desired positionsmoothly
            rb.transform.position = Vector3.Lerp(rb.transform.position, TeleportPos, 0.2f);
            yield return new WaitForFixedUpdate();
        }

        //Stop all the debuffs so the player can continue moving
        invulnerable = false;
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
        powerCode.OnPowerAcquired?.Invoke();
    }

    private IEnumerator PowerBoxRespawn(GameObject Box)
    {
        float powerBoxRespawnDelay = 4f;
        Destroy(Box);
        Vector3 BoxPos = Box.transform.position;
        yield return new WaitForSeconds(powerBoxRespawnDelay);
        Instantiate(powerBoxPref, BoxPos, Quaternion.identity);
    }

    #endregion

}