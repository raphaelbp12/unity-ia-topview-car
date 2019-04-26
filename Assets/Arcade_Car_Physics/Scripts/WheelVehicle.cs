/*
 * This code is part of Arcade Car Physics for Unity by Saarg (2018)
 * 
 * This is distributed under the MIT Licence (see LICENSE.md for details)
 */
using Assets.Classes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if MULTIOSCONTROLS
    using MOSC;
#endif

namespace VehicleBehaviour {
    [RequireComponent(typeof(Rigidbody))]
    public class WheelVehicle : MonoBehaviour {
        
        [Header("Inputs")]
    #if MULTIOSCONTROLS
        [SerializeField] PlayerNumber playerId;
    #endif
        // If isPlayer is false inputs are ignored
        [SerializeField] bool isPlayer = true;
        public bool IsPlayer { get{ return isPlayer; } set{ isPlayer = value; } } 

        // Input names to read using GetAxis
        [SerializeField] string throttleInput = "Throttle";
        [SerializeField] string brakeInput = "Brake";
        [SerializeField] string turnInput = "Horizontal";
        [SerializeField] string jumpInput = "Jump";
        [SerializeField] string driftInput = "Drift";
	    [SerializeField] string boostInput = "Boost";
        
        /* 
         *  Turn input curve: x real input, y value used
         *  My advice (-1, -1) tangent x, (0, 0) tangent 0 and (1, 1) tangent x
         */
        [SerializeField] AnimationCurve turnInputCurve = AnimationCurve.Linear(-1.0f, -1.0f, 1.0f, 1.0f);

        [Header("Wheels")]
        [SerializeField] WheelCollider[] driveWheel;
        public WheelCollider[] DriveWheel { get { return driveWheel; } }
        [SerializeField] WheelCollider[] turnWheel;

        public WheelCollider[] TurnWheel { get { return turnWheel; } }

        // This code checks if the car is grounded only when needed and the data is old enough
        bool isGrounded = false;
        int lastGroundCheck = 0;
        public bool IsGrounded { get {
            if (lastGroundCheck == Time.frameCount)
                return isGrounded;

            lastGroundCheck = Time.frameCount;
            isGrounded = true;
            foreach (WheelCollider wheel in wheels)
            {
                if (!wheel.gameObject.activeSelf || !wheel.isGrounded)
                    isGrounded = false;
            }
            return isGrounded;
        }}

        [Header("Behaviour")]
        /*
         *  Motor torque represent the torque sent to the wheels by the motor with x: speed in km/h and y: torque
         *  The curve should start at x=0 and y>0 and should end with x>topspeed and y<0
         *  The higher the torque the faster it accelerate
         *  the longer the curve the faster it gets
         */
        [SerializeField] AnimationCurve motorTorque = new AnimationCurve(new Keyframe(0, 200), new Keyframe(50, 300), new Keyframe(200, 0));

        // Differential gearing ratio
        [Range(2, 16)]
        [SerializeField] float diffGearing = 4.0f;
        public float DiffGearing { get { return diffGearing; } set { diffGearing = value; } }

        // Basicaly how hard it brakes
        [SerializeField] float brakeForce = 1500.0f;
        public float BrakeForce { get { return brakeForce; } set { brakeForce = value; } }

        // Max steering hangle, usualy higher for drift car
        [Range(0f, 50.0f)]
        [SerializeField] float steerAngle = 30.0f;
        public float SteerAngle { get { return steerAngle; } set { steerAngle = Mathf.Clamp(value, 0.0f, 50.0f); } }

        // The value used in the steering Lerp, 1 is instant (Strong power steering), and 0 is not turning at all
        [Range(0.001f, 1.0f)]
        [SerializeField] float steerSpeed = 0.2f;
        public float SteerSpeed { get { return steerSpeed; } set { steerSpeed = Mathf.Clamp(value, 0.001f, 1.0f); } }

        // How hight do you want to jump?
        [Range(1f, 1.5f)]
        [SerializeField] float jumpVel = 1.3f;
        public float JumpVel { get { return jumpVel; } set { jumpVel = Mathf.Clamp(value, 1.0f, 1.5f); } }

        // How hard do you want to drift?
        [Range(0.0f, 2f)]
        [SerializeField] float driftIntensity = 1f;
        public float DriftIntensity { get { return driftIntensity; } set { driftIntensity = Mathf.Clamp(value, 0.0f, 2.0f); }}

        // Reset Values
        Vector3 spawnPosition;
        Quaternion spawnRotation;

        /*
         *  The center of mass is set at the start and changes the car behavior A LOT
         *  I recomment having it between the center of the wheels and the bottom of the car's body
         *  Move it a bit to the from or bottom according to where the engine is
         */
        [SerializeField] Transform centerOfMass;

        // Force aplied downwards on the car, proportional to the car speed
        [Range(0.5f, 10f)]
        [SerializeField] float downforce = 1.0f;
        public float Downforce { get{ return downforce; } set{ downforce = Mathf.Clamp(value, 0, 5); } }     

        // When IsPlayer is false you can use this to control the steering
        float steering;
        public float Steering { get{ return steering; } set{ steering = Mathf.Clamp(value, -1f, 1f); } } 

        // When IsPlayer is false you can use this to control the throttle
        float throttle;
        public float Throttle { get{ return throttle; } set{ throttle = Mathf.Clamp(value, -1f, 1f); } } 

        // Like your own car handbrake, if it's true the car will not move
        [SerializeField] bool handbrake;
        public bool Handbrake { get{ return handbrake; } set{ handbrake = value; } } 
        
        // Use this to disable drifting
        [HideInInspector] public bool allowDrift = true;
        bool drift;
        public bool Drift { get{ return drift; } set{ drift = value; } }         

        // Use this to read the current car speed (you'll need this to make a speedometer)
        [SerializeField] float speed = 0.0f;
        public float Speed { get{ return speed; } }

        [Header("Particles")]
        // Exhaust fumes
        [SerializeField] ParticleSystem[] gasParticles;

        [Header("Boost")]
        // Disable boost
        [HideInInspector] public bool allowBoost = true;

        // Maximum boost available
        [SerializeField] float maxBoost = 10f;
        public float MaxBoost { get { return maxBoost; } set {maxBoost = value;} }

        // Current boost available
        [SerializeField] float boost = 10f;
        public float Boost { get { return boost; } set { boost = Mathf.Clamp(value, 0f, maxBoost); } }

        // Regen boostRegen per second until it's back to maxBoost
        [Range(0f, 1f)]
        [SerializeField] float boostRegen = 0.2f;
        public float BoostRegen { get { return boostRegen; } set { boostRegen = Mathf.Clamp01(value); } }

        /*
         *  The force applied to the car when boosting
         *  NOTE: the boost does not care if the car is grounded or not
         */
        [SerializeField] float boostForce = 5000;
        public float BoostForce { get { return boostForce; } set { boostForce = value; } }
        
        // Use this to boost when IsPlayer is set to false
        public bool boosting = false;
        // Use this to jump when IsPlayer is set to false
        public bool jumping = false;

        // Boost particles and sound
        [SerializeField] ParticleSystem[] boostParticles;
        [SerializeField] AudioClip boostClip;
        [SerializeField] AudioSource boostSource;
        
        // Private variables set at the start
        Rigidbody _rb;
        WheelCollider[] wheels;

        public bool showDebugDraw = true;

        public float randomThrottle;
        public float randomSteering;
        private GameObject goal;
        private bool goalReached = false;
        private Vector3 lastPosition;
        private bool gameover = false;

        public int ticks = 0;
        public int ticksOnCrash = 3000;
        public int checkpointsReached = 0;
        public bool hasCrashedOnWall = false;
        public float distanceTravelled = 0.0f;
        public int maxLifeTimeSec = 25;
        public int minLifeTimeSec = 5;
        public float meanVel = 50;
        public float minMeanVel = 0.04f;
        public float minInstantVel = 0.04f;
        public float goalDistance;
        public float goalAngle;
        public float minDistAllowed = 10;
        public float score = 0;
        public float relativeGoalDistance = 0;


        public float linearVel;
        public float angVel;

        public float maxSteering = 0;
        public float maxThrottle = 0;

        public float realTime = 0;

        public string carName = "";

        public List<Layer> neuralLayers = new List<Layer>();
        public List<Layer> parentLayers = new List<Layer>();

        // Init rigidbody, center of mass, wheels and more
        void Start() {
#if MULTIOSCONTROLS
            Debug.Log("[ACP] Using MultiOSControls");
#endif
            if (boostClip != null) {
                boostSource.clip = boostClip;
            }

            goal = GameObject.FindGameObjectWithTag("Goal");

            //randomThrottle = UnityEngine.Random.Range(-1.0f, 1.0f);
            //randomSteering = UnityEngine.Random.Range(-25.0f, 25.0f);

            randomSteering = 0.0f;
            randomThrottle = 0.0f;

            boost = maxBoost;

            _rb = GetComponent<Rigidbody>();
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;

            if (_rb != null && centerOfMass != null)
            {
                _rb.centerOfMass = centerOfMass.localPosition;
            }

            wheels = GetComponentsInChildren<WheelCollider>();

            // Set the motor torque to a non null value because 0 means the wheels won't turn no matter what
            foreach (WheelCollider wheel in wheels)
            {
                wheel.motorTorque = 0.0001f;
            }

            lastPosition = transform.position;
            goalDistance = 0.0f;
            goalAngle = 0.0f;
        }

        // Visual feedbacks and boost regen
        void Update()
        {
            distanceTravelled += Vector3.Distance(transform.position, lastPosition);
            lastPosition = transform.position;
            foreach (ParticleSystem gasParticle in gasParticles)
            {
                gasParticle.Play();
                ParticleSystem.EmissionModule em = gasParticle.emission;
                em.rateOverTime = handbrake ? 0 : Mathf.Lerp(em.rateOverTime.constant, Mathf.Clamp(150.0f * throttle, 30.0f, 100.0f), 0.1f);
            }

            if (isPlayer && allowBoost) {
                boost += Time.deltaTime * boostRegen;
                if (boost > maxBoost) { boost = maxBoost; }
            }
            GetCarOutputsToNeural();

            CalcGameover();

            NeuralNetwork();
        }

        void LateUpdate()
        {

        }

        List<float> GetCarOutputsToNeural()
        {
            List<float> result = new List<float>();

            int maxLaserDistance = 50;

            if (goal != null)
            {
                goalDistance = Vector3.Distance(this.gameObject.transform.position, goal.transform.position);
                goalAngle = GetGoalAngle();
                if(showDebugDraw)
                    Debug.DrawRay(transform.position, transform.TransformDirection(new Vector3((float)Math.Cos((goalAngle * -1) + (float)Math.PI / 2), 0, (float)Math.Sin((goalAngle * -1) + (float)Math.PI / 2))) * 10, Color.blue);

                //Debug.Log("goalAngle " + goalAngle);
            } else
            {
                goalDistance = 0.0f;
                goalAngle = 0.0f;

                //Debug.Log("goalAngle and distances dont exist ");
            }

            Vector3 velProjected = Vector3.ProjectOnPlane(_rb.velocity, new Vector3(0, 1, 1));
            linearVel = Vector3.Dot(velProjected, transform.forward);

            angVel = _rb.angularVelocity.y;

            //Debug.Log("velocidade " + linearVel + " angular " + angVel);

            result.Add(linearVel / 34.96f);
            result.Add(angVel / 6.0f);

            //result.Add(goalDistance);
            //result.Add(goalAngle);

            result.Add(GetLaserDistToWall(maxLaserDistance, new Vector3(0, 0, 1)));
            result.Add(GetLaserDistToWall(maxLaserDistance, new Vector3(0, 0, -1)));
            result.Add(GetLaserDistToWall(maxLaserDistance, new Vector3(0.5f, 0, 0.866f)));
            result.Add(GetLaserDistToWall(maxLaserDistance, new Vector3(-0.5f, 0, 0.866f)));

            result.Add(GetLaserDistToWall(maxLaserDistance, new Vector3(0.9659f, 0, 0.2588f)));
            result.Add(GetLaserDistToWall(maxLaserDistance, new Vector3(-0.9659f, 0, 0.2588f)));

            //result.Add(GetLaserDistToWall(maxLaserDistance, new Vector3(-1, 0, 0)) / maxLaserDistance);
            //result.Add(GetLaserDistToWall(maxLaserDistance, new Vector3(1, 0, 0)) / maxLaserDistance);

            //result.Add(GetLaserDistToWall(maxLaserDistance, new Vector3(1, 0, 1).normalized) / maxLaserDistance);
            //result.Add(GetLaserDistToWall(maxLaserDistance, new Vector3(1, 0, -1).normalized) / maxLaserDistance);
            //result.Add(GetLaserDistToWall(maxLaserDistance, new Vector3(-1, 0, 1).normalized) / maxLaserDistance);
            //result.Add(GetLaserDistToWall(maxLaserDistance, new Vector3(-1, 0, -1).normalized) / maxLaserDistance);

            return result;
        }

        float GetGoalAngle ()
        {
            float goalAng = Vector3.Angle(this.gameObject.transform.position, goal.transform.position);
            Vector3 goalDir = goal.transform.position - this.gameObject.transform.position;

            goalAng = (float)Math.Acos(Vector3.Dot(goalDir.normalized, transform.forward));
            float whichWay = Vector3.Cross(transform.forward, goalDir.normalized).y;
            goalAng = (goalAng + (float)Math.PI / 2) * (Math.Sign(whichWay) * -1);

            if (goalAng < 0)
            {
                goalAng = goalAng + (float)Math.PI;
            }

            goalAng = (goalAng - (float)Math.PI / 2) * -1;

            return goalAng;
        }

        float GetLaserDistToWall(float maxDist, Vector3 direction)
        {
            // Bit shift the index of the layer (8) to get a bit mask
            int layerMask = ~ ((1 << 8) | (1 << 9));

            // This would cast rays only against colliders in layer 8.
            // But instead we want to collide against everything except layer 8. The ~ operator does this, it inverts a bitmask.

            RaycastHit hit;
            // Does the ray intersect any objects excluding the player layer

            Vector3 transformDir = Vector3.ProjectOnPlane(transform.TransformDirection(direction), new Vector3(0, 1, 0));

            if (Physics.Raycast(transform.position, transformDir, out hit, maxDist, layerMask))
            {
                //Debug.Log("Did Hit  " + hit.collider.gameObject.tag);
                if (hit.collider.CompareTag("Wall"))
                {
                    if (showDebugDraw)
                        Debug.DrawRay(transform.position, transformDir * hit.distance, Color.green);
                    return hit.distance;
                } else
                {
                    if (showDebugDraw)
                        Debug.DrawRay(transform.position, transformDir * hit.distance, Color.yellow);
                    return maxDist;
                }
            }
            else
            {
                if (showDebugDraw)
                    Debug.DrawRay(transform.position, transformDir * maxDist, Color.red);
                //Debug.Log("Did not Hit");
                return maxDist;
            }
        }

        bool CalcGameover()
        {
            if (gameover)
            {
                return gameover;
            }

            realTime = ticks;

            if (realTime > minLifeTimeSec)
            {
                if (distanceTravelled < minDistAllowed)
                {
                    setGameover(false, "distancia percorrida mto pequena");
                }

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
            if(!gameover)
            {
                if (crashedOnWall)
                {
                    hasCrashedOnWall = crashedOnWall;
                    ticksOnCrash = ticks;
                }

                gameover = true;
                gameObject.SetActive(false);
                UnityEngine.Object.Destroy(gameObject);
            }
        }

        public float CalculateScore(float highestTravelledDist, float highestGoalDistance, float highestTicksOnCrash)
        {
            float relativeTravelledDist = (highestTravelledDist - distanceTravelled + 0.001f) / highestTravelledDist;

            relativeGoalDistance = (highestGoalDistance - goalDistance + 0.001f) / highestGoalDistance;

            float currentScore = (1.0f / relativeTravelledDist);

            //if (hasCrashedOnWall)
            //{
            //    currentScore = 0;
            //}

            if (checkpointsReached > 0)
            {
                currentScore = currentScore * checkpointsReached * 20;
            }

            score = currentScore;
            return currentScore;
        }

        private void NeuralNetwork ()
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

            List<List<float>> firstLayerWeights = neuralLayers.Count > 1 ? neuralLayers[1].GetWeights() : new List<List<float>>();
            Layer secondLayer = new Layer(new List<float>(), 7, firstLayer.neurons, firstLayerWeights);
            newLayers.Add(secondLayer);

            List<List<float>> secondLayerWeights = neuralLayers.Count > 2 ? neuralLayers[2].GetWeights() : new List<List<float>>();
            Layer thirdLayer = new Layer(new List<float>(), 5, secondLayer.neurons, secondLayerWeights);
            newLayers.Add(thirdLayer);

            List<List<float>> thirdLayerWeights = neuralLayers.Count > 3 ? neuralLayers[3].GetWeights() : new List<List<float>>();
            Layer fourthLayer = new Layer(new List<float>(), 2, thirdLayer.neurons, thirdLayerWeights);
            newLayers.Add(fourthLayer);

            List<float> vels = fourthLayer.GetOutputs();

            if(vels.Count > 0)
            {
                randomThrottle = vels[0];
                randomSteering = vels[1];

                if (randomSteering > 1)
                    randomSteering = 1;

                if (randomSteering > maxSteering)
                    maxSteering = randomSteering;

                if (randomThrottle > maxThrottle)
                    maxThrottle = randomThrottle;

                randomSteering = randomSteering * 25;

                //Debug.Log("randomThrottle " + randomThrottle + " randomSteering " + randomSteering);
            }

            neuralLayers = newLayers;
        }

        public List<Neuron> GetGenome()
        {
            List<Neuron> genome = new List<Neuron>();

            foreach(Layer layer in neuralLayers)
            {
                genome.AddRange(layer.neurons.ToArray());
            }

            return genome;
        }
        
        // Update everything
        void FixedUpdate () {
            ticks = ticks + 1;
            // Mesure current speed
            speed = transform.InverseTransformDirection(_rb.velocity).z * 3.6f;

            // Get all the inputs!
            if (isPlayer) {
                // Accelerate & brake
                //if (throttleInput != "" && throttleInput != null)
                //{
                //    throttle = GetInput(throttleInput) - GetInput(brakeInput);
                //    Debug.Log("throttle " + throttle);
                //}

                throttle = randomThrottle;
                //Debug.Log("throttle " + throttle);
                // Boost
                boosting = (GetInput(boostInput) > 0.5f);
                // Turn
                //steering = turnInputCurve.Evaluate(GetInput(turnInput)) * steerAngle;
                steering = randomSteering;

                //Debug.Log("steering " + steering);
                // Dirft
                drift = GetInput(driftInput) > 0 && _rb.velocity.sqrMagnitude > 100;
                // Jump
                jumping = GetInput(jumpInput) != 0;
            }

            // Direction
            foreach (WheelCollider wheel in turnWheel)
            {
                wheel.steerAngle = Mathf.Lerp(wheel.steerAngle, steering, steerSpeed);
            }

            foreach (WheelCollider wheel in wheels)
            {
                wheel.brakeTorque = 0;
            }

            // Handbrake
            if (handbrake)
            {
                foreach (WheelCollider wheel in wheels)
                {
                    // Don't zero out this value or the wheel completly lock up
                    wheel.motorTorque = 0.0001f;
                    wheel.brakeTorque = brakeForce;
                }
            }
            else if (Mathf.Abs(speed) < 4 || Mathf.Sign(speed) == Mathf.Sign(throttle))
            {
                foreach (WheelCollider wheel in driveWheel)
                {
                    wheel.motorTorque = throttle * motorTorque.Evaluate(speed) * diffGearing / driveWheel.Length;
                }
            }
            else
            {
                foreach (WheelCollider wheel in wheels)
                {
                    wheel.brakeTorque = Mathf.Abs(throttle) * brakeForce;
                }
            }

            // Jump
            if (jumping && isPlayer) {
                if (!IsGrounded)
                    return;
                
                _rb.velocity += transform.up * jumpVel;
            }

            // Boost
            if (boosting && allowBoost && boost > 0.1f) {
                _rb.AddForce(transform.forward * boostForce);

                boost -= Time.fixedDeltaTime;
                if (boost < 0f) { boost = 0f; }

                if (boostParticles.Length > 0 && !boostParticles[0].isPlaying) {
                    foreach (ParticleSystem boostParticle in boostParticles) {
                        boostParticle.Play();
                    }
                }

                if (boostSource != null && !boostSource.isPlaying) {
                    boostSource.Play();
                }
            } else {
                if (boostParticles.Length > 0 && boostParticles[0].isPlaying) {
                    foreach (ParticleSystem boostParticle in boostParticles) {
                        boostParticle.Stop();
                    }
                }

                if (boostSource != null && boostSource.isPlaying) {
                    boostSource.Stop();
                }
            }

            // Drift
            if (drift && allowDrift) {
                Vector3 driftForce = -transform.right;
                driftForce.y = 0.0f;
                driftForce.Normalize();

                if (steering != 0)
                    driftForce *= _rb.mass * speed/7f * throttle * steering/steerAngle;
                Vector3 driftTorque = transform.up * 0.1f * steering/steerAngle;


                _rb.AddForce(driftForce * driftIntensity, ForceMode.Force);
                _rb.AddTorque(driftTorque * driftIntensity, ForceMode.VelocityChange);             
            }
            
            // Downforce
            _rb.AddForce(-transform.up * speed * downforce);
        }

        // Reposition the car to the start position
        public void ResetPos() {
            transform.position = spawnPosition;
            transform.rotation = spawnRotation;

            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }

        public void toogleHandbrake(bool h)
        {
            handbrake = h;
        }

        // MULTIOSCONTROLS is another package I'm working on ignore it I don't know if it will get a release.
#if MULTIOSCONTROLS
        private static MultiOSControls _controls;
#endif

        // Use this method if you want to use your own input manager
        private float GetInput(string input) {
#if MULTIOSCONTROLS
        return MultiOSControls.GetValue(input, playerId);
#else
        return Input.GetAxis(input);
#endif
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("Wall"))
            {
                //Debug.Log("collide wall");
                setGameover(true, "bateu na parede");
                //other.gameObject.SetActive(false);
                //Debug.Log("position: " + this.gameObject.transform.position.ToString());
            } else if (other.gameObject.CompareTag("Goal"))
            {
                //Debug.Log("collide goal");
                //other.gameObject.SetActive(false);
                this.goalReached = true;
                this.gameObject.SetActive(false);
                //Debug.Log("position: " + this.gameObject.transform.position.ToString());
            } else if (other.gameObject.CompareTag("Checkpoint"))
            {
                checkpointsReached += 1;
                //Debug.Log("collide checkpoint");
            }
            //Destroy(other.gameObject);
        }
    }
}
