using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControllerDestory : MonoBehaviour
{
    public StarFishHealth starfish;
    
    // Update is called once per frame
    void Update()
    {
        if (starfish == null)
            Destroy(this.gameObject);
    }
}
