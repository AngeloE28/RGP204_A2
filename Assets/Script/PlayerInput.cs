using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    public Transform cam;

    public float forward, up, right;

    Quaternion rotation;

    Rigidbody body;

    float xRotation = 0, yRotation = 0;
    float mouseSensitivity = 100f;

    //float speed = 0;

    // Start is called before the first frame update
    void Start()
    {
        body = GetComponent<Rigidbody>();

        //Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;


        xRotation -= mouseY;
        yRotation += mouseX;

        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        rotation = Quaternion.Euler(xRotation, yRotation, 0);


        cam.transform.position = transform.position;
        cam.transform.rotation = rotation;
        transform.rotation = Quaternion.Euler(0, yRotation, 0);

        Move();
    }

    void Move()
    {
        bool isForward = false;
        bool isUp = false;
        bool isRight = false;

        float acc = 0.05f;
        float maxSpeed = 10;

        if (Input.GetKey(KeyCode.W))
        {
            forward += acc;
            isForward = isForward ? false : true;
        }
        if (Input.GetKey(KeyCode.S))
        {
            forward -= acc;
            isForward = isForward ? false : true;
        }

        if (Input.GetKey(KeyCode.A))
        {
            right -= acc;
            isRight = isRight ? false : true;
        }
        if (Input.GetKey(KeyCode.D))
        {
            right += acc;
            isRight = isRight ? false : true;
        }
        if (Input.GetKey(KeyCode.Space))
        {
            up += acc;
            isUp = isUp ? false : true;
        }
        if (Input.GetKey(KeyCode.LeftShift))
        {
            up -= acc;
            isUp = isUp ? false : true;
        }


        forward = Mathf.Clamp(forward, -maxSpeed, maxSpeed);
        right = Mathf.Clamp(right, -maxSpeed, maxSpeed);
        up = Mathf.Clamp(up, -maxSpeed, maxSpeed);

        if (!isForward)
        {
            if (forward > acc)
                forward -= acc;
            else if (forward < acc)
                forward += acc;
            else
                forward = 0;
        }
        if (!isRight)
        {
            if (right > acc)
                right -= acc;
            else if (right < acc)
                right += acc;
            else
                right = 0;
        }
        if (!isUp)
        {
            if (up > acc)
                up -= acc;
            else if (up < acc)
                up += acc;
            else
                up = 0;
        }

        Vector3 vel = new Vector3();
        vel += cam.forward * forward;
        vel += cam.right * right;
        vel += new Vector3(0, up, 0);

        body.velocity = vel;
    }
}
