using Assets.Classes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VehicleBehaviour;

public class NeuralNetwork : MonoBehaviour
{
    public GameObject modelPrefab;
    public GameObject car;
    public GameObject carGO;
    public string carName = "";

    Rigidbody _rb;


    public float linearVel;
    public float angVel;

    public bool showDebugDraw = true;
    public float gameSpeed = 1;

    //private GameObject goal;

    public List<Layer> neuralLayers = new List<Layer>();
    public List<Layer> parentLayers = new List<Layer>();

    public float randomThrottle;
    public float randomSteering;

    public float maxSteering = 0;
    public float maxThrottle = 0;


    private bool gameover = false;
    private Vector3 lastPosition;
    private float lastDistanceIncrement;
    private Vector3 lastDistancePosition;
    public Vector3 lastDistanceInLastTime;
    private float distanceInLastTime = 0.0f;


    private float minValidDistanceIncrement = 0.005f;
    private float minMeanVel = 0.04f;
    private float minInstantVel = 0.04f;
    private int maxLifeTimeSec = 2500;
    private int minLifeTimeSec = 500;
    private float minDistAllowed = 10;
    private float minDistInLastTimeAllowed = 5;

    public float meanVelInTicks = 0;
    public int ticks = 0;
    public int ticksWithMinDistanceValid = 0;
    public float realTime = 0;
    public float distanceTravelled = 0.0f;
    public float meanVel = 50;
    public bool hasCrashedOnWall = false;
    public int ticksOnCrash = 3000;
    public float score = 0;

    // Start is called before the first frame update
    void Start()
    {
        //goal = GameObject.FindGameObjectWithTag("Goal");

        GameObject car = Instantiate(modelPrefab, transform.position, Quaternion.identity);
        car.GetComponent<UnicycleController>().parentNeuralNetwork = this;
        carGO = car.gameObject;

        _rb = carGO.GetComponent<Rigidbody>();

        randomSteering = 0.0f;
        randomThrottle = 0.0f;

        lastPosition = carGO.transform.position;
        lastDistancePosition = carGO.transform.position;
        //goalDistance = 0.0f;
        //goalAngle = 0.0f;
    }

    // Update is called once per frame
    void Update()
    {
    }

    void FixedUpdate()
    {
        ticks = ticks + 1;
        lastDistanceIncrement = Vector3.Distance(carGO.transform.position, lastPosition);
        if (lastDistanceIncrement > minValidDistanceIncrement)
        {
            distanceTravelled += lastDistanceIncrement;
            ticksWithMinDistanceValid = ticksWithMinDistanceValid + 1;
        }
        lastPosition = carGO.transform.position;
        meanVelInTicks = distanceTravelled / (ticks);

        CalcGameover();

        if (gameSpeed > 1 && ticks % (2 * gameSpeed) != 0)
            return;

        // GetCarOutputsToNeural();

        Network();
    }

    List<float> GetCarOutputsToNeural()
    {
        List<float> result = new List<float>();

        int maxLaserDistance = 50;

        // if (goal != null)
        // {
        //     goalDistance = Vector3.Distance(this.gameObject.transform.position, goal.transform.position);
        //     goalAngle = GetGoalAngle();
        //     if(showDebugDraw)
        //         Debug.DrawRay(transform.position, transform.TransformDirection(new Vector3((float)Math.Cos((goalAngle * -1) + (float)Math.PI / 2), 0, (float)Math.Sin((goalAngle * -1) + (float)Math.PI / 2))) * 10, Color.blue);

        //     //Debug.Log("goalAngle " + goalAngle);
        // } else
        // {
        //     goalDistance = 0.0f;
        //     goalAngle = 0.0f;

        //     //Debug.Log("goalAngle and distances dont exist ");
        // }

        Vector3 velProjected = Vector3.ProjectOnPlane(_rb.velocity, new Vector3(0, 1, 1));

        linearVel = Vector3.Dot(velProjected, carGO.transform.forward);
        angVel = _rb.angularVelocity.y;

        //Debug.Log("velocidade " + linearVel + " angular " + angVel);

        float maxLinearVelPoss = 34.96f;
        float maxAngVelPoss = 6.0f;

        result.Add(linearVel / maxLinearVelPoss);
        result.Add(angVel / maxAngVelPoss);

        //result.Add(goalDistance);
        //result.Add(goalAngle);

        result.Add(GetLaserDistToWall(maxLaserDistance, new Vector3(0, 0, 1)));
        result.Add(GetLaserDistToWall(maxLaserDistance, new Vector3(0, 0, -1)));
        result.Add(GetLaserDistToWall(maxLaserDistance, new Vector3(0.5f, 0, 0.866f))); // 30 degrees
        result.Add(GetLaserDistToWall(maxLaserDistance, new Vector3(-0.5f, 0, 0.866f))); // 30 degrees

        result.Add(GetLaserDistToWall(maxLaserDistance, new Vector3(0.9659f, 0, 0.2588f))); // 15 degrees
        result.Add(GetLaserDistToWall(maxLaserDistance, new Vector3(-0.9659f, 0, 0.2588f))); // 15 degrees

        //result.Add(GetLaserDistToWall(maxLaserDistance, new Vector3(-1, 0, 0)) / maxLaserDistance);
        //result.Add(GetLaserDistToWall(maxLaserDistance, new Vector3(1, 0, 0)) / maxLaserDistance);

        //result.Add(GetLaserDistToWall(maxLaserDistance, new Vector3(1, 0, 1).normalized) / maxLaserDistance);
        //result.Add(GetLaserDistToWall(maxLaserDistance, new Vector3(1, 0, -1).normalized) / maxLaserDistance);
        //result.Add(GetLaserDistToWall(maxLaserDistance, new Vector3(-1, 0, 1).normalized) / maxLaserDistance);
        //result.Add(GetLaserDistToWall(maxLaserDistance, new Vector3(-1, 0, -1).normalized) / maxLaserDistance);

        return result;
    }

    float GetLaserDistToWall(float maxDist, Vector3 direction)
    {
        // Bit shift the index of the layer (8) to get a bit mask
        int layerMask = ~ ((1 << 8) | (1 << 9));

        // This would cast rays only against colliders in layer 8.
        // But instead we want to collide against everything except layer 8. The ~ operator does this, it inverts a bitmask.

        RaycastHit hit;
        // Does the ray intersect any objects excluding the player layer

        Vector3 transformDir = Vector3.ProjectOnPlane(carGO.transform.TransformDirection(direction), new Vector3(0, 1, 0));

        if (Physics.Raycast(carGO.transform.position, transformDir, out hit, maxDist, layerMask))
        {
            //Debug.Log("Did Hit  " + hit.collider.gameObject.tag);
            if (hit.collider.CompareTag("Wall"))
            {
                if (showDebugDraw)
                    Debug.DrawRay(carGO.transform.position, transformDir * hit.distance, Color.green);
                return hit.distance;
            } else
            {
                if (showDebugDraw)
                    Debug.DrawRay(carGO.transform.position, transformDir * hit.distance, Color.yellow);
                return maxDist;
            }
        }
        else
        {
            if (showDebugDraw)
                Debug.DrawRay(carGO.transform.position, transformDir * maxDist, Color.red);
            //Debug.Log("Did not Hit");
            return maxDist;
        }
    }

    private void Network()
    {
        List<Layer> newLayers = new List<Layer>();

        if (parentLayers.Count > 0 && neuralLayers.Count == 0)
        {
            neuralLayers = parentLayers;

            foreach (Layer layer in neuralLayers)
            {
                layer.ResetLayer();
            }
        }

        List<float> neuralInputs = GetCarOutputsToNeural();

        Layer firstLayer = new Layer(neuralInputs, neuralInputs.Count, new List<Neuron>(), new List<List<float>>());
        newLayers.Add(firstLayer);

        List<List<float>> pastSecondLayerWeights = neuralLayers.Count > 1 ? neuralLayers[1].GetWeights() : new List<List<float>>();
        Layer secondLayer = new Layer(new List<float>(), 4, firstLayer.neurons, pastSecondLayerWeights);
        newLayers.Add(secondLayer);

        List<List<float>> pastThirdLayerWeights = neuralLayers.Count > 2 ? neuralLayers[2].GetWeights() : new List<List<float>>();
        Layer thirdLayer = new Layer(new List<float>(), 4, secondLayer.neurons, pastThirdLayerWeights);
        newLayers.Add(thirdLayer);

        List<List<float>> pastFourthLayerWeights = neuralLayers.Count > 3 ? neuralLayers[3].GetWeights() : new List<List<float>>();
        Layer fourthLayer = new Layer(new List<float>(), 2, thirdLayer.neurons, pastFourthLayerWeights);
        newLayers.Add(fourthLayer);

        List<float> vels = fourthLayer.GetOutputs();

        if (vels.Count > 0)
        {
            randomThrottle = vels[0] * 2;
            randomSteering = vels[1];

            //randomThrottle = 10;
            //randomSteering = 0;

            if (randomSteering > 1)
                randomSteering = 1;

            if (Math.Abs(randomSteering) > Math.Abs(maxSteering))
                maxSteering = randomSteering;

            if (Math.Abs(randomThrottle) > Math.Abs(maxThrottle))
                maxThrottle = randomThrottle;

            randomSteering = randomSteering * 1;

            carGO.GetComponent<UnicycleController>().throttle = randomThrottle;
            carGO.GetComponent<UnicycleController>().steering = randomSteering;

            //Debug.Log("randomThrottle " + randomThrottle + " randomSteering " + randomSteering);
        }

        neuralLayers = newLayers;
    }

    bool CalcGameover()
    {
        if (gameover)
        {
            return gameover;
        }

        realTime = ticks;

        if (ticks % 60 == 0)
        {
            distanceInLastTime = Vector3.Distance(carGO.transform.position, lastDistancePosition);
            lastDistancePosition = carGO.transform.position;
        }

        if (realTime > minLifeTimeSec)
        {

            if (distanceInLastTime < minDistInLastTimeAllowed)
            {
                setGameover(false, "distancia percorrida mto pequena nos ultimos ticks");
            }

            //if (distanceTravelled < minDistAllowed)
            //{
            //    setGameover(false, "distancia percorrida mto pequena");
            //}

            meanVel = distanceTravelled / realTime;

            if (meanVel < minMeanVel)
            {
                setGameover(false, "velocidade media baixa");
            }

            if (linearVel < minInstantVel)
            {
                setGameover(false, "velocidade instantanea baixa");
            }
        }

        if (realTime > maxLifeTimeSec)
            setGameover(false, "maxLifeTimeSec");


        return gameover;
    }

    void setGameover(bool crashedOnWall, string reason)
    {
        Debug.Log("setGameoverCalled " + reason);
        if (!gameover)
        {
            if (crashedOnWall)
            {
                hasCrashedOnWall = crashedOnWall;
                ticksOnCrash = ticks;
            }

            gameover = true;
            gameObject.SetActive(false);
            carGO.SetActive(false);
            UnityEngine.Object.Destroy(carGO);
            UnityEngine.Object.Destroy(gameObject);
        }
    }

    public float CalculateScore(float highestTravelledDist, float highestMeanVelInTicks)
    {
        float normalizedTravelledDist = (highestTravelledDist - distanceTravelled + 0.000001f) / highestTravelledDist;

        if (highestTravelledDist == distanceTravelled)
        {
            normalizedTravelledDist = 1.0f;
        }

        // float normalizedMeanVel = (highestMeanVelInTicks - meanVelInTicks + 0.000001f) / highestMeanVelInTicks;

        // if (highestMeanVelInTicks == meanVelInTicks)
        // {
        //     normalizedMeanVel = 1.0f;
        // }

        //relativeGoalDistance = (highestGoalDistance - goalDistance + 0.001f) / highestGoalDistance;

        float currentScore = normalizedTravelledDist;

        if (ticksWithMinDistanceValid == 0)
        {
            currentScore = 0;
        }

        //if (hasCrashedOnWall)
        //{
        //    currentScore = 0;
        //}

        //if (checkpointsReached > 0)
        //{
        //    currentScore = currentScore * checkpointsReached * 20;
        //}

        score = currentScore;
        return currentScore;
    }

    public List<Neuron> GetGenome()
    {
        List<Neuron> genome = new List<Neuron>();

        foreach (Layer layer in neuralLayers)
        {
            genome.AddRange(layer.neurons.ToArray());
        }

        return genome;
    }

    public NeuralNetwork DeepCopy()
    {
        NeuralNetwork other = (NeuralNetwork)this.MemberwiseClone();
        other.neuralLayers = new List<Layer>();

        foreach (Layer layer in neuralLayers)
        {
            other.neuralLayers.Add(layer.DeepCopy());
        }

        return other;
    }

    public void OnChildTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Wall"))
        {
            //Debug.Log("collide wall");
            setGameover(true, "bateu na parede");
            //other.gameObject.SetActive(false);
            //Debug.Log("position: " + this.gameObject.transform.position.ToString());
        }
        //else if (other.gameObject.CompareTag("Goal"))
        //{
        //    //Debug.Log("collide goal");
        //    //other.gameObject.SetActive(false);
        //    this.goalReached = true;
        //    this.gameObject.SetActive(false);
        //    //Debug.Log("position: " + this.gameObject.transform.position.ToString());
        //}
        //else if (other.gameObject.CompareTag("Checkpoint"))
        //{
        //    checkpointsReached += 1;
        //    //Debug.Log("collide checkpoint");
        //}
        //Destroy(other.gameObject);
    }

    //float GetGoalAngle()
    //{
    //    float goalAng = Vector3.Angle(this.gameObject.transform.position, goal.transform.position);
    //    Vector3 goalDir = goal.transform.position - this.gameObject.transform.position;

    //    goalAng = (float)Math.Acos(Vector3.Dot(goalDir.normalized, transform.forward));
    //    float whichWay = Vector3.Cross(transform.forward, goalDir.normalized).y;
    //    goalAng = (goalAng + (float)Math.PI / 2) * (Math.Sign(whichWay) * -1);

    //    if (goalAng < 0)
    //    {
    //        goalAng = goalAng + (float)Math.PI;
    //    }

    //    goalAng = (goalAng - (float)Math.PI / 2) * -1;

    //    return goalAng;
    //}
}
