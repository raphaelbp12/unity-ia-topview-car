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

    public List<NeuralNetwork> carsOrdered;
    public List<NeuralNetwork> thisGenerationCars;

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
    public int numberOfMutations = 2;
    public int numNeuralInputs = 8;

    private List<List<NeuralNetwork>> carsHistory = new List<List<NeuralNetwork>>();

    public int ticks = 0;
    public int ticksIntervalCalcCamera = 20;

    [SerializeField] List<WallMover> movingWalls;

    // This script will simply instantiate the Prefab when the game starts.
    void Start()
    {
        Time.timeScale = gameSpeed;
        GenerateCars(new List<NeuralNetwork>());
    }

    void Update()
    {
    }

    void FixedUpdate()
    {
        ticks += 1;
        NeuralNetwork[] newCars = FindObjectsOfType<NeuralNetwork>();
        //if (activeCarIndex < newCars.Length)
        //{
        //    text.text = "goalDistance " + newCars[activeCarIndex].goalDistance.ToString() + " " + "goalAngle " + newCars[activeCarIndex].goalAngle.ToString();
        //}

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

            foreach (NeuralNetwork car in newCars)
            {
                if (car.distanceTravelled > highestTravelledDist)
                {
                    highestTravelledDist = car.distanceTravelled;
                }

                //if (car.goalDistance > highestGoalDistance)
                //{
                //    highestGoalDistance = car.goalDistance;
                //}

                if (car.meanVelInTicks > highestMeanVelInTicks)
                {
                    highestMeanVelInTicks = car.meanVelInTicks;
                }
            }


            foreach (NeuralNetwork car in newCars)
            {
                float score = car.CalculateScore(highestTravelledDist, highestMeanVelInTicks);
            }

            carsOrdered = newCars.ToList().OrderByDescending(o => o.score).ToList();


            SelectCarToCamera(carsOrdered);
        } else
        {
            foreach (WallMover wall in movingWalls)
            {
                wall.restartMovement();
            }
            NaturalSelection();
        }
    }

    public void ChangeGameSpeed()
    {
        gameSpeed = gameSpeedSlider.value;
        Time.timeScale = gameSpeed;
    }

    void SelectCarToCamera(List<NeuralNetwork> newCars)
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
            camera.SendMessage("SetCar", newCars[activeCarIndex].carGO.gameObject);
        }
    }

    void GenerateCars(List<NeuralNetwork> newCars)
    {
        int realCarNumber = newCars.Count > 0 ? newCars.Count : numCars*2;
        generations += 1;
        scoreHistory.Add(currentHighestScore);
        thisGenerationCars = new List<NeuralNetwork>();
        cars = new GameObject[realCarNumber];
        carsOrdered = new List<NeuralNetwork>();

        // Instantiate at position (0, 0, 0) and zero rotation.
        for (int i = 0; i < realCarNumber; i++)
        {
            GameObject car = Instantiate(myPrefab, spawnPoint.transform.position, Quaternion.identity);
            NeuralNetwork carComp = new NeuralNetwork();

            carComp = car.GetComponent<NeuralNetwork>();
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

        List<NeuralNetwork> carListProbabilities = new List<NeuralNetwork>();

        foreach (NeuralNetwork car in thisGenerationCars)
        {
            if (car.distanceTravelled > highestTravelledDist)
            {
                highestTravelledDist = car.distanceTravelled;
            }

            //if (car.goalDistance > highestGoalDistance)
            //{
            //    highestGoalDistance = car.goalDistance;
            //}

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

        foreach (NeuralNetwork car in thisGenerationCars)
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

        for(int i = (numCars - 1); i > (numCars - numberOfParents - 1); i--)
        {
            carListProbabilities.Add(carsOrdered[i]);
        }


        List<NeuralNetwork> newCars = new List<NeuralNetwork>();

        if (carsOrdered[0].score != 0 && carsOrdered[1].score != 0)
        {
            newCars.Add(carsOrdered[0].DeepCopy());
            newCars.Add(carsOrdered[1].DeepCopy());
            newCars.Add(carsOrdered[numCars - 1].DeepCopy());
            newCars.Add(carsOrdered[numCars - 2].DeepCopy());
            newCars.AddRange(CrossOver(carListProbabilities, numCars - 4));

            NeuralNetwork emptyCar = carsOrdered[0].DeepCopy();
            emptyCar.parentLayers = new List<Layer>();
            emptyCar.neuralLayers = new List<Layer>();

            for(int i = 0; i < numCars; i++)
            {
                newCars.Add(emptyCar.DeepCopy());
            }

            carsHistory.Add(carsOrdered);
            carsOrdered = new List<NeuralNetwork>();
        }

        //for(int i = 0; i < numCars; i++)
        //{
        //    newCars.Add(carsOrdered[0]);
        //}

        GenerateCars(newCars);

        //for(int i = 0; i < cars.Length; i++)
        //{
        //    cars[i].
        //}
    }

    List<NeuralNetwork> CrossOver(List<NeuralNetwork> carListProbabilities, int numChildren)
    {
        List<NeuralNetwork> newCars = new List<NeuralNetwork>();

        for (int i = 0; i < numChildren; i++)
        {
            int carMotherIndex = UnityEngine.Mathf.FloorToInt(UnityEngine.Random.Range(0.0f, 1.0f) * carListProbabilities.Count);
            int carFatherIndex = UnityEngine.Mathf.FloorToInt(UnityEngine.Random.Range(0.0f, 1.0f) * carListProbabilities.Count);

            List<Neuron> motherGenome = carListProbabilities[carMotherIndex].GetGenome();
            List<Neuron> fatherGenome = carListProbabilities[carFatherIndex].GetGenome();


            int crossOverPoint = UnityEngine.Mathf.FloorToInt(UnityEngine.Random.Range(0.0f, 1.0f) * (motherGenome.Count - numNeuralInputs)) + numNeuralInputs;

            List<Neuron> resultGenome = motherGenome.Take(crossOverPoint).Concat(fatherGenome.Skip(crossOverPoint)).ToList();
            List<Neuron> childrenGenome = new List<Neuron>();
            foreach(Neuron neuron in resultGenome)
            {
                childrenGenome.Add(neuron.DeepCopy());
            }

            List<int> mutationPoints = new List<int>();

            for (int j = 0; j < numberOfMutations; j++)
            {
                mutationPoints.Add(UnityEngine.Mathf.FloorToInt(UnityEngine.Random.Range(0.0f, 1.0f) * (childrenGenome.Count - numNeuralInputs)) + numNeuralInputs);
            }

            foreach (int mutationPoint in mutationPoints)
            {
                if (UnityEngine.Random.Range(0.0f, 1.0f) < mutationProbability)
                {
                    int numWeights = childrenGenome[mutationPoint].weights.Count();
                    childrenGenome[mutationPoint].generateWeights(numWeights, new List<float>());
                }
            }

            NeuralNetwork childCar = new NeuralNetwork();

            int genesToSkip = 0;
            List<Neuron> previousLayerNeurons = new List<Neuron>();

            NeuralNetwork baseCar = carListProbabilities[carMotherIndex];

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
