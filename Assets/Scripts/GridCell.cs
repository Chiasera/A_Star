using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridCell : Cell
{
    [SerializeField]
    private Vector2Int gridPosition;
    private Grid grid;

    public Vector2Int GridPosition { get { return gridPosition; } }

    public void Initialize(Vector2Int position, CellType type, Grid grid)
    {
        base.Initialize(type);
        gridPosition = position;
        this.grid = grid;
    }

    //returns h* for a given grid cell
    public override float Heuristic(Cell target)
    {
        return grid.ManhatanDistance(gridPosition, ((GridCell)target).gridPosition);
    }
}
