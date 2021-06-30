using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIPatrol : MonoBehaviour
{
    public Rigidbody aiRB;
    public SphereCollider aiControllerCollider;
    public SphereCollider starfishCollider;

    public float forwardAccel;
    public float accelMultiplier;
    public float maxWaitTime;
    public float smoothVal = 2.0f;

    [Header("Raycast System")]
    public LayerMask floorMask;
    public float rayLength = 0.5f;
    public Transform rayPoint;

    private float minX;
    private float maxX;
    private float minZ;
    private float maxZ;

    private Vector3 patrolNode;
    private float currentWaitTime;

    // Start is called before the first frame update
    void Start()
    {
        currentWaitTime = maxWaitTime;
        GetFloorSize();
        patrolNode = GetNewPos();

        aiRB.transform.parent = null;
    }

    // Update is called once per frame
    void Update()
    {
        Physics.IgnoreCollision(aiControllerCollider, starfishCollider, true);

        transform.position = aiRB.transform.position;
        if (aiRB.transform.position.y < -45.0f)
            aiRB.transform.position = new Vector3(aiRB.transform.position.x, aiRB.transform.position.y + 5.0f, aiRB.transform.position.z);
    }

    private void FixedUpdate()
    {
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
        SetRotation();
    }

    private void GetFloorSize()
    {
        GameObject floor = GameObject.FindWithTag("Floor");
        Renderer floorSize = floor.GetComponent<Renderer>();
        minX = (floorSize.bounds.center.x - floorSize.bounds.extents.x);
        maxX = (floorSize.bounds.center.x + floorSize.bounds.extents.x);
        minZ = (floorSize.bounds.center.z - floorSize.bounds.extents.z);
        maxZ = (floorSize.bounds.center.z + floorSize.bounds.extents.z);
    }

    private Vector3 GetNewPos()
    {
        //GameObject floor = GameObject.FindWithTag("Floor");
        float randomX = Random.Range(minX, maxX);
        float randomZ = Random.Range(minZ, maxZ);
        Vector3 newPos = new Vector3(randomX, transform.position.y, randomZ);
        return newPos;
    }

    private void Move()
    {
        Vector3 moveDir = (patrolNode - transform.position).normalized;

        Vector3 desiredVelocity = moveDir * forwardAccel * accelMultiplier;

        aiRB.AddForce(desiredVelocity);

        if (Vector3.Distance(transform.position, patrolNode) <= 10.0f)
        {
            if (currentWaitTime <= 0)
            {
                patrolNode = GetNewPos();
                currentWaitTime = maxWaitTime;
            }
            else
                currentWaitTime -= Time.deltaTime;
        }
    }

    private void SetRotation()
    {
        Vector3 targetDir = patrolNode - transform.position;
        Vector3 newDir = Vector3.RotateTowards(transform.forward, targetDir, 0.3f, 0.0f);
        transform.rotation = Quaternion.LookRotation(newDir);
    }
}
