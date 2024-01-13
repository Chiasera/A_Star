using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class NavMeshAgent : AgentAI
{
    public GameObject splinePrefab;
    protected Stack<Cell> path;
    protected Cell[] cellsArray;
    public Spline spline;
    public Stack<Cell> Path { get { return path; } }
    protected A_StarSearch pathSearch;
    [Range(1, 20)]
    public float pathCurvature = 1.0f;
    protected float repellForce = 1.0f;
    private float repellRadius = 4.0f;
    private int forecastDistance = 3;
    private int adjustForecastDistance = 5;
    public bool isComputingPath = false;
    private bool isIdle = false;
    private Vector3 currentPosition;
    private Vector3 lastPosition;
    protected CancellationTokenSource token;
    public bool Test;
    private float initialMoveSpeed;

    public bool IsComputingPath { get { return isComputingPath; } set { isComputingPath = value; } }
    public async Task SetTarget(Cell cell)
    {
        if (!isComputingPath && token != null && !token.IsCancellationRequested)
        {
            //if already on a path, don't lerp backwards to go to the current cell's center position
            //Instead, lerp towards the next cell on the path and set it as the current cell
            if (spline != null)
            {
                ChooseNextStartPosition(cell);
            }
            pathSearch = new A_StarSearch(currentCell, cell, FindObjectOfType<GameManager>().Swag);
            isComputingPath = true;
            path = await pathSearch.StartSearch(token);
            //for instance if search time > timeout
            if (path == null)
            {
                while (path == null && !token.IsCancellationRequested)
                {
                    //try to recompute path
                    await Task.Yield();
                    path = await pathSearch.StartSearch(token);
                }
            }
            if (path != null)
            {
                cellsArray = path.ToArray();
                if (path.Count > 0)
                {
                    nextCell = path.Pop();
                }
                UpdateSpline();
                await LerpToStartPosition();
            } else
            {
                IsComputingPath = false;
                await SetTarget(GameManager.goalCell);
            }
        }
    }

    private void ChooseNextStartPosition(Cell targetCell)
    {
        currentCell = currentCell.GetNeighbors().OrderBy(neighbor => neighbor.Heuristic(targetCell) * neighbor.TravelCost).First();
    }

    protected override void AvoidObstacles(List<AgentAI> agents, float repellForce, bool onApproachDescelerate = false)
    {
        foreach (var obstacle in agents)
        {
            if (isActiveAndEnabled && obstacle != null && obstacle != this)
            {
                //additional logic here hehehe
                if (Vector3.Distance(transform.position, GameManager.goalCell.transform.position)
                    > Vector3.Distance(obstacle.transform.position, GameManager.goalCell.transform.position)
                    && Vector3.Distance(transform.position, obstacle.transform.position) < repellRadius)
                {
                    if (onApproachDescelerate && Vector3.Distance(transform.position, obstacle.transform.position) < repellRadius/2)
                    {
                        movementSpeed = initialMoveSpeed / 2;
                    }
                    else
                    {
                        movementSpeed = initialMoveSpeed;
                    }
                    for (int i = 1; i < forecastDistance + 1; i++)
                    {
                        float relativeDistance = forecastDistance - (i) / 4;
                        int maxIndex = Mathf.FloorToInt(spline.T) + i;
                        if (maxIndex > 0 && maxIndex < spline.Knots.Count - 2)
                        {
                            SplineKnot knot = spline.Knots[maxIndex];
                            if (Vector3.Distance(knot.transform.position, obstacle.transform.position) < relativeDistance)
                            {
                                Vector3 directionToObstacle = obstacle.transform.position - knot.transform.position;
                                Vector3 tangent = spline.DerivativeAtSegment(spline.T % Mathf.Max(i, 1), Mathf.FloorToInt(spline.T));
                                //is the obstacle on the right or the left side of the curve ?
                                Vector3 obstaclePosition = Vector3.Cross(tangent, new Vector3(directionToObstacle.x, 0, directionToObstacle.z));
                                Debug.DrawRay(knot.transform.position, tangent, Color.yellow);
                                Vector3 perpendicular = Vector3.Cross(tangent, Vector3.up).normalized;
                                knot.transform.position += perpendicular * Time.deltaTime * repellForce * Mathf.Sign(obstaclePosition.y);
                                transform.Rotate(0, rotationSpeed * Time.deltaTime * Mathf.Sign(obstaclePosition.y), 0);
                            }
                        }                      
                    }
                }
            }
        }
    }

    private void Start()
    {
        currentPosition = transform.position;
        initialMoveSpeed = movementSpeed;
        Debug.Log(initialMoveSpeed);
        StartCoroutine(IsIdle());
    }

    protected void UpdateSpline()
    {
        if (spline == null)
        {
            spline = Instantiate(splinePrefab).GetComponent<Spline>();
        }
        spline.ClearPath();
        spline.Create(pathSearch.SplineKnotsPositions);
    }

    private async Task LerpToStartPosition()
    {
        Vector3 direction = spline.CurveInfo.position - transform.position;
        Quaternion targetRotation;
        while (direction.magnitude > 0.1f && !token.IsCancellationRequested)
        {
            if (token.IsCancellationRequested)
            {
                return;
            }
            else
            {
                transform.position += direction.normalized * Time.deltaTime * movementSpeed;
                direction = spline.CurveInfo.position - transform.position;
                targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
                await Task.Yield();
            }
        }
        isComputingPath = false;
        if (isAsleep) isAsleep = false;
    }

    protected void OnDrawGizmos()
    {
        if (path != null && path.Count > 0)
        {
            for (int i = 0; i < cellsArray.Length - 1; i++)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(cellsArray[i].transform.position + Vector3.up, cellsArray[i + 1].transform.position + Vector3.up);
            }
        }
    }

    public async Task Activate(Cell target)
    {
        token = new CancellationTokenSource();
        GameManager.cancellationTokens.Add(token);
        isComputingPath = false;
        await SetTarget(target);
        if (this.isActiveAndEnabled)
        {
            rb = GetComponent<Rigidbody>();
        }
    }

    protected virtual void Update()
    {
        OnCellChange();
        if (token != null && !isAsleep && !isComputingPath)
        {
            FollowPath();
            AvoidObstacles(GameManager.humans, 0.5f, true);
            AvoidObstacles(GameManager.chairs, 1.0f, false);
        }

        if (Vector3.Distance(transform.position, GameManager.goalCell.transform.position) < 2f)
        {
            Destroy(gameObject);
        }
    }

    IEnumerator IsIdle()
    {
        while (true)
        {
            yield return new WaitForSeconds(3);
            lastPosition = currentPosition;
            currentPosition = transform.position;
            if (Vector3.Distance(currentPosition, lastPosition) < 0.1f)
            {
                isIdle = true;
            }
            if (isIdle && !isComputingPath)
            {
                isIdle = false;
                yield return SetTarget(GameManager.goalCell);
            }
        }
    }


    public void FollowPath()
    {
        if (spline.SegmentLength > 0)
        {
            //somewhat constant velocity, no acceleration
            spline.T += Time.deltaTime * movementSpeed;
        }
        else
        {
            spline.T += Time.deltaTime;
        }
        transform.position = spline.CurveInfo.position;
        if (Vector3.Distance(transform.position, nextCell.transform.position) < 1.5f && Path.Count > 0)
        {
            nextCell = path.Pop();
            if (FindObjectOfType<GameManager>().Swag)
            {
                nextCell.CellType = CellType.Basic;
            }
        }
        // Calculate the target rotation based on the velocity direction
        Quaternion targetRotation = Quaternion.LookRotation(spline.CurveInfo.velocity.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}
