using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.VFX;

public class GameManager : MonoBehaviour
{
    private Grid grid;
    [SerializeField]
    private GameObject agentPrefab;
    [SerializeField]
    private GameObject chairPrefab;
    [SerializeField]
    private GameObject goalPortal;
    public static Cell goalCell;
    public static List<Cell> availableCells = new List<Cell>();
    public static List<AgentAI> humans;
    public static List<AgentAI> chairs;
    public static List<CancellationTokenSource> cancellationTokens;
    public bool drawSplinesMultithreaded;
    public static bool gameStopped = false;
    public bool stopOnFirstSuccess;
    private static bool StopOnFirstSuccess = false;
    public static bool isGoalCellUpdating = false;
    public bool Swag = false;
    public static int gameCounter;
    private Coroutine countCoroutine;

    // Start is called before the first frame update
    async void Start()
    {
        StopOnFirstSuccess = stopOnFirstSuccess;
        if (cancellationTokens == null)
        {
            cancellationTokens = new List<CancellationTokenSource>();
        }
        if (availableCells == null)
        {
            availableCells = new List<Cell>();
        }
        if (humans == null)
        {
            humans = new List<AgentAI>();
        }
        if (chairs == null)
        {
            chairs = new List<AgentAI>();
        }
        humans.Clear();
        chairs.Clear();
        availableCells.Clear();
        grid = FindObjectOfType<Grid>();
        grid.GenerateGrid(GridType.Connected8);
        //Set random goal onto the map
        int randGoalPosition = UnityEngine.Random.Range(0, availableCells.Count - 1);
        goalCell = availableCells[randGoalPosition];
        goalCell.CellType = CellType.Goal; //randomly choose a goal
        Instantiate(goalPortal, goalCell.transform.position, Quaternion.identity);
        availableCells.Remove(goalCell);
        for (int i = 0; i < 30; i++)
        {
            //don't await, activate all at the same time
            SpawnAgent();
        }
        for (int i = 0; i < 20; i++)
        {
            SpawnChair();
        }
        await Task.Delay(2000);
        StartCoroutine(ActivateAgents());
        countCoroutine = StartCoroutine(StartGameCount());
    }

    private IEnumerator StartGameCount()
    {
        float count = 0;
        while(count < 10)
        {
            count += Time.deltaTime;
            yield return null;
        }
        if(gameCounter == 0)
        {
            StopGame();
        }
    }

    IEnumerator ActivateAgents()
    {
        foreach (NavMeshAgent human in humans)
        {
            yield return human.Activate(GameManager.goalCell);
        }

        foreach (ChairAI chair in chairs)
        {
            chair.Activate(humans, 500);
        }
    }

    private void Update()
    {
        Debug.Log(gameCounter);
    }

    public void SpawnAgent()
    {
        CancellationTokenSource token = new CancellationTokenSource();
        cancellationTokens.Add(token);
        int randAgentPosition = UnityEngine.Random.Range(0, availableCells.Count - 1);
        HumanAI agent = Instantiate(agentPrefab).GetComponent<HumanAI>();
        agent.transform.position = availableCells[randAgentPosition].transform.position + Vector3.up * 1.5f;
        agent.SetCurrentCell(availableCells[randAgentPosition]);
        availableCells.Remove(availableCells[randAgentPosition]);
        humans.Add(agent);
    }

    //assuming the chair will be spawn after the player
    public void SpawnChair()
    {
        int randomChairPosition = UnityEngine.Random.Range(0, availableCells.Count - 1);
        ChairAI chair = Instantiate(chairPrefab).GetComponent<ChairAI>();
        chair.transform.position = availableCells[randomChairPosition].transform.position + Vector3.up * 1.5f;
        chair.SetCurrentCell(availableCells[randomChairPosition]);
        //availableCells[randomChairPosition].CellType = CellType.Obstacle;
        availableCells.Remove(availableCells[randomChairPosition]);
        chairs.Add(chair);
    }

    public void StopGame()
    {
        StopCoroutine(countCoroutine);
        if (!gameStopped)
        {
            isGoalCellUpdating = true;
            gameStopped = true;
            foreach (var cancelToken in cancellationTokens)
            {
                //stop all async operations, avoid memory leaks and allow for garbage collection
                cancelToken.Cancel();
                cancelToken.Dispose();
            }
            cancellationTokens.Clear();
            Destroy(FindObjectOfType<VisualEffect>().gameObject);
            int randGoalPosition = UnityEngine.Random.Range(0, availableCells.Count - 1);
            goalCell = availableCells[randGoalPosition];
            goalCell.CellType = CellType.Goal; //randomly choose a goal
            Instantiate(goalPortal, goalCell.transform.position, Quaternion.identity);
            if (!StopOnFirstSuccess)
            {
                StartCoroutine(ActivateAgents());
                Debug.Log("GAME FINISHED!!!");
            }
            gameStopped = false;
            gameCounter = 0;
            isGoalCellUpdating= false;
        }
        countCoroutine = StartCoroutine(StartGameCount());
    }

    private void OnApplicationQuit()
    {
        StopGame();
        DisableVFX();
    }

    private void DisableVFX()
    {
        foreach (var vfx in FindObjectsOfType<VisualEffect>())
        {
            Destroy(vfx.gameObject);
        }
    }
}
