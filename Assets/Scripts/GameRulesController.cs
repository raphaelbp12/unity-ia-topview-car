using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameRulesController : MonoBehaviour
{
    // Reference to the Prefab. Drag a Prefab into this field in the Inspector.
    public GameObject myPrefab;

    public int numCars;

    // This script will simply instantiate the Prefab when the game starts.
    void Start()
    {
        // Instantiate at position (0, 0, 0) and zero rotation.
        for(int i = 0; i < numCars; i++)
        {
            Instantiate(myPrefab, new Vector3(0, 0, 0), Quaternion.identity);
        }
    }
}
