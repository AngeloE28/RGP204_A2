using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoralSpawner : MonoBehaviour
{
    public GameObject[] corals;
    // Gets the path
    public Transform path;
    private List<Transform> pathNodes;
    private int currentNode = 0;
    public float smoothVal = 15.0f;
    public float speed;

    public float maxHealth;
    public float currentHealth;

    [Header("Raycast System")]
    public LayerMask floorMask;
    public float rayLength = 0.5f;
    public Transform rayPoint;

    // Start is called before the first frame update
    void Start()
    {
        currentHealth = maxHealth;
        Transform[] pathTransforms = path.GetComponentsInChildren<Transform>();
        pathNodes = new List<Transform>();

        // Adds the waypoints to the pathNodes
        for(int i = 0; i< pathTransforms.Length; i++)
        {
            if (pathTransforms[i] != path.transform)
                pathNodes.Add(pathTransforms[i]);
        }
    }

    public void FishTakeDamage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
            Invoke("SpawnCoral", 0.5f);
    }

    private void SpawnCoral()
    {
        // Disable the game object
        gameObject.SetActive(false);

        // Once the fish is destroyed spawn the corals
        foreach(GameObject c in corals)
        {
            c.SetActive(true);
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // Change its rotation according to ground
        RaycastHit hit;

        // Check if the ray is colliding with any object with the Ground layermask
        if (Physics.Raycast(rayPoint.position, -transform.up, out hit, rayLength, floorMask))
        {
            // Rotate the starfish when flying up
            Quaternion targetRotation;

            // Get target rotation
            targetRotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;

            // Smooth out rotation
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * smoothVal);
        }

        Move();
        CheckWayPointDistance();
    }

    private void Move()
    {
        if(pathNodes != null)
        {
            // Get the co-ordinates of the current node in the path list
            Vector3 targetPos = pathNodes[currentNode].position;

            // Move towards the node
            Vector3 moveDir = (targetPos - transform.position).normalized;

            Vector3 desiredVelocity = transform.position + moveDir * speed * Time.deltaTime;

            transform.position = desiredVelocity;
        }
    }



    private void CheckWayPointDistance()
    {
        // Check if starfish has made it to the waypoint and loop through each waypoint
        if (Vector3.Distance(transform.position, pathNodes[currentNode].position) < 5.0f)
        {
            // Check if its the last waypoint
            if (currentNode == pathNodes.Count - 1)
                currentNode = 0;
            else
                currentNode++;
        }
    }
}
