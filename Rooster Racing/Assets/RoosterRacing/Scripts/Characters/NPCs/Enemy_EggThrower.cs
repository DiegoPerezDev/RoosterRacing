using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*  INSTRUCTIONS:
 *  Last full check: V0.3
 *  This class manages a bot that use the power of throwing eggs every certine time.
 *  This bot is for testing purposes only.
 */
[RequireComponent(typeof(CH_Powers))]
public class Enemy_EggThrower : MonoBehaviour
{
    [Range(1,5)] [SerializeField] private int delay = 3;
    private CH_Powers powersCode;

    void Awake() => powersCode = GetComponent<CH_Powers>();

    void Start()
    {
        powersCode.unlimitedPower = true;
        powersCode.currentPower = CH_Powers.Powers.eggThrow;
        if (delay < 1) 
            delay = 1;
        StartCoroutine(ThrowerBehaviour());
    }

    //wait a moment and then throw the egg in an endless loop
    private IEnumerator ThrowerBehaviour()
    {
        yield return new WaitForSeconds(delay);
        powersCode.ActivatePower();
        StartCoroutine(ThrowerBehaviour());
    }
}
