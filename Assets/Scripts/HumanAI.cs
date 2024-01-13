using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class HumanAI : NavMeshAgent
{
    private async void OnCollisionEnter(Collision collision)
    {
       if (collision.gameObject.CompareTag("Chair"))
        {
            if (GameManager.goalCell != null)
            {
                if(token != null && !token.IsCancellationRequested && !isComputingPath)
                {
                    await SetTarget(GameManager.goalCell);
                    Debug.Log("RECALCULATING PATH ...");
                }               
            }
        }
        if (collision.gameObject.CompareTag("Human"))
        {
            bool isFartherToTarget = Vector3.Distance(transform.position, GameManager.goalCell.transform.position)
                > Vector3.Distance(collision.transform.position, GameManager.goalCell.transform.position);
            //if behind or both are collision in same direction, make them seperate and compute each a new A* instance
            if (isFartherToTarget)
            {
                if(GameManager.goalCell != null)
                {
                    if (token != null && !token.IsCancellationRequested && !isComputingPath)
                    {
                        await SetTarget(GameManager.goalCell);
                        Debug.Log("RECALCULATING PATH ...");
                    }
                }            
            }
        }      
    }

    private void OnDestroy()
    {
        GameManager.humans.Remove(this);
        Debug.Log(GameManager.humans.Count);
        currentCell.CellType = CellType.Basic;
        if(spline != null)
        {
            Destroy(spline.gameObject);
        }
        GameManager.gameCounter++;
        FindObjectOfType<GameManager>().StopGame();
    }
}
