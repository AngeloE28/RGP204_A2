using UnityEngine;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Collections;
using Unity.Mathematics;

// This Script was made by following the scripts made by BadGraphixD
// Source Project https://github.com/BadGraphixD/How-many-Boids-can-Unity-handle

public class FishBoidController : MonoBehaviour
{
    // Create an instance of the controller
    public static FishBoidController Instance;

    public int fishAmount;
    public Mesh fishMesh;
    public Material fishMaterial;

    public float speed;
    public float fishPerceptionRadius;
    public float boundarySize;

    public float seperationWeight;
    public float cohesionWeight;
    public float alignmentWeight;

    public float avoidBoundaryWeight;
    public float avoidBoundaryTurnDist;

    // Start is called before the first frame update
    void Start()
    {
        Instance = this;

        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        EntityArchetype fishArchetype = entityManager.CreateArchetype(
            typeof(FishBoid),
            typeof(RenderMesh),
            typeof(RenderBounds),
            typeof(LocalToWorld));

        NativeArray<Entity> fishArray = new NativeArray<Entity>(fishAmount, Allocator.Temp);
        entityManager.CreateEntity(fishArchetype, fishArray);

        for (int i = 0; i < fishArray.Length; i++)
        {
            Unity.Mathematics.Random random = new Unity.Mathematics.Random((uint)i + 1);
            entityManager.SetComponentData(fishArray[i], new LocalToWorld
            {
                Value = float4x4.TRS(RandPos(), RandRot(), new float3(15.0f))
            });

            entityManager.SetSharedComponentData(fishArray[i], new RenderMesh
            {
                mesh = fishMesh,
                material = fishMaterial
            });
        }

        fishArray.Dispose();
    }
    
    private float3 RandPos()
    {
        return new float3(UnityEngine.Random.Range(-boundarySize / 2.0f, boundarySize / 2.0f),
                          UnityEngine.Random.Range(-boundarySize / 2.0f, boundarySize / 2.0f),
                          UnityEngine.Random.Range(-boundarySize / 2.0f, boundarySize / 2.0f));
    }

    private quaternion RandRot()
    {
        return quaternion.Euler(UnityEngine.Random.Range(-360.0f, 360.0f),
                                UnityEngine.Random.Range(-360.0f, 360.0f),
                                UnityEngine.Random.Range(-360.0f, 360.0f));
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(
            Vector3.zero,
            new Vector3(boundarySize, boundarySize, boundarySize));
    }
}