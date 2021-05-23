using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class WallPath : MonoBehaviour
{
    const float waypointGizmoRadius = 0.3f;
    // Start is called before the first frame update
    private void OnDrawGizmos()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            int j = GetNextIndex(i);
            Gizmos.DrawSphere(GetWaypoint(i), waypointGizmoRadius);
            Gizmos.DrawLine(GetWaypoint(i), GetWaypoint(j));
        }
    }

    public int GetNextIndex(int i)
    {
        if (i + 1 >= transform.childCount)
        {
            return 0;
        }
        return i + 1;
    }

    public Vector3 GetWaypoint(int i)
    {
        return transform.GetChild(i).position;
    }

    public List<Vector3> GetPoints()
    {
        return transform.Cast<Transform>().Select(childTransform => childTransform.position).ToList();
    }
}
