using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamBobbing : MonoBehaviour
{
    public float maxYPos = 15;
    public float minYPos = 10;

    public float movementSpeed = 3.0f;

    // Update is called once per frame
    void Update()
    {
        if (transform.position.y >= maxYPos)
            movementSpeed = -3.0f;
        if (transform.position.y <= minYPos)
            movementSpeed = 3.0f;

        transform.position = new Vector3(transform.position.x, transform.position.y + movementSpeed * Time.deltaTime, transform.position.z);       
    }
}
