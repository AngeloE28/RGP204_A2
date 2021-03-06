using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeaweedSpawner : MonoBehaviour
{
    public GameObject floor;
    public LayerMask floorMask;
    public GameManager gameManager;

    [Header("Spawn Values")]
    public GameObject[] prefab;
    public GameObject[] prefabCounter;
    public int prefabNumberPerSpawn = 10;
    public int maxNumberofPrefabs = 1250;
    public int minNumberofPrefabs = 50;
    public bool isStarfish = false;
    public string gameObjectTag;

    [Header("Timers")]
    public float spawnTimer = 0.0f;
    public float spawnMaxWait = 6.0f;
    public float spawnMinWait = 4.0f;
    public float startSpawnerTimer = 0.0f;
    
    private bool stop = false;
    private bool restart = true;
    private float radius;
    private IEnumerator spawn;


    // Start is called before the first frame update
    void Awake()
    {
        GetFloorSize();
        spawn = SpawnPrefab();
        StartCoroutine(spawn);
        prefabCounter = GameObject.FindGameObjectsWithTag(gameObjectTag);
    }

    private void Update()
    {      
        prefabCounter = GameObject.FindGameObjectsWithTag(gameObjectTag);
        if (!gameManager.isGameRunning)
            StopAllCoroutines();
    }

    // Update is called once per frame
    void LateUpdate()
    {

        spawnTimer = Random.Range(spawnMinWait, spawnMaxWait);
        
        if(!stop && restart)
        {
            StartCoroutine(spawn);
            stop = true;
            restart = false;
        }

        if (prefabCounter.Length < maxNumberofPrefabs)
            stop = false;
        else if (prefabCounter.Length > maxNumberofPrefabs)
        {
            stop = true;
            StopCoroutine(spawn);
        }

        if (prefabCounter.Length < minNumberofPrefabs)
            restart = true;
    }

    private void GetFloorSize()
    {
        MeshCollider floorSize = floor.GetComponent<MeshCollider>();
        radius = floorSize.bounds.max.magnitude; 
    }

    IEnumerator SpawnPrefab()
    {
        yield return new WaitForSeconds(startSpawnerTimer);

        while (true)
        {
            for (int i = 0; i < prefabNumberPerSpawn; i++)
            {
                GameObject obj = prefab[Random.Range(0, prefab.Length)];

                Vector2 spawnPosV2 = Random.insideUnitCircle * radius;

                Vector3 spawnPos = new Vector3(spawnPosV2.x, 6.0f, spawnPosV2.y);

                Vector3 transformOffsetSpawnPos = transform.position + spawnPos;

                RaycastHit hit;
                if (Physics.Raycast(transformOffsetSpawnPos, Vector3.down, out hit, 100f, floorMask))
                {
                    if (isStarfish)
                    {
                        Vector3 semiFinalSpawnPos = hit.point;

                        Vector3 finalSpawnPos = new Vector3(semiFinalSpawnPos.x, semiFinalSpawnPos.y + 2.0f, semiFinalSpawnPos.z);

                        GameObject newObject = Instantiate(obj, finalSpawnPos, Quaternion.identity);
                        newObject.transform.SetParent(this.gameObject.transform);  
                    }
                    else
                    {
                        Vector3 finalSpawnPos = hit.point;

                        GameObject newObject = Instantiate(obj, finalSpawnPos, Quaternion.identity);
                        newObject.transform.SetParent(this.gameObject.transform);
                    }

                }
            }

            yield return new WaitForSeconds(spawnTimer);
        }
    }
}
