using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/*  INSTRUCTIONS:
 *  Last full check: V0.3
 *  This code manages what happens in the HUD canvas.
 *  Not all the levels displays the same information in the HUD, the free test mode and the level selection are different than the rest of the levels.
 */
public class UI_HUD : MonoBehaviour
{
    // HUD fields
    [Tooltip("Only changable before play-mode")]
    [SerializeField] private bool showFPS, showCurrentPower, showPowerSelection, showRotationMode, showAccelerationMode;
    [SerializeField] private TextMeshProUGUI fpsTMP;
    [SerializeField] private TextMeshProUGUI currentPowerTMP;
    [SerializeField] private ToggleGroup powerSelectionToggleGroup;
    [SerializeField] private Toggle unlimiterPowerToggle;
    [SerializeField] private Toggle autoRotationToggle, manualRotationToggle;
    [SerializeField] private Toggle autoAccelerationToggle, manualAccelerationToggle;
    private int fps;

    // Player fields
    private CH_Powers powersCode;
    private CH_Movement movementCode;


    // -- -- -- MONOBEHAVIOUR -- -- -- --
    void OnEnable()
    {
        if (showCurrentPower)
        {
            CH_Powers.OnPowerAcquired += ShowPowerText;
            CH_Powers.OnPowerUsed     += PowerUsed;
        }
        if (showPowerSelection)
            CH_Powers.OnPowerUsed += DeselectPowerSelectionToggles;
    }
    void OnDisable()
    {
        if (showCurrentPower)
        {
            CH_Powers.OnPowerAcquired -= ShowPowerText;
            CH_Powers.OnPowerUsed     -= PowerUsed;
        }
        if (showPowerSelection)
            CH_Powers.OnPowerUsed -= DeselectPowerSelectionToggles;
        StopCoroutine(FPSCoroutine());
    }
    void Awake()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            powersCode = player.GetComponent<CH_Powers>();
            movementCode = player.GetComponent<CH_Movement>();
        }
        else
            print("Player not found!");
    }
    void Start()
    {
        if(fpsTMP)
        {
            if (!showFPS)
                fpsTMP.gameObject.transform.parent.gameObject.SetActive(false);
            else
                StartCoroutine(FPSCoroutine());
        }
        if (!showCurrentPower && currentPowerTMP)
            currentPowerTMP.gameObject.transform.parent.gameObject.SetActive(false);
        if (!showPowerSelection && unlimiterPowerToggle)
        {
            unlimiterPowerToggle.gameObject.SetActive(false);
            powerSelectionToggleGroup.gameObject.SetActive(false);
        }
        if (!showRotationMode)
        {
            if (autoRotationToggle && movementCode)
            {
                autoRotationToggle.gameObject.transform.parent.parent.gameObject.SetActive(false);
                autoRotationToggle.isOn = movementCode.rotationMode == CH_Movement.MovementModes.Auto;
            }
        }
        
        if (!showAccelerationMode)
        {
            if (autoAccelerationToggle && movementCode)
            {
                autoAccelerationToggle.gameObject.transform.parent.parent.gameObject.SetActive(false);
                autoAccelerationToggle.isOn = movementCode.accelerationMode == CH_Movement.MovementModes.Auto;
            }
        }
    }
    void Update()
    {
        fps++;
    }


    // -- -- -- POWER SELECTION PANEL -- -- --
    public void Toggle_InfinitePowerAmount(bool enable) => powersCode.unlimitedPower = enable;
    public void Toggles_SetPower(int powerNum)
    {
        var UISelected = EventSystem.current.currentSelectedGameObject;
        var toggleSelected = UISelected.GetComponent<Toggle>();
        if (toggleSelected.isOn)
            CH_Powers.OnPowerAcquired?.Invoke((CH_Powers.Powers)powerNum);
        else
            CH_Powers.OnPowerAcquired?.Invoke(CH_Powers.Powers.none);
    }
    private void DeselectPowerSelectionToggles() => powerSelectionToggleGroup.SetAllTogglesOff();


    // -- -- -- CURRENT POWER PANEL -- -- --
    private void ShowPowerText(CH_Powers.Powers power) => currentPowerTMP.text = $"Power: \n'{power}'";
    private void PowerUsed()
    {
        if (powersCode.unlimitedPower)
            return;
        ShowPowerText(CH_Powers.Powers.none);
    }


    // -- -- -- MOVEMENT MODES PANEL -- -- --
    public void Toggles_SetPlayerAccelerationMode(int mode) => 
        Toggles_SetPlayerAccelerationMode((CH_Movement.MovementModes)mode);
    public void Toggles_SetPlayerRotationMode(int mode) => 
        Toggles_SetPlayerRotationMode((CH_Movement.MovementModes)mode);
    private void Toggles_SetPlayerAccelerationMode(CH_Movement.MovementModes mode)
    {
        movementCode.accelerationMode = mode;
        movementCode.moveTrigger = (mode == CH_Movement.MovementModes.Auto);
    }
    private void Toggles_SetPlayerRotationMode(CH_Movement.MovementModes mode)
    {
        movementCode.rotationMode = mode;
        if (mode == CH_Movement.MovementModes.Auto)
            movementCode.pathCreator.path.SetCharacterInPathFeatures(ref movementCode.pathFeatures, transform.position);
    }


    // -- -- -- FPS PANEL -- -- --
    private IEnumerator FPSCoroutine()
    {
        yield return new WaitForSecondsRealtime(1f);
        UpdateFPS();
        fps = 0;
        StartCoroutine(FPSCoroutine());
    }
    private void UpdateFPS() => fpsTMP.text = $"FPS: {fps}";

}