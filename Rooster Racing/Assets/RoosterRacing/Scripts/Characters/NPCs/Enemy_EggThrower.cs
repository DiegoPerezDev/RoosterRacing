using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*  INSTRUCTIONS:
 *  Last full check: V0.0.2
 *  This class manages a bot that use the power of throwing eggs every certine times.
 *  This bot is for testing purposes only.
 */
public class Enemy_EggThrower : MonoBehaviour
{
    [Range(1,5)] [SerializeField] private int delay = 3;
    private CH_Powers PowersCode;


    void Awake()
    {
        //find the components needed
        PowersCode = GetComponent<CH_Powers>();

        //check if i got the components, else stop the game before any bug start
        if(PowersCode == null)
        {
            print("A gameObject was not found in the code " + this.name + "!");
            Debug.Break();
        }
    }

    void Start()
    {
        //prepare this bot so it can use the EggThrow power constantly
        PowersCode.unlimitedPower = true;
        PowersCode.currentPower = CH_Powers.Powers.eggThrow;
        StartCoroutine(ThrowerBehaviour());
    }

    //wait a moment and then throw the egg in an endless loop
    private IEnumerator ThrowerBehaviour()
    {
        yield return new WaitForSeconds(delay);
        PowersCode.powerTrigger = true;
        StartCoroutine(ThrowerBehaviour());
    }
}
