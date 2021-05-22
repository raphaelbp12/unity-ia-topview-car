using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CinematicUnicycleController : MonoBehaviour
{
    public NeuralNetwork parentNeuralNetwork;
    public float linearVelocity = 0f;
    public float angularVelocity = 0f;

    private Rigidbody rb;

    [SerializeField] float speed = 0.0f;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void FixedUpdate()
    {
        speed = transform.InverseTransformDirection(rb.velocity).z * 3.6f;

        rb.velocity = rb.rotation * new Vector3(0f, 0f, linearVelocity * 20.0f);
        rb.angularVelocity = new Vector3(0f, angularVelocity * 40.0f, 0f);
    }

    public void DestroyMyself()
    {
        UnityEngine.Object.Destroy(rb);
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
