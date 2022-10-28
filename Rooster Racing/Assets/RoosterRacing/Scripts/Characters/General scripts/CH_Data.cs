using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CH_Data : MonoBehaviour
{
    public string playerName;
    [HideInInspector] public int playerNumber;
    [HideInInspector] public bool imPlayer;

    void Awake()
    {
        imPlayer = GetComponent<PlayerInputs>() != null;
    }
}