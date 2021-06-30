using UnityEngine;
using System.Collections;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Collections;
using Unity.Mathematics;

// This script was made by following the source code
// https://github.com/BadGraphixD/How-many-Boids-can-Unity-handle

public class FishBoidController : MonoBehaviour
{
    public static FishBoidController Instance;

    [Header("Fish Boids")]
    [SerializeField] private int fishAmount;
    [SerializeField] private Mesh fishMesh;
    [SerializeField] private Material fishMat;

    public float fishSpeed;
    public float fishSpeedMin;
    public float fishSpeedMax;
    public float fishPerceptionRadius;
    public float cageWidth;
    public float cageHeight;
    public float cageBreadth;

    public float separationWeight;
    public float cohesionWeight;
    public float alignmentWeight;

    public float avoidWallsWeight;
    public float avoidWallsTurnDist;
    public int maxSpawnCycle;
    private int spawnCycleCounter;

    IEnumerator spawner;

    [Header("Timers")]
    public float spawnTimer = 0.0f;
    public float spawnMaxWait = 6.0f;
    public float spawnMinWait = 4.0f;
    public float startSpawnerTimer = 0.0f;

    private void Awake()
    {
        Instance = this;
        spawner = SpawnFish();
        StartCoroutine(spawner);
    }

    private void LateUpdate()
    {
        spawnTimer = UnityEngine.Random.Range(spawnMinWait, spawnMaxWait);
        fishSpeed = UnityEngine.Random.Range(fishSpeedMin, fishSpeedMax);
        if (spawnCycleCounter > maxSpawnCycle)
            StopCoroutine(spawner);
    }

    IEnumerator SpawnFish()
    {
        yield return new WaitForSeconds(startSpawnerTimer);

        while (true)
        {
            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            EntityArchetype fishArchetype = entityManager.CreateArchetype(typeof(FishBoid), typeof(RenderMesh),
                                                                          typeof(RenderBounds), typeof(LocalToWorld));

            NativeArray<Entity> fishArray = new NativeArray<Entity>(fishAmount, Allocator.Temp);
            entityManager.CreateEntity(fishArchetype, fishArray);

            for (int i = 0; i < fishArray.Length; i++)
            {
                entityManager.SetComponentData(fishArray[i], new LocalToWorld
                {
                    Value = float4x4.TRS(
                        RandPos(),
                        RandRot(),
                        new float3(1.0f))
                });
                entityManager.SetSharedComponentData(fishArray[i], new RenderMesh
                {
                    mesh = fishMesh,
                    material = fishMat,
                });
            }

            fishArray.Dispose();

            yield return new WaitForSeconds(spawnTimer);
            spawnCycleCounter++;
        }
    }

    private float3 RandPos()
    {
        return new float3(
            UnityEngine.Random.Range(-cageWidth / 2f, cageWidth / 2f),
            UnityEngine.Random.Range(-cageHeight / 2f, cageHeight / 2f),
            UnityEngine.Random.Range(-cageBreadth / 2f, cageBreadth / 2f)
        );
    }
    private quaternion RandRot()
    {
        return quaternion.Euler(
            UnityEngine.Random.Range(-360f, 360f),
            UnityEngine.Random.Range(-360f, 360f),
            UnityEngine.Random.Range(-360f, 360f)
        );
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(
            Vector3.zero,
            new Vector3(cageWidth, cageHeight, cageBreadth));
    }
}
