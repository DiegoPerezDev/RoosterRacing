using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*  INSTRUCTIONS:
 *  Last full check: V0.3
 *  This script has the management of the inputs of the player in-game.
 *  This script does not have the inputs for the menu management.
 */
public class PlayerInputs : MonoBehaviour
{
    private CH_Powers powersCode;
    private CH_Movement movementCode;


    void Awake()
    {
        InputsManager.playerInputsCode = this;
        powersCode = GetComponent<CH_Powers>();
        movementCode = GetComponent<CH_Movement>();
    }

    void Start()
    {
        SetPlayerInputsCallbacks();
    }

    void OnDisable() => RemovePlayerInputsCallbacks();


    /// <summary>
    /// Set the call backs of the input action maps of the player, this mean that we tell what to do when performing the inputs.
    /// </summary>
    private void SetPlayerInputsCallbacks()
    {
        if (InputsManager.input == null)
        {
            print("Cant initialize the player inputs because there is no input system.");
            return;
        }
        else
            InputsManager.input.PlayerActionMap.Enable();

#if UNITY_STANDALONE
        InputsManager.input.PlayerActionMap.pause.performed     += ctx => GameManager.OnPauseByTime?.Invoke(true);
        InputsManager.input.PlayerActionMap.right.started       += ctx => movementCode.movingRight = true;
        InputsManager.input.PlayerActionMap.right.canceled      += ctx => movementCode.movingRight = false;
        InputsManager.input.PlayerActionMap.left.started        += ctx => movementCode.movingLeft = true;
        InputsManager.input.PlayerActionMap.left.canceled       += ctx => movementCode.movingLeft = false;
        InputsManager.input.PlayerActionMap.accelerate.started  += ctx => movementCode.SetMoveTrigger(true);
        InputsManager.input.PlayerActionMap.accelerate.canceled += ctx => movementCode.SetMoveTrigger(false);
        InputsManager.input.PlayerActionMap.jump.performed      += ctx => movementCode.jumpTrigger = true;
        InputsManager.input.PlayerActionMap.power.performed     += ctx => powersCode.ActivatePower();
        InputsManager.input.PlayerActionMap.lookBack.started    += ctx => CameraManager.ChangeCameraDisplaying(true);
        InputsManager.input.PlayerActionMap.lookBack.canceled   += ctx => CameraManager.ChangeCameraDisplaying(false);
#endif
    }
    private void RemovePlayerInputsCallbacks()
    {
        if (InputsManager.input == null)
            return;

#if UNITY_STANDALONE
        InputsManager.input.PlayerActionMap.pause.performed     -= ctx => GameManager.OnPauseByTime?.Invoke(true);
        InputsManager.input.PlayerActionMap.right.started       -= ctx => movementCode.movingRight = true;
        InputsManager.input.PlayerActionMap.right.canceled      -= ctx => movementCode.movingRight = false;
        InputsManager.input.PlayerActionMap.left.started        -= ctx => movementCode.movingLeft = true;
        InputsManager.input.PlayerActionMap.left.canceled       -= ctx => movementCode.movingLeft = false;
        InputsManager.input.PlayerActionMap.accelerate.started  -= ctx => movementCode.SetMoveTrigger(true);
        InputsManager.input.PlayerActionMap.accelerate.canceled -= ctx => movementCode.SetMoveTrigger(false);
        InputsManager.input.PlayerActionMap.jump.performed      -= ctx => movementCode.jumpTrigger = true;
        InputsManager.input.PlayerActionMap.power.performed     -= ctx => powersCode.ActivatePower();
        InputsManager.input.PlayerActionMap.lookBack.started    -= ctx => CameraManager.ChangeCameraDisplaying(true);
        InputsManager.input.PlayerActionMap.lookBack.canceled   -= ctx => CameraManager.ChangeCameraDisplaying(false);
#endif
    }

}