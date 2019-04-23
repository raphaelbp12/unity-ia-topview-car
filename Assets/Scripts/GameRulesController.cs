using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameRulesController : MonoBehaviour
{
    // Reference to the Prefab. Drag a Prefab into this field in the Inspector.
    public GameObject myPrefab;
    public GameObject camera;

    public int numCars;
    private GameObject[] cars;
    private int activeCarIndex;

    // This script will simply instantiate the Prefab when the game starts.
    void Start()
    {
        cars = new GameObject[numCars];
        // Instantiate at position (0, 0, 0) and zero rotation.
        for(int i = 0; i < numCars; i++)
        {
            GameObject car = Instantiate(myPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            Debug.Log("car " + car.ToString());
            cars[i] = car;
        }
        activeCarIndex = 0;
    }

    void FixedUpdate()
    {
        if (cars.Length > 0)
        {
            if (!cars[activeCarIndex].activeInHierarchy)
            {
                for (int i = 0; i < numCars; i++)
                {
                    if(cars[i].activeInHierarchy)
                    {
                        activeCarIndex = i;
                    }
                }

            }
            camera.SendMessage("SetCar", cars[activeCarIndex]);
        }
    }
}
