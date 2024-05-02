using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Threading.Tasks;


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
        SetRacePlacing();
    }


    private async Task SetRacePlacing()
    {
        await Task.Delay(200);
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

    /// <summary>
    /// Get the players position in the race by tracking them in the path of the 'PathCreator' library. 
    /// Then sorting them from the one nearest to the start till the one nearest to the end of the path (from last pos to first pos, descending sorting).
    /// Finally, save the race placing of each competitor on a list.
    /// </summary>
    private void GetPlayersPlaceAtRace()
    {
        // Get 'time' on path of each player (0 to 1 mapping from start to end of the road position)
        List<float> toSort = new List<float>();
        for (int i = 0; i < competitors.Count; i++)
            toSort.Add(competitors[i].MoveCode.timeOnPath);

        // Sort players by position in the race (3rd, 2nd...)
        var sorting = toSort
            .Select((x, i) => new KeyValuePair<float, float>(x, i))
            .OrderBy(x => x.Key)
            .ToList();
        var playerPlacingAscending = sorting.Select(x => (int)x.Value).ToList();
        playerPlacingDescending = Enumerable.Reverse(playerPlacingAscending).ToList();

        // Save race placing of each competitor (e.g. player #1 race place #3)
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
        ResetCollectionVariables();
    }

    private void ResetCollectionVariables()
    {
        competitors = new List<Competitor>();
        playersFinalScoresAscending = new List<int>();
        prevCompetitorOrder = new List<int>();
        playerPlacingDescending = new List<int>();
    }

}