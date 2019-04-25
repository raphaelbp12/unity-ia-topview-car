using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using VehicleBehaviour;

public class GameRulesController : MonoBehaviour
{
    // Reference to the Prefab. Drag a Prefab into this field in the Inspector.
    public GameObject myPrefab;
    public GameObject camera;
    public Text text;

    public int numCars;
    private GameObject[] cars;
    private int activeCarIndex;

    public List<WheelVehicle> carsOrdered;

    // This script will simply instantiate the Prefab when the game starts.
    void Start()
    {
        cars = new GameObject[numCars];
        carsOrdered = new List<WheelVehicle>();
        // Instantiate at position (0, 0, 0) and zero rotation.
        for (int i = 0; i < numCars; i++)
        {
            GameObject car = Instantiate(myPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            Debug.Log("car " + car.ToString());
            cars[i] = car;
        }
        activeCarIndex = 0;
        text.text = "goalDistance unset";
    }

    void FixedUpdate()
    {
    }

    void Update()
    {
        WheelVehicle[] newCars = FindObjectsOfType<WheelVehicle>();
        if (activeCarIndex < newCars.Length)
        {
            text.text = "goalDistance " + newCars[activeCarIndex].goalDistance.ToString() + " " + "goalAngle " + newCars[activeCarIndex].goalAngle.ToString();
        }

        if (newCars.Length > 0)
        {
            float lowestTravelledDist = newCars[0].distanceTravelled;
            float highestGoalDistance = 0.0f;

            foreach (WheelVehicle car in newCars)
            {
                if (car.distanceTravelled < lowestTravelledDist)
                {
                    lowestTravelledDist = car.distanceTravelled;
                }

                if (car.goalDistance > highestGoalDistance)
                {
                    highestGoalDistance = car.goalDistance;
                }
            }

            foreach (WheelVehicle car in newCars)
            {
                float score = car.CalculateScore(lowestTravelledDist, highestGoalDistance);
            }

            carsOrdered = newCars.ToList().OrderByDescending(o => o.score).ToList();


            SelectCarToCamera(carsOrdered);
        }
    }

    void SelectCarToCamera(List<WheelVehicle> newCars)
    {
        if (newCars.Count > 0)
        {
            if (!newCars[0].gameObject.activeInHierarchy)
            {
                Debug.Log("newCars length " + newCars.Count);
                for (int i = 0; i < newCars.Count; i++)
                {
                    var car = newCars[i];
                    if (car.gameObject.activeInHierarchy)
                    {
                        activeCarIndex = i;
                    }
                }

            }
            camera.SendMessage("SetCar", newCars[activeCarIndex].gameObject);
        }
    }
}
