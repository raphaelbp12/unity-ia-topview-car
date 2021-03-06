﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnicycleController : MonoBehaviour
{
    public NeuralNetwork parentNeuralNetwork;

    [SerializeField] WheelCollider leftWheel;
    [SerializeField] WheelCollider rightWheel;
    [SerializeField] Transform centerOfMass;

    [SerializeField] float speed = 0.0f;
    public float Speed { get{ return speed; } }

    [Range(0.5f, 10f)]
    [SerializeField] float downforce = 1.0f;

    [SerializeField] public float throttle = 0.0f;
    [SerializeField] public float steering = 0.0f;

    [Range(2, 10000)]
    [SerializeField] float diffGearing = 2000.0f;

    Rigidbody _rb;

    // Start is called before the first frame update
    void Start()
    {
        _rb = GetComponent<Rigidbody>();

        if (_rb != null && centerOfMass != null)
        {
            _rb.centerOfMass = centerOfMass.localPosition;
        }

        if (leftWheel != null && rightWheel != null) {
            leftWheel.motorTorque = 0.0001f;
            rightWheel.motorTorque = 0.0001f;
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        speed = transform.InverseTransformDirection(_rb.velocity).z * 3.6f;

        //throttle = Input.GetAxis("Vertical");
        //steering = Input.GetAxis("Horizontal");

        leftWheel.brakeTorque = 0;
        rightWheel.brakeTorque = 0;

        throttle = throttle * 10;
        steering = steering * 1;

        float throttleLeft = (throttle + steering) / 2;
        float throttleRight = (throttle - steering) / 2;
        
        leftWheel.motorTorque = throttleLeft * diffGearing;
        rightWheel.motorTorque = throttleRight * diffGearing;

        // _rb.AddForce(-transform.up * speed * downforce);
    }

    public void DestroyMyself()
    {
        UnityEngine.Object.Destroy(_rb);
        UnityEngine.Object.Destroy(gameObject);
    }

    void OnTriggerEnter(Collider aCol)
    {
        if (parentNeuralNetwork != null)
        {
            parentNeuralNetwork.OnChildTriggerEnter(aCol);
        }
    }
}
