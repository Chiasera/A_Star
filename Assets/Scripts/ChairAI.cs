using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class ChairAI: AgentAI 
{
    private AgentAI targetHuman;
    private Vector3 targetPosition;
    private float distanceForecast = 5;
    // Start is called before the first frame update

    public AgentAI GetClosestHuman(List<AgentAI> humans)
    {
        float closestDistance = Mathf.Infinity;
        if (GameManager.humans.Count > 0)
        {
            AgentAI closestHuman = GameManager.humans[0];
            foreach (HumanAI human in humans)
            {
                float nextDistance = Vector3.Distance(transform.position, human.transform.position);
                if ( nextDistance < closestDistance)
                {
                    closestHuman = human;
                    closestDistance = nextDistance;
                }
            }
            return closestHuman;
        }
        return null;
    }

    public async void Activate(List<AgentAI> humans, int delayMilliseconds)
    {
        CancellationTokenSource token = new CancellationTokenSource();
        GameManager.cancellationTokens.Add(token);
        await Task.Delay(delayMilliseconds);
        isAsleep = false;
        targetHuman = GetClosestHuman(humans);
    }

    protected override void AvoidObstacles(List<AgentAI> agents, float repulsionRange, bool onApproachDescelerate = false)
    {
        // Repulsion logic
        Vector3 repulsionForce = Vector3.zero;
        foreach (ChairAI chair in GameManager.chairs) 
        {
            if (chair != this)  // Exclude the current chair
            {
                float distance = Vector3.Distance(transform.position, chair.transform.position);
                if (distance < repulsionRange)
                {
                    // Calculate the repulsion direction and scale by how close they are
                    Vector3 direction = (transform.position - chair.transform.position).normalized;
                    //(repulsionRange - distance) must be positive
                    repulsionForce += direction * (repulsionRange - distance);
                    Debug.DrawRay(transform.position, repulsionForce, Color.white);
                    transform.position += repulsionForce * Time.deltaTime;
                }
            }
        }
    }

    private void Update()
    {
        if (isActiveAndEnabled)
        {
            if (targetHuman != null)
            {
                targetPosition = targetHuman.transform.position + targetHuman.transform.forward * distanceForecast;
                transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * movementSpeed);
                // Calculate the target rotation based on the velocity direction
                Quaternion targetRotation = Quaternion.LookRotation(targetHuman.transform.position - transform.position);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
            if (!isAsleep)
            {
                targetHuman = GetClosestHuman(GameManager.humans);
            }
            AvoidObstacles(GameManager.chairs, 3.0f);
        }      
    }


    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Cell"))
        {
            collision.gameObject.GetComponent<Cell>().CellType = CellType.Obstacle;
        }
    }

    protected virtual void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Cell"))
        {
            collision.gameObject.GetComponent<Cell>().CellType = CellType.Basic;
        }
    }
}
