using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarController : MonoBehaviour
{
    public float speed;

    private Rigidbody rb;

    void Start()
    {
        Debug.Log("start");
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(moveHorizontal, 0.0f, moveVertical);

        //rb.AddForce(movement * speed);
        rb.MovePosition( rb.position + movement * speed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Wall"))
        {
            Debug.Log("collide wall");
            //other.gameObject.SetActive(false);
            this.gameObject.SetActive(false);
            Debug.Log("position: " + this.gameObject.transform.position.ToString());
        }
        //Destroy(other.gameObject);
    }
}
