using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyBox;

/*  INSTRUCTIONS:
 *  Last full check: V0.3
 *  Script attached to the teleporters of the scenes in the level selection scene.
 *  This script enables the functionality for the player to go to a certain scene when colliding with this gameObject or shows a message in case that the scene is not enabled.
 */
public class LevelSelectionTeleporter : MonoBehaviour
{
    [SerializeField] private bool inDevelopment;
    [ConditionalField("inDevelopment", true)] [SerializeField] private int levelIndexToTeleport;
    private bool teleporting;
    private readonly string inDevelopmentMessage = "In Development";


    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (inDevelopment)
                UI_PopUps.ShowMessage(inDevelopmentMessage);
            else if (!teleporting)
            {
                teleporting = true;
                GameManager.EnterScene(levelIndexToTeleport);
            }
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
            UI_PopUps.HideMessage();
    }

}