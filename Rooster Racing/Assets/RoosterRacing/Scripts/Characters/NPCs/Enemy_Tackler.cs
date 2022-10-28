using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*  INSTRUCTIONS:
 *  Last full check: V0.0.2
 *  This class manages an enemy bot that tackle to both sides, switching between them. 
 *  This bot is for testing purposes only.
 */
[RequireComponent(typeof(CH_Powers))]
public class Enemy_Tackler : MonoBehaviour
{
    [Range(1, 5)] [SerializeField] private int delay = 3;
    private CH_Powers powersCode;
    private CH_Movement moveCode;


    private void Awake()
    {
        //find the components needed
        moveCode = GetComponent<CH_Movement>();
        powersCode = GetComponent<CH_Powers>();
    }

    void Start()
    {
        //prepare this bot so it can use the Tackle power constantly
        powersCode.unlimitedPower = true;
        powersCode.alwaysTackle = true;
        powersCode.currentPower = CH_Powers.Powers.tackle;
        
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
        powersCode.ActivatePower();
        yield return new WaitForSeconds(delay);
        moveCode.movingRight = false;
        moveCode.movingLeft = true;
        powersCode.ActivatePower();

        StartCoroutine(EndlessTackle());
    }
}