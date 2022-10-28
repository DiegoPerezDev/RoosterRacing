using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class RaceManager : MonoBehaviour
{
    public List<Competitor> competitors = new List<Competitor>();
    public static List<string> playerName = new List<string>();
    public static List<int> playersFinalScoresAscending = new List<int>();
    [SerializeField] private UI_HUD hud;
    private List<int> prevCompetitorOrder = new List<int>();
    private List<int> playerPlacingDescending = new List<int>();
    

    public struct Competitor
    {
        public Competitor(Rigidbody rb, string playerName, CH_Movement moveCode)
        {
            this.Rb = rb;
            this.PlayerName = playerName;
            this.MoveCode = moveCode;
            this.RacePlace = 0;
        }
        public Rigidbody Rb { get; set; }
        public CH_Movement MoveCode;
        public string PlayerName { get; set; }
        public int RacePlace;
    }


    void OnEnable()
    {
        GameManager.OnWinGame  += RaceEnd;
        GameManager.OnLoseGame += RaceEnd;
    }

    void OnDisable()
    {
        GameManager.OnWinGame  -= RaceEnd;
        GameManager.OnLoseGame -= RaceEnd;
    }

    void Start()
    {
        StartHudPlacingPanel();
        StartCoroutine(UpdatePlayerRacePlacing());
    }


    private IEnumerator UpdatePlayerRacePlacing()
    {
        GetPlayersPlaceAtRace();
        SaveRacingOrder();

        // Delay and restart
        float timer = 0f;
        float delay = 0.2f;
        while(timer < delay)
        {
            timer += Time.deltaTime;
            yield return null;
        }
        StartCoroutine(UpdatePlayerRacePlacing());
    }

    private void GetPlayersPlaceAtRace()
    {
        // Get time on path
        List<float> toSort = new List<float>();
        for (int i = 0; i < competitors.Count; i++)
            toSort.Add(competitors[i].MoveCode.timeOnPath);

        // Sort
        var sorting = toSort
            .Select((x, i) => new KeyValuePair<float, float>(x, i))
            .OrderBy(x => x.Key)
            .ToList();
        var playerPlacingAscending = sorting.Select(x => (int)x.Value).ToList();
        playerPlacingDescending = Enumerable.Reverse(playerPlacingAscending).ToList();

        // Race placing
        var competitorsInOrder = new List<int>();
        for (int i = 0; i < competitors.Count; i++)
        {
            var temp = competitors[playerPlacingDescending[i]];
            temp.RacePlace = i + 1;
            competitors[playerPlacingDescending[i]] = temp;
        }
    }

    private void SaveRacingOrder()
    {
        if (prevCompetitorOrder.Count == competitors.Count)
        {
            for (int i = 0; i < competitors.Count; i++)
            {
                if (prevCompetitorOrder[i] != playerPlacingDescending[i])
                {
                    hud.UpdatePlacingInfo(playerPlacingDescending);
                    break;
                }
            }
        }

        prevCompetitorOrder = new List<int>();
        for (int i = 0; i < competitors.Count; i++)
            prevCompetitorOrder.Add(playerPlacingDescending[i]);
    }

    private void StartHudPlacingPanel()
    {
        playerName = new List<string>();
        for (int i = 0; i < competitors.Count; i++)
            playerName.Add(competitors[i].PlayerName);

        GetPlayersPlaceAtRace();
        hud.UpdatePlacingInfo(playerPlacingDescending);
    }

    private void RaceEnd()
    {
        UI_RaceTransitions.UpdatePlacingInfo(playerPlacingDescending);
    }

}