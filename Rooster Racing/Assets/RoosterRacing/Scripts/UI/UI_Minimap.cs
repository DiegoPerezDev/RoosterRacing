using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathCreation;
using UnityEngine.UI;

public class UI_Minimap : MonoBehaviour
{
    [SerializeField] private PathCreator pathCreator;
    [SerializeField] private GameObject playersContainer, MinimapPlayerPrefab;
    private List<Transform> playersInMap = new List<Transform>();
    private List<CH_Movement> playersMoveCodes = new List<CH_Movement>();

    void Start()
    {
        foreach (Transform child in playersContainer.GetComponentInChildren<Transform>())
        {
            var playerInMap = Instantiate(MinimapPlayerPrefab);
            playerInMap.transform.SetParent(transform, false);
            playerInMap.GetComponent<RawImage>().color = playersInMap.Count > 0? Color.yellow : Color.red;
            playersInMap.Add(playerInMap.transform);
            playersMoveCodes.Add(child.GetComponent<CH_Movement>());
        }
    }

    void FixedUpdate()
    {
        MovePlayersOnMinimap();
    }


    private void MovePlayersOnMinimap()
    {
        for (int i = 0; i < playersInMap.Count; i++)
            playersInMap[i].position = pathCreator.path.GetPointAtTime(playersMoveCodes[i].timeOnPath);
    }

}