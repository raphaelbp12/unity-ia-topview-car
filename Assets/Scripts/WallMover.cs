using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallMover : MonoBehaviour
{
    [SerializeField] public WallPath wallPath;
    [SerializeField] float waypointTolerance = 1f;
    [SerializeField] float speed = 1f;
    public int currentWaypointIndex;

    private void Start()
    {
        restartMovement();
    }

    public void restartMovement()
    {
        currentWaypointIndex = 0;
        transform.position = GetCurrentWaypoint();
    }

    void FixedUpdate()
    {
        WallBehavior();
    }

    private void WallBehavior()
    {
        Vector3 nextPosition = GetCurrentWaypoint();

        if (wallPath != null)
        {
            if(AtWaypoint())
            {
                CycleWaypoint();
            }
            nextPosition = GetCurrentWaypoint();
        }
        moveTo(nextPosition);
    }

    private void moveTo(Vector3 nextPosition)
    {
        Vector3 direction = nextPosition - transform.position;
        float theta = ((float)Math.Atan2(direction.normalized.x, direction.normalized.z) * 180 / (float)Math.PI);

        transform.rotation = Quaternion.Euler(1, theta, 1);
        transform.position = transform.position + speed * (direction.normalized);
    }

    private Vector3 GetCurrentWaypoint()
    {
        return wallPath.GetWaypoint(currentWaypointIndex);
    }

    private void CycleWaypoint()
    {
        currentWaypointIndex = wallPath.GetNextIndex(currentWaypointIndex);
    }

    private bool AtWaypoint()
    {
        float distanceToWaypoint = Vector3.Distance(transform.position, GetCurrentWaypoint());
        return distanceToWaypoint < waypointTolerance;
    }
}
