using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public enum CellType
{
    Basic, Special, Obstacle, Goal, Boundary
}
public abstract class Cell : MonoBehaviour
{
    [SerializeField]
    protected CellType cellType;
    [SerializeField]
    protected List<Cell> neighbors;
    public Material baseMat;
    public Material pathMat;
    public Material goalMat;
    public Material obstacleMat;
    protected Renderer _renderer;
    protected bool isOccupied;
    protected bool isBooked = false;


    protected float travelCost;
    public bool IsOccupied { get { return isOccupied; } set { isOccupied = value; } }
    public float TravelCost { get { return travelCost; } }
    public bool IsBooked { get { return isBooked; } set { isBooked = value; } }
    public CellType CellType
    {
        get { return cellType; }
        set
        {
            try
            {
                cellType = value;
                UpdateMaterial();
                UpdateTravelCost();
            }catch (MissingReferenceException e)
            {
                Debug.Log(e);
            }                
        }
    }

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        //OnAgentStandUpdate();
    }

    public void Initialize(CellType type)
    {
        cellType = type;
        neighbors = new List<Cell>();
        //add this cell to the available cells on initialization
        GameManager.availableCells.Add(this);
    }

    //Update the material -- for debug purposes
    private void UpdateMaterial()
    {
        switch (cellType)
        {
            case CellType.Obstacle:
                _renderer.material = obstacleMat;
                break;
            case CellType.Goal:
                _renderer.material = goalMat;
                break;
            case CellType.Basic:
                _renderer.material = baseMat;
                break;
            case CellType.Boundary:
                _renderer.material = goalMat;
                break;
        }
    }

    protected void UpdateTravelCost()
    {
        switch (cellType)
        {
            case CellType.Basic:
                travelCost = 1.0f;
                break;
            case CellType.Special:
                travelCost = 2.0f;
                break;
            case CellType.Obstacle:
                travelCost = Mathf.Infinity;
                break;
            case CellType.Goal:
                travelCost = 1.0f;
                break;
            case CellType.Boundary:
                travelCost = 1.0f;
                break;
        }
    }

    public List<Cell> GetNeighbors()
    {
        return neighbors;
    }

    private void Update()
    {
        if (this == GameManager.goalCell)
        {
            CellType = CellType.Goal;
        }
    }

    private async void OnAgentStandUpdate()
    {
        await Task.Delay(1000);
        RaycastHit hit;
        if (!Physics.Raycast(transform.position, Vector3.up, out hit))
        {
            try
            {
                CellType = CellType.Basic;
            }
            catch
            {
                //ignore
            }
        }
    }

    public void AddNeighbor(GridCell cell)
    {
        neighbors.Add(cell);
    }

    public void AddToPath()
    {
        if (this.cellType != CellType.Goal)
        {
            _renderer.material = pathMat;
        }
    }

    public void ResetCell()
    {
        _renderer.material = baseMat;
    }

    public Vector3 GetWorldPosition()
    {
        return transform.parent.position + transform.localPosition;
    }

    public abstract float Heuristic(Cell target);

    public override bool Equals(object obj)
    {
        // Check for null and compare run-time types.
        if ((obj == null) || !this.GetType().Equals(obj.GetType()))
        {
            return false;
        }
        else
        {
            Cell cell = (Cell)obj;
            return transform.position == cell.transform.position; // Assuming the unique identifier for a cell is its world position
        }
    }

    public override int GetHashCode()
    {
        // Use the position as the basis for the hash code, for example
        if(this != null)
        {
            return transform.position.GetHashCode();
        }
        return -1;
    }
}
