using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*  INSTRUCTIONS:
 *  Last full check: V0.0.2
 *  This class manages an enemy bot that tackle to both sides, switching between them. 
 *  This bot is for testing purposes only.
 */
public class Enemy_Tackler : MonoBehaviour
{
    [Range(1, 5)] [SerializeField] private int delay = 3;
    private CH_Powers PowersCode;
    private CH_Movement moveCode;


    private void Awake()
    {
        //find the components needed
        moveCode = GetComponent<CH_Movement>();
        PowersCode = GetComponent<CH_Powers>();

        //check if i got the components, else stop the game before any bug start
        if( (PowersCode == null) || (moveCode == null) )
        {
            print("A gameObject was not found in the code " + this.name + "!");
            Debug.Break();
        }
    }

    void Start()
    {
        //prepare this bot so it can use the Tackle power constantly
        PowersCode.unlimitedPower = true;
        PowersCode.alwaysTackle = true;
        PowersCode.currentPower = CH_Powers.Powers.tackle;
        
        //Start the tackle loop
        StartCoroutine(EndlessTackle());
    }

    //Tackle in an endless loop
    private IEnumerator EndlessTackle()
    {
        //Wait a moment and tackle to the left side, then do the same thing with the right side
        yield return new WaitForSeconds(delay);
        moveCode.movingRight = true;
        moveCode.movingLeft = false;
        PowersCode.powerTrigger = true;
        yield return new WaitForSeconds(delay);
        moveCode.movingRight = false;
        moveCode.movingLeft = true;
        PowersCode.powerTrigger = true;

        StartCoroutine(EndlessTackle());
    }
}