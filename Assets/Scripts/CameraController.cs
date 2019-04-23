using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public GameObject car;

    private Vector3 offset;
    // Start is called before the first frame update
    void Start()
    {
        offset = transform.position - car.transform.position;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        transform.position = car.transform.position + new Vector3(0, 30, 0);
    }

    public void SetCar(GameObject newCar)
    {
        car = newCar;
    }
}
