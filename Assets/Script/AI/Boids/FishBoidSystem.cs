using Unity.Jobs;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;

// This script was made by following the source code
// https://github.com/BadGraphixD/How-many-Boids-can-Unity-handle
public class FishBoidSystem : JobComponentSystem
{

    private EntityQuery fishGroup;
    private FishBoidController fishController;

    // Copy all fish positions and forwards into buffer
    [BurstCompile]
    [RequireComponentTag(typeof(FishBoid))]
    [System.Obsolete]
    private struct CopyPosAndForwardsInBuffer : IJobForEachWithEntity<LocalToWorld>
    {
        public NativeArray<float3> fishPositions;
        public NativeArray<float3> fishForwards;

        public void Execute(Entity fish, int fishIndex, [ReadOnly] ref LocalToWorld localToWorld)
        {
            fishPositions[fishIndex] = localToWorld.Position;
            fishForwards[fishIndex] = localToWorld.Forward;
        }
    }

    // Assign each boid to a cell with an index that is stored in a hashMap
    [BurstCompile]
    [RequireComponentTag(typeof(FishBoid))]
    [System.Obsolete]
    private struct HashPositionsToHashMap : IJobForEachWithEntity<LocalToWorld>
    {
        public NativeMultiHashMap<int, int>.ParallelWriter hashMap;
        [ReadOnly] public quaternion cellRotatVary;
        [ReadOnly] public float3 posOffsetVary;
        [ReadOnly] public float cellRadius;

        public void Execute(Entity fish, int fishIndex, [ReadOnly] ref LocalToWorld localToWorld)
        {
            var hash = (int)math.hash(new int3(math.floor(math.mul(cellRotatVary, localToWorld.Position + posOffsetVary) / cellRadius)));
            hashMap.Add(hash, fishIndex);
        }
    }


    // Sums up positions and forward direction of all void of each cell in the hasmap
    [BurstCompile]
    [System.Obsolete]
    private struct MergeCellsJob : IJobNativeMultiHashMapMergedSharedKeyIndices
    {
        public NativeArray<int> indicesOfCells;
        public NativeArray<float3> cellPositions;
        public NativeArray<float3> cellForwards;
        public NativeArray<int> cellCount;

        public void ExecuteFirst(int firstFishIndexEncountered)
        {
            indicesOfCells[firstFishIndexEncountered] = firstFishIndexEncountered;
            cellCount[firstFishIndexEncountered] = 1;
            float3 posInThisCell = cellPositions[firstFishIndexEncountered] / cellCount[firstFishIndexEncountered];
        }

        public void ExecuteNext(int firstFishIndexAsCellKey, int fishIndexEncountered)
        {
            cellCount[firstFishIndexAsCellKey] += 1;
            cellForwards[firstFishIndexAsCellKey] += cellForwards[fishIndexEncountered];
            cellPositions[firstFishIndexAsCellKey] += cellPositions[fishIndexEncountered];
            indicesOfCells[fishIndexEncountered] = firstFishIndexAsCellKey;
        }
    }

    // Calculates the forces applied to each fish
    [BurstCompile]
    [RequireComponentTag(typeof(FishBoid))]
    [System.Obsolete]
    private struct MoveFish : IJobForEachWithEntity<LocalToWorld>
    {
        [ReadOnly] public float deltaTime;
        [ReadOnly] public float fishSpeed;

        [ReadOnly] public float separationWeight;
        [ReadOnly] public float alignmentWeight;
        [ReadOnly] public float cohesionWeight;

        [ReadOnly] public float cageWidth;
        [ReadOnly] public float cageHeight;
        [ReadOnly] public float cageBreadth;
        [ReadOnly] public float cageAvoidDist;
        [ReadOnly] public float cageAvoidWeight;

        [ReadOnly] public float cellSize;
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<int> cellIndices;
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<float3> posSumsOfCells;
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<float3> forwardSumsOfCells;
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<int> cellFishCount;

        public void Execute(Entity fish, int fishIndex, ref LocalToWorld localToWorld)
        {
            float3 fishPos = localToWorld.Position;
            int cellIndex = cellIndices[fishIndex];

            int nearbyFishCount = cellFishCount[cellIndex] - 1;
            float3 posSum = posSumsOfCells[cellIndex] - localToWorld.Position;
            float3 forwardSum = forwardSumsOfCells[cellIndex] - localToWorld.Forward;

            float3 force = float3.zero;

            if (nearbyFishCount > 0)
            {
                float3 avgPos = posSum / nearbyFishCount;

                float distToAvgPosSq = math.lengthsq(avgPos - fishPos);
                float maxDistToAvgPosSq = cellSize * cellSize;

                float distNormalized = distToAvgPosSq / maxDistToAvgPosSq;
                float needToLeave = math.max(1 - distNormalized, 0.0f);

                float3 toAvgPos = math.normalizesafe(avgPos - fishPos);
                float3 avgForward = forwardSum / nearbyFishCount;

                force += -toAvgPos * separationWeight * needToLeave;
                force += toAvgPos * cohesionWeight;
                force += avgForward * alignmentWeight;
            }

            if (math.min(math.min((cageWidth / 2.0f) - math.abs(fishPos.x),
                                 (cageHeight / 2.0f) - math.abs(fishPos.y)),
                                 (cageBreadth / 2.0f) - math.abs(fishPos.z))
                < cageAvoidDist)
            {
                force += -math.normalize(fishPos) * cageAvoidWeight;
            }

            float3 velocity = localToWorld.Forward * fishSpeed;
            velocity += force * deltaTime;
            velocity = math.normalize(velocity) * fishSpeed;

            localToWorld.Value = float4x4.TRS(
                localToWorld.Position + velocity * deltaTime,
                quaternion.LookRotationSafe(velocity, localToWorld.Up),
                new float3(1.0f));
        }
    }

    protected override void OnCreate()
    {
        fishGroup = GetEntityQuery(new EntityQueryDesc
        {
            All = new[] { ComponentType.ReadOnly<FishBoid>(), ComponentType.ReadWrite<LocalToWorld>() },
            Options = EntityQueryOptions.FilterWriteGroup
        });
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (!fishController)
            fishController = FishBoidController.Instance;
        if (fishController)
        {
            int fishCount = fishGroup.CalculateEntityCount();

            var cellIndices = new NativeArray<int>(fishCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var cellFishCount = new NativeArray<int>(fishCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var fishPositions = new NativeArray<float3>(fishCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var fishForwards = new NativeArray<float3>(fishCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var hashMap = new NativeMultiHashMap<int, int>(fishCount, Allocator.TempJob);

            var posAndForwardsCopyJob = new CopyPosAndForwardsInBuffer
            {
                fishPositions = fishPositions,
                fishForwards = fishForwards
            };
            JobHandle posAndForwardsCopyJobHandle = posAndForwardsCopyJob.Schedule(fishGroup, inputDeps);

            quaternion randomHashRot = quaternion.Euler(UnityEngine.Random.Range(-360f, 360f),
                                                        UnityEngine.Random.Range(-360f, 360f),
                                                        UnityEngine.Random.Range(-360f, 360f));

            float offsetRange = fishController.fishPerceptionRadius / 2.0f;
            float3 randHashOffset = new float3(UnityEngine.Random.Range(-offsetRange, offsetRange),
                                               UnityEngine.Random.Range(-offsetRange, offsetRange),
                                               UnityEngine.Random.Range(-offsetRange, offsetRange));

            var hashPositionsJob = new HashPositionsToHashMap
            {
                hashMap = hashMap.AsParallelWriter(),
                cellRotatVary = randomHashRot,
                posOffsetVary = randHashOffset,
                cellRadius = fishController.fishPerceptionRadius,
            };
            JobHandle hashPositionsJobHandle = hashPositionsJob.Schedule(fishGroup, inputDeps);

            // Continue when the posAndForwardsCopyJob and hashPositionsjob are complete
            JobHandle copyAndHashJobHandle = JobHandle.CombineDependencies(posAndForwardsCopyJobHandle, hashPositionsJobHandle);

            var mergeCellsJob = new MergeCellsJob
            {
                indicesOfCells = cellIndices,
                cellPositions = fishPositions,
                cellForwards = fishForwards,
                cellCount = cellFishCount,
            };
            JobHandle mergeCellsJobHandle = mergeCellsJob.Schedule(hashMap, 64, copyAndHashJobHandle);

            var moveJob = new MoveFish
            {
                deltaTime = Time.DeltaTime,
                fishSpeed = fishController.fishSpeed,

                separationWeight = fishController.separationWeight,
                alignmentWeight = fishController.alignmentWeight,
                cohesionWeight = fishController.cohesionWeight,

                cageWidth = fishController.cageWidth,
                cageHeight = fishController.cageHeight,
                cageBreadth = fishController.cageBreadth,
                cageAvoidDist = fishController.avoidWallsTurnDist,
                cageAvoidWeight = fishController.avoidWallsWeight,

                cellSize = fishController.fishPerceptionRadius,
                cellIndices = cellIndices,
                posSumsOfCells = fishPositions,
                forwardSumsOfCells = fishForwards,
                cellFishCount = cellFishCount
            };
            JobHandle moveJobHandle = moveJob.Schedule(fishGroup, mergeCellsJobHandle);
            moveJobHandle.Complete();
            hashMap.Dispose();

            inputDeps = moveJobHandle;
            fishGroup.AddDependency(inputDeps);

            return inputDeps;
        }
        else
            return inputDeps;
    }
}