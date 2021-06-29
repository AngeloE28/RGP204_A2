using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunProjectile : MonoBehaviour
{
    public GameObject bullet; // The bullet to be instantiated
    public Transform attackPoint;
    public Transform bulletCollection;
    public Camera playerCam;
    public LayerMask floorMask;
    public LayerMask enemyMask;

    [Header("Gun Statistics")]
    public float timeBetweenShots;
    public float fireRate;
    public bool canSpray = false;
    public bool canBurstShot;
    public int bulletsPerClick;
    public float bulletSpread;

    private bool shooting;
    private bool readyToShoot;
    private int bulletsShot;

    private bool allowInvoke = true;

    // Start is called before the first frame update
    void Start()
    {
        readyToShoot = true;
    }

    // Update is called once per frame
    void Update()
    {
        PlayerInput();
    }

    private void PlayerInput()
    {
        // Player can hold left click to spray bullets
        if (canSpray)
            shooting = Input.GetKey(KeyCode.Mouse0);
        else // Player taps to shoot (Default value is tapping)
            shooting = Input.GetKeyDown(KeyCode.Mouse0);

        if(readyToShoot && shooting)
        {

            bulletsShot = 0;
            if (!canBurstShot)
            {
                Shoot();
                //Debug.Log("Test 1");
            }
            else
            {
                BurstShot();
                //Debug.Log("Test 2");
            }
        }
    }

    private void Shoot()
    {
        readyToShoot = false;

        // Ray going through the middle of the screen
        Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0.0f));
        RaycastHit hit;

        // Check if it hits anything
        Vector3 targetPoint;
        if (Physics.Raycast(transform.position, transform.forward, out hit, Mathf.Infinity, floorMask))        
            targetPoint = hit.point;        
        else if (Physics.Raycast(transform.position, transform.forward, out hit, Mathf.Infinity, enemyMask))
            targetPoint = hit.point;
        else
            targetPoint = ray.GetPoint(100);

        // Spawn the bullet
        GameObject currentBullet = Instantiate(bullet, attackPoint.position, playerCam.transform.rotation);
        currentBullet.GetComponentInChildren<Bullet>().hitPoint = targetPoint;

        // Destroy bullet after 2 sec
        Destroy(currentBullet.gameObject, 2.0f);

        // Count how many bullets were shot
        bulletsShot++;

        if (allowInvoke)
        {
            Invoke("ResetShot", timeBetweenShots);
            allowInvoke = false;
        }
    }

    private void BurstShot()
    {
        readyToShoot = false;

        // Ray going through the middle of the screen
        Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0.0f));
        RaycastHit hit;

        // Rotation that bullet will be spawned
        Quaternion fireRotation = Quaternion.LookRotation(transform.forward);

        // Add spread for burst option for gun

        Vector3 target;
        if (Physics.Raycast(transform.position, transform.forward, out hit, Mathf.Infinity, floorMask)) {
            // For some reason this shoots out in funky direction
            //target = hit.point;
            target = ray.GetPoint(100);
        }
        else if (Physics.Raycast(transform.position, transform.forward, out hit, Mathf.Infinity, enemyMask))
            target = hit.point;
        else
        {
            target = ray.GetPoint(100);
        }

        // Calculate the bullet spread
        float xDir = Random.Range(-bulletSpread, bulletSpread);
        float yDir = Random.Range(-bulletSpread, bulletSpread);

        Vector3 spread = new Vector3(xDir, yDir, 0.0f) + target;

        // Spawn the bullet
        GameObject currentBullet = Instantiate(bullet, attackPoint.position, playerCam.transform.rotation);
        currentBullet.GetComponentInChildren<Bullet>().hitPoint = spread;
        currentBullet.transform.SetParent(bulletCollection);

        // Destroy bullet after 2 sec
        Destroy(currentBullet.gameObject, 2.0f);

        // Count how many bullets were shot
        bulletsShot++;

        if (allowInvoke)
        {
            Invoke("ResetShot", timeBetweenShots);
            allowInvoke = false;
        }

        // Repeats for the rest of the bullets per click
        if (bulletsShot < bulletsPerClick)
            Invoke("BurstShot", fireRate);
    }

    private void ResetShot()
    {
        // Resets variables back to true
        readyToShoot = true;
        allowInvoke = true;
    }
}
