using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public Rigidbody bulletRb;
    public float damage;
    public Vector3 hitPoint;
    public float shootForce;

    // Start is called before the first frame update
    void Start()
    {
        bulletRb.AddForce((hitPoint - this.transform.position).normalized * shootForce, ForceMode.Impulse);
    }

    private void OnTriggerEnter(Collider other)
    {    
        if(other.tag == "Starfish")
        {
            other.GetComponent<CoralSpawner>().FishTakeDamage(damage);
        }
    }
}
