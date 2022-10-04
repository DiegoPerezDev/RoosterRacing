using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*  INSTRUCTIONS
 *  Last full check: V0.0.2
 *  This class make a bot respawn to its default position every n seconds.
 *  This bot is for testing purposes only.
 */
public class Enemy_Idle : MonoBehaviour
{
    [Range(1f,20f)] [SerializeField] private int delay = 5;
    private Vector3 startPos;
    

    void Start()
    {
        startPos = transform.position;
        StartCoroutine(ReturnToStartPos());
    }

    private IEnumerator ReturnToStartPos()
    {
        yield return new WaitForSecondsRealtime(delay);
        transform.position = startPos;
        StartCoroutine(ReturnToStartPos());
    }
}
