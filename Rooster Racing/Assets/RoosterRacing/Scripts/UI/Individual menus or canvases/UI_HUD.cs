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
    [SerializeField] private bool showFPS, showCurrentPower, showPowerSelection, showRotationMode, showAccelerationMode, showRacePlacing;

    // Power fields
    [SerializeField] private TextMeshProUGUI currentPowerTMP;
    [SerializeField] private ToggleGroup powerSelectionToggleGroup;
    [SerializeField] private Toggle unlimiterPowerToggle;
    private CH_Powers powersCode;

    // Race placing fields
    [SerializeField] private GameObject placingPanelGO;
    private List<TextMeshProUGUI> placingText;
    private readonly Color regularTextColor = new Color(10f/255f, 10f / 255f, 10f / 255f);
    private readonly Color playerTextColor = new Color(230f / 255f, 220f / 255f, 80f / 255f);

    // Displacement fields
    [SerializeField] private Toggle autoRotationToggle, manualRotationToggle;
    [SerializeField] private Toggle autoAccelerationToggle, manualAccelerationToggle;
    private CH_Movement movementCode;

    // FPS fields
    [SerializeField] private TextMeshProUGUI fpsTMP;
    private int fps;


    // -- -- -- MONOBEHAVIOUR -- -- -- --
    void OnEnable()
    {
        if (showCurrentPower)
        {
            CH_Powers.OnPowerAcquiredByPlayer += ShowPowerText;
            CH_Powers.OnPowerUsedByPlayer += PowerUsed;
        }
        if (showPowerSelection)
            CH_Powers.OnPowerUsedByPlayer += DeselectPowerSelectionToggles;
    }
    void OnDisable()
    {
        if (showCurrentPower)
        {
            CH_Powers.OnPowerAcquiredByPlayer -= ShowPowerText;
            CH_Powers.OnPowerUsedByPlayer -= PowerUsed;
        }
        if (showPowerSelection)
            CH_Powers.OnPowerUsedByPlayer -= DeselectPowerSelectionToggles;
        StopCoroutine(FPSCoroutine());
    }
    void Awake()
    {
        // Get player codes
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            powersCode = player.GetComponent<CH_Powers>();
            movementCode = player.GetComponent<CH_Movement>();
        }
        else
            print("Player not found!");

        // Race placing
        placingText = new List<TextMeshProUGUI>();
        if (placingPanelGO)
        {
            foreach (TextMeshProUGUI childText in placingPanelGO.GetComponentsInChildren<TextMeshProUGUI>())
                placingText.Add(childText);
        }
    }
    void Start()
    {
        // FPS
        if(fpsTMP)
        {
            if (!showFPS)
                fpsTMP.gameObject.transform.parent.gameObject.SetActive(false);
            else
                StartCoroutine(FPSCoroutine());
        }

        // Powers
        if (!showCurrentPower && currentPowerTMP)
            currentPowerTMP.gameObject.transform.parent.gameObject.SetActive(false);
        if (!showPowerSelection && unlimiterPowerToggle)
        {
            unlimiterPowerToggle.gameObject.SetActive(false);
            powerSelectionToggleGroup.transform.parent.gameObject.SetActive(false);
        }

        // Place in race
        if(!showRacePlacing)
        {
            if (placingPanelGO)
                placingPanelGO.SetActive(false);
        }

        // Player displacement
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
            CH_Powers.OnPowerAcquiredByPlayer?.Invoke((CH_Powers.Powers)powerNum);
        else
            CH_Powers.OnPowerAcquiredByPlayer?.Invoke(CH_Powers.Powers.none);
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
        movementCode.RestartPathFeatures();
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


    // -- -- -- RACE PLACING -- -- --
  
    public void UpdatePlacingInfo(List<int> descendingRaceOrder)
    {
        for (int i = 1; i < RaceManager.playerName.Count + 1; i++)
        {
            placingText[i - 1].text = $"{i}{(i > 3? "th": (i > 2 ? "rd" : (i > 1 ? "nd" : "st")))} place: {RaceManager.playerName[descendingRaceOrder[i - 1]]}";
            if (RaceManager.playerName[descendingRaceOrder[i - 1]] == "Player")
            {
                placingText[i - 1].color = playerTextColor;
                placingText[i - 1].fontStyle = FontStyles.Bold;
            }
            else
            {
                placingText[i - 1].color = regularTextColor;
                placingText[i - 1].fontStyle = FontStyles.Normal;
            }
        }
    }

}