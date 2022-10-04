using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*  INSTRUCTIONS:
 *  Last full check: V0.0.2
 *  This class detects other players nearby, it's used for the tackle power for now.
 */
public class CH_PlayerDetector : MonoBehaviour
{
    [Tooltip("Is this the right or the left detector?")]
    [SerializeField] private bool rightSide = false;
    private CH_Powers powersCode; //this script is currently in the grandparent


    void Awake()
    {
        //Get components
        powersCode = transform.parent.parent.gameObject.GetComponent<CH_Powers>();

        //if there is a component mising then stop the game
        if(powersCode == null)
        {
            print("gameObject not found!");
            Debug.Break();
        }
    }
    
    //it can only trigger with the player because of the project settings for better performance
    private void OnTriggerEnter(Collider other)
    {
        if(rightSide)
            powersCode.enemyOnRightSide = true;
        else
            powersCode.enemyOnLeftSide = true;
    }

    //it can only trigger with the player because of the project settings for better performance
    private void OnTriggerExit(Collider other)
    {
        if(rightSide)
            powersCode.enemyOnRightSide = false;
        else
            powersCode.enemyOnLeftSide = false;
    }
}
