using Assets.Classes;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using VehicleBehaviour;

public class GameRulesController : MonoBehaviour
{
    // Reference to the Prefab. Drag a Prefab into this field in the Inspector.

    public int generations = 0;
    public GameObject myPrefab;
    public GameObject camera;
    public GameObject spawnPoint;
    public Text text;
    public Text genText;
    public Text remainingCarText;
    public Slider gameSpeedSlider;
    public Text gameSpeedText;

    public int numCars = 10;
    private GameObject[] cars;
    private int activeCarIndex;

    public List<WheelVehicle> carsOrdered;
    public List<WheelVehicle> thisGenerationCars;

    public float highestTravelledDist;
    public float highestGoalDistance;
    public float highestMeanVelInTicks;
    public float highestTicksOnCrash;
    public float highestSteering = 0;
    public float highestThrottle = 0;
    public float currentHighestScore = 0;

    public float gameSpeed = 1.0f;
    public List<float> scoreHistory = new List<float>();

    public int numberOfParents = 20;

    public float mutationProbability = 0.1f;

    private List<List<WheelVehicle>> carsHistory = new List<List<WheelVehicle>>();

    public int ticks = 0;
    public int ticksIntervalCalcCamera = 20;

    // This script will simply instantiate the Prefab when the game starts.
    void Start()
    {
        Time.timeScale = gameSpeed;
        GenerateCars(new List<WheelVehicle>());
    }

    void Update()
    {
    }

    void FixedUpdate()
    {
        ticks += 1;
        WheelVehicle[] newCars = FindObjectsOfType<WheelVehicle>();
        if (activeCarIndex < newCars.Length)
        {
            text.text = "goalDistance " + newCars[activeCarIndex].goalDistance.ToString() + " " + "goalAngle " + newCars[activeCarIndex].goalAngle.ToString();
        }

        remainingCarText.text = "Remaining Cars: " + newCars.Length;
        genText.text = "Generations: " + generations.ToString();
        gameSpeedText.text = "Game Speed: " + gameSpeed;

        if (newCars.Length > 0)
        {
            if (ticks % ticksIntervalCalcCamera != 0)
                return;
            highestTravelledDist = newCars[0].distanceTravelled;
            highestGoalDistance = 0.0f;
            highestTicksOnCrash = 0.0f;
            highestMeanVelInTicks = 0.0f;

            foreach (WheelVehicle car in newCars)
            {
                if (car.distanceTravelled > highestTravelledDist)
                {
                    highestTravelledDist = car.distanceTravelled;
                }

                if (car.goalDistance > highestGoalDistance)
                {
                    highestGoalDistance = car.goalDistance;
                }

                if (car.meanVelInTicks > highestMeanVelInTicks)
                {
                    highestMeanVelInTicks = car.meanVelInTicks;
                }
            }


            foreach (WheelVehicle car in newCars)
            {
                float score = car.CalculateScore(highestTravelledDist, highestMeanVelInTicks);
            }

            carsOrdered = newCars.ToList().OrderByDescending(o => o.score).ToList();


            SelectCarToCamera(carsOrdered);
        } else
        {
            NaturalSelection();
        }
    }

    public void ChangeGameSpeed()
    {
        gameSpeed = gameSpeedSlider.value;
        Time.timeScale = gameSpeed;
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

    void GenerateCars(List<WheelVehicle> newCars)
    {
        generations += 1;
        scoreHistory.Add(currentHighestScore);
        thisGenerationCars = new List<WheelVehicle>();
        cars = new GameObject[numCars];
        carsOrdered = new List<WheelVehicle>();
        // Instantiate at position (0, 0, 0) and zero rotation.
        for (int i = 0; i < numCars; i++)
        {
            GameObject car = Instantiate(myPrefab, spawnPoint.transform.position, Quaternion.identity);
            WheelVehicle carComp = new WheelVehicle();

            carComp = car.GetComponent<WheelVehicle>();
            carComp.gameSpeed = gameSpeed;

            if (newCars.Count > 0)
            {
                carComp.parentLayers = newCars[i].neuralLayers;
            }

            carComp.carName = generations + "-" + i;

            thisGenerationCars.Add(carComp);

            Debug.Log("car " + car.ToString());
            cars[i] = car;
        }
        activeCarIndex = 0;
        text.text = "goalDistance unset";
    }

    void NaturalSelection()
    {
        float highestScore = 0.0f;

        List<WheelVehicle> carListProbabilities = new List<WheelVehicle>();

        foreach (WheelVehicle car in thisGenerationCars)
        {
            if (car.distanceTravelled > highestTravelledDist)
            {
                highestTravelledDist = car.distanceTravelled;
            }

            if (car.goalDistance > highestGoalDistance)
            {
                highestGoalDistance = car.goalDistance;
            }

            if (car.ticksOnCrash > highestTicksOnCrash)
            {
                highestTicksOnCrash = car.ticksOnCrash;
            }

            if (car.maxSteering > highestSteering)
            {
                highestSteering = car.maxSteering;
            }

            if (car.maxThrottle > highestThrottle)
            {
                highestThrottle = car.maxThrottle;
            }

            if (car.meanVelInTicks > highestMeanVelInTicks)
            {
                highestMeanVelInTicks = car.meanVelInTicks;
            }
        }

        foreach (WheelVehicle car in thisGenerationCars)
        {
            car.CalculateScore(highestTravelledDist, highestMeanVelInTicks);
            float thisCarScore = car.score;

            if (thisCarScore > highestScore)
                highestScore = thisCarScore;
        }

        currentHighestScore = highestScore;

        carsOrdered = thisGenerationCars.ToList().OrderByDescending(o => o.score).ToList();

        //foreach (WheelVehicle car in thisGenerationCars)
        //{
        //    float probability = UnityEngine.Mathf.Floor(car.score / highestScore * 100);

        //    for (int i = 0; i < probability; i++)
        //    {
        //        carListProbabilities.Add(car);
        //    }
        //}

        for(int i = 0; i < numberOfParents; i++)
        {
            carListProbabilities.Add(carsOrdered[i]);
        }


        List<WheelVehicle> newCars = new List<WheelVehicle>();
        newCars.Add(carsOrdered[0].DeepCopy());
        newCars.Add(carsOrdered[1].DeepCopy());
        newCars.AddRange(CrossOver(carListProbabilities, numCars - 2));

        //for(int i = 0; i < numCars; i++)
        //{
        //    newCars.Add(carsOrdered[0]);
        //}

        carsHistory.Add(carsOrdered);
        carsOrdered = new List<WheelVehicle>();

        GenerateCars(newCars);

        //for(int i = 0; i < cars.Length; i++)
        //{
        //    cars[i].
        //}
    }

    List<WheelVehicle> CrossOver(List<WheelVehicle> carListProbabilities, int numChildren)
    {
        List<WheelVehicle> newCars = new List<WheelVehicle>();

        for (int i = 0; i < numChildren; i++)
        {
            int carMotherIndex = UnityEngine.Mathf.FloorToInt(UnityEngine.Random.Range(0.0f, 1.0f) * carListProbabilities.Count);
            int carFatherIndex = UnityEngine.Mathf.FloorToInt(UnityEngine.Random.Range(0.0f, 1.0f) * carListProbabilities.Count);

            List<Neuron> motherGenome = carListProbabilities[carMotherIndex].GetGenome();
            List<Neuron> fatherGenome = carListProbabilities[carFatherIndex].GetGenome();


            int crossOverPoint = UnityEngine.Mathf.FloorToInt(UnityEngine.Random.Range(0.0f, 1.0f) * motherGenome.Count);

            List<Neuron> resultGenome = motherGenome.Take(crossOverPoint).Concat(fatherGenome.Skip(crossOverPoint)).ToList();
            List<Neuron> childrenGenome = new List<Neuron>();
            foreach(Neuron neuron in resultGenome)
            {
                childrenGenome.Add(neuron.DeepCopy());
            }

            int mutationPoint = UnityEngine.Mathf.FloorToInt(UnityEngine.Random.Range(0.0f, 1.0f) * childrenGenome.Count);

            if (UnityEngine.Random.Range(0.0f, 1.0f) < mutationProbability)
            {
                int numWeights = childrenGenome[mutationPoint].weights.Count();
                childrenGenome[mutationPoint].generateWeights(numWeights, new List<float>());
            }

            WheelVehicle childCar = new WheelVehicle();

            int genesToSkip = 0;
            List<Neuron> previousLayerNeurons = new List<Neuron>();

            WheelVehicle baseCar = carListProbabilities[carMotherIndex];

            foreach(Layer layer in baseCar.neuralLayers)
            {
                childCar.neuralLayers.Add(layer.DeepCopy());
            }

            for (int j = 0; j < baseCar.neuralLayers.Count; j++)
            {
                int genesToTake = baseCar.neuralLayers[j].neurons.Count;
                childCar.neuralLayers[j].neurons = childrenGenome.Skip(genesToSkip).Take(genesToTake).ToList();
                genesToSkip += genesToTake;

                if (previousLayerNeurons.Count > 0)
                {
                    for(int k = 0; k < baseCar.neuralLayers[j].neurons.Count; k++)
                    {
                        childCar.neuralLayers[j].neurons[k].neuronsPreviousLayer = previousLayerNeurons;
                    }
                }

                previousLayerNeurons = childCar.neuralLayers[j].neurons;
            }

            newCars.Add(childCar);
        }

        return newCars;
    }
}
