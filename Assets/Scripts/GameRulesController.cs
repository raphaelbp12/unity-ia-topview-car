using System.Collections;
using System.Collections.Generic;
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
        text.text = "goalDistance unset";
    }

    void FixedUpdate()
    {
        WheelVehicle[] newCars = FindObjectsOfType<WheelVehicle>();
        text.text = "goalDistance " + newCars[activeCarIndex].goalDistance.ToString();
        SelectCarToCamera(newCars);
    }

    void SelectCarToCamera(WheelVehicle[] newCars)
    {
        if (newCars.Length > 0)
        {
            if (!newCars[activeCarIndex].gameObject.activeInHierarchy)
            {
                Debug.Log("newCars length " + newCars.Length);
                for (int i = 0; i < newCars.Length; i++)
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
