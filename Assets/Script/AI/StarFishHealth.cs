using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StarFishHealth : MonoBehaviour
{
    public float maxHealth;
    private float currentHealth;

    // Start is called before the first frame update
    void Start()
    {
        currentHealth = maxHealth;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Coral")
            Destroy(other.gameObject);
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
    }
  
}
