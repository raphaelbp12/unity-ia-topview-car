using Assets.Classes;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using VehicleBehaviour;
using Newtonsoft.Json;

public class GameRulesController : MonoBehaviour
{
    // Reference to the Prefab. Drag a Prefab into this field in the Inspector.

    public bool showDrawDebug = false;
    private int trackCount;
    public int generations = 0;
    public GameObject myPrefab;
    public GameObject camera;
    public List<GameObject> spawnPoints;
    public int currentSpawnPoint = 0;
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

    public float highestTravelledDist = 0f;
    public List<float> highestTravelledDistByTrack;
    public float highestGoalDistance;
    public float highestMeanVelInTicks = 0f;
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

    private NeuralNetwork bestCurrentCar;
    private NeuralNetwork loadedNeuralNetwork;
    private List<NeuralNetwork> loadedNeuralNetworks = new List<NeuralNetwork>();
    private bool wasCarLoaded = false;
    public Button saveBestCarButton;
    public Button loadCarButton;
    private string pathToTheFile = "./cars/";
    public InputField carFileName;
    //private List<string> existingCars = new List<string>();
    private List<string> existingCars = new List<string>() { "massa", "dia1", "dia2", "dia3", "diferente", "diferente2", "azulao" };

    public int numberPersistentCars = 4;

    [SerializeField] List<WallMover> movingWalls;

    private List<WallPath> wallPaths = new List<WallPath>();

    // This script will simply instantiate the Prefab when the game starts.
    void Start()
    {
        trackCount = spawnPoints.Count;

        Button saveBtn = saveBestCarButton.GetComponent<Button>();
		saveBtn.onClick.AddListener(SaveBestCarOnClick);

        Button loadBtn = loadCarButton.GetComponent<Button>();
		loadBtn.onClick.AddListener(LoadCarOnClick);

        if (existingCars.Count > 0)
        {
            numberPersistentCars = existingCars.Count;

            if (numberPersistentCars > numberOfParents)
            {
                numberOfParents = numberPersistentCars;
            }
        }

        for (int i = 0; i < trackCount; i++)
        {
            highestTravelledDistByTrack.Add(0f);
        }
        Time.timeScale = gameSpeed;
        GenerateCars(new List<NeuralNetwork>());


        for (int i = 0; i < trackCount; i++)
        {
            wallPaths.Add(movingWalls[i+1].wallPath);
        }
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


            carsOrdered = newCars.ToList().OrderByDescending(o => o.distanceTravelled).ToList();
            SelectCarToCamera(carsOrdered);
        }
        else
        {
            PrepareNextTrack();
        }
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

    void SaveBestCarOnClick(){
		Debug.Log ("You have clicked the button!");
        SaveCar(bestCurrentCar);
	}

    void LoadCarOnClick(){
        string carName = "car";
        string path = pathToTheFile + "car.txt";

        if (carFileName.text != "")
        {
            carName = carFileName.text;
            path = pathToTheFile + carName + ".txt";
        }

        if (File.Exists(path)) {
            LoadCar(path, carName);
        }
	}

    public void SaveCar(NeuralNetwork car)
    {
        string path = pathToTheFile + "car.txt";

        if (carFileName.text != "")
        {
            path = pathToTheFile + carFileName.text + ".txt";
        }

        if (!File.Exists(path)) {
            File.Create(path);
        }

        var json = File.ReadAllText(path);
        var carsParsed = JsonConvert.DeserializeObject<List<List<List<float>>>>(json);
        List<List<List<float>>> neuralLayerWeights = new List<List<List<float>>>() {};

        foreach (Layer layer in car.neuralLayers)
        {
            neuralLayerWeights.Add(layer.GetWeights());
        }

        File.WriteAllText(path, JsonConvert.SerializeObject(neuralLayerWeights));
    }

    public void LoadCar(string path, string carName) {
        var json = File.ReadAllText(path);
        var neuralLayerWeights = JsonConvert.DeserializeObject<List<List<List<float>>>>(json);
        NeuralNetwork carComp = new NeuralNetwork();
        carComp.carName = carName;

        loadedNeuralNetwork = carComp;
        loadedNeuralNetwork.neuralLayers = carComp.Network(neuralLayerWeights);
        loadedNeuralNetwork.parentLayers = loadedNeuralNetwork.neuralLayers;
        loadedNeuralNetworks.Add(loadedNeuralNetwork.DeepCopy());
        wasCarLoaded = true;
    }

    void PrepareNextTrack()
    {
        foreach (WallMover wall in movingWalls)
        {
            wall.restartMovement();
        }

        highestTravelledDistByTrack[currentSpawnPoint] = highestTravelledDist;
        highestTravelledDist = 0;

        int nextCurrentSpawnPoint = currentSpawnPoint + 1;

        if (nextCurrentSpawnPoint == spawnPoints.Count) {
            generations += 1;
            currentSpawnPoint = 0;
            NaturalSelection();
        } else {
            currentSpawnPoint = nextCurrentSpawnPoint;
            GenerateCars(thisGenerationCars);
        }
    }

    public void ChangeGameSpeed()
    {
        gameSpeed = gameSpeedSlider.value;
        Time.timeScale = gameSpeed;
    }

    void GenerateCars(List<NeuralNetwork> newCars)
    {
        int realCarNumber = newCars.Count > 0 ? newCars.Count : numCars;
        int loadedCars = 0;

        thisGenerationCars = new List<NeuralNetwork>();
        carsOrdered = new List<NeuralNetwork>();

        if (newCars.Count == 0)
        {
            foreach (string fileName in existingCars)
            {
                string path = pathToTheFile + fileName + ".txt";
                if (File.Exists(path))
                {
                    LoadCar(path, fileName);
                }
            }
        }

        if (wasCarLoaded && loadedNeuralNetworks.Count > 0)
        {
            foreach(NeuralNetwork car in loadedNeuralNetworks)
            {
                NeuralNetwork carToAdd = car.DeepCopy();
                carToAdd.wasLoaded = true;
                newCars.Add(carToAdd);
                loadedNeuralNetwork = new NeuralNetwork();
                loadedCars += 1;
            }
            loadedNeuralNetworks = new List<NeuralNetwork>();
            wasCarLoaded = false;

            realCarNumber += loadedCars;
        }

        cars = new GameObject[realCarNumber];

        // Instantiate at position (0, 0, 0) and zero rotation.
        for (int i = 0; i < realCarNumber; i++)
        {
            GameObject car = Instantiate(myPrefab, spawnPoints[currentSpawnPoint].transform.position, Quaternion.identity);
            NeuralNetwork carComp = new NeuralNetwork();

            carComp = car.GetComponent<NeuralNetwork>();
            carComp.gameSpeed = gameSpeed;

            if ( i < newCars.Count) {
                carComp.currentTrack = currentSpawnPoint;
                carComp.parentLayers = newCars[i].neuralLayers;
                carComp.wasLoaded = newCars[i].wasLoaded;
                carComp.firstScore = newCars[i].firstScore;
                carComp.topScore = newCars[i].topScore;

                if (newCars[i].carName == "") {
                    carComp.carName = generations + "-" + i;
                } else {
                    carComp.carName = newCars[i].carName;
                    carComp.positionsByTrack = newCars[i].positionsByTrack;
                }
            }

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
            car.firstScore = false;
            car.topScore = false;
            
            float thisCarScore = car.CalculateTotalScore(wallPaths);

            if (thisCarScore > highestScore)
                highestScore = thisCarScore;
        }

        currentHighestScore = highestScore;
        scoreHistory.Add(currentHighestScore);

        carsOrdered = thisGenerationCars.ToList().OrderByDescending(o => o.score).ToList();

        bestCurrentCar = carsOrdered[0].DeepCopy();

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
            carsOrdered[0].firstScore = true;

            for (int i = 0; i < numberPersistentCars; i++)
            {
                NeuralNetwork carToAdd = carsOrdered[i].DeepCopy();
                carToAdd.topScore = true;
                newCars.Add(carToAdd);
            }
            newCars.Add(carsOrdered[numCars - 1].DeepCopy());
            newCars.Add(carsOrdered[numCars - 2].DeepCopy());
            newCars.AddRange(CrossOver(carListProbabilities, numCars - (2 + numberPersistentCars)));

            // NeuralNetwork emptyCar = carsOrdered[0].DeepCopy();
            // emptyCar.carName = "";
            // emptyCar.parentLayers = new List<Layer>();
            // emptyCar.neuralLayers = new List<Layer>();

            // for(int i = 0; i < numCars; i++)
            // {
            //     newCars.Add(emptyCar.DeepCopy());
            // }

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

            childCar.carName = "";
            newCars.Add(childCar);
        }

        return newCars;
    }
}
