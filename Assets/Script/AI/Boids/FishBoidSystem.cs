using Unity.Jobs;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;

// This Script was made by following the scripts made by BadGraphixD
// Source Project https://github.com/BadGraphixD/How-many-Boids-can-Unity-handle

public class FishBoidSystem : JobComponentSystem
{
    private EntityQuery fishGroup;
    private FishBoidController fishController;

    // Copies all fish boid positions and headings(forward) into buffer
    [BurstCompile]
    [RequireComponentTag(typeof(FishBoid))]
    [System.Obsolete]
    private struct CopyPosAndHeadingsInBuffer : IJobForEachWithEntity<LocalToWorld>
    {
        public NativeArray<float3> fishPos;
        public NativeArray<float3> fishForwards;

        public void Execute(Entity fish, int fishIndex, [ReadOnly] ref LocalToWorld localToWorld)
        {
            fishPos[fishIndex] = localToWorld.Position;
            fishForwards[fishIndex] = localToWorld.Forward;
        }
    }


    // Each fish is placed in a cell and each fishIndex is stored in a hashMap and each hash corresponds to a cell
    [BurstCompile]
    [RequireComponentTag(typeof(FishBoid))]
    [System.Obsolete]
    private struct HashPosToHashMap : IJobForEachWithEntity<LocalToWorld>
    {
        public NativeMultiHashMap<int, int>.ParallelWriter hashMap;
        [ReadOnly] public quaternion cellRotVary;
        [ReadOnly] public float3 posOffsetVary;
        [ReadOnly] public float cellRadius;

        public void Execute(Entity fish, int fishIndex, [ReadOnly] ref LocalToWorld localToWorld)
        {
            var hash = (int)math.hash(new int3(math.floor(math.mul(cellRotVary, localToWorld.Position + posOffsetVary) / cellRadius)));
            hashMap.Add(hash, fishIndex);
        }
    }

    // Sums up positions and headings(forward) of all fish of each cell.
    [BurstCompile]
    [System.Obsolete]
    private struct MergeCellsJob : IJobNativeMultiHashMapMergedSharedKeyIndices
    {
        public NativeArray<int> indicesOfCells;
        public NativeArray<float3> cellPos;
        public NativeArray<float3> cellForwards;
        public NativeArray<int> cellCount;

        public void ExecuteFirst(int firstFishIndexEncountered)
        {
            indicesOfCells[firstFishIndexEncountered] = firstFishIndexEncountered;
            cellCount[firstFishIndexEncountered] = 1;
            float3 posInThisCell = cellPos[firstFishIndexEncountered] / cellCount[firstFishIndexEncountered];
        }

        public void ExecuteNext(int firstFishIndexAsCellKey, int fishIndexEncountered)
        {
            cellCount[firstFishIndexAsCellKey] += 1;
            cellForwards[firstFishIndexAsCellKey] += cellForwards[fishIndexEncountered];
            cellPos[firstFishIndexAsCellKey] += cellPos[fishIndexEncountered];
            indicesOfCells[fishIndexEncountered] = firstFishIndexAsCellKey;
        }
    }

    // Calculates the forces for each fish
    [BurstCompile]
    [RequireComponentTag(typeof(FishBoid))]
    [System.Obsolete]
    private struct MoveFishes : IJobForEachWithEntity<LocalToWorld>
    {
        [ReadOnly] public float deltaTime;
        [ReadOnly] public float fishSpeed;

        [ReadOnly] public float separationWeight;
        [ReadOnly] public float alignmentWeight;
        [ReadOnly] public float cohesionWeight;

        [ReadOnly] public float boundarySize;
        [ReadOnly] public float boundaryAvoidDist;
        [ReadOnly] public float boundaryAvoidWeight;

        [ReadOnly] public float cellSize;
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<int> cellIndices;
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<float3> posSumsOfCells;
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<float3> forwardSumsOfCells;
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<int> cellFishCount;

        public void Execute(Entity fish, int fishIndex, ref LocalToWorld localToWorld)
        {
            float3 fishPos = localToWorld.Position;
            int cellIndex = cellIndices[fishIndex];

            int nearbyFishCount = cellFishCount[fishIndex] - 1;
            float3 posSum = posSumsOfCells[fishIndex] - localToWorld.Position;
            float3 forwardSum = forwardSumsOfCells[fishIndex] - localToWorld.Position;

            float3 force = float3.zero;

            if (nearbyFishCount > 0)
            {
                float3 averagePos = posSum / nearbyFishCount;

                float distToAveragePosSq = math.lengthsq(averagePos - fishPos);
                float maxDistToAveragePosSq = cellSize * cellSize;

                float distNoramlized = distToAveragePosSq / maxDistToAveragePosSq;
                float needToLeave = math.max(1 - distNoramlized, 0.0f);

                float3 toAveragePos = math.normalizesafe(averagePos - fishPos);
                float3 averageForward = forwardSum / nearbyFishCount;

                force += -toAveragePos * separationWeight * needToLeave;
                force += toAveragePos * cohesionWeight;
                force += averageForward * alignmentWeight;
            }

            if (math.min(math.min(
                (boundarySize / 2f) - math.abs(fishPos.x),
                (boundarySize / 2f) - math.abs(fishPos.y)),
                (boundarySize / 2f) - math.abs(fishPos.z)) < boundaryAvoidDist)
                force += -math.normalize(fishPos) * boundaryAvoidWeight;


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
            All = new[]
            {
                ComponentType.ReadOnly<FishBoid>(), ComponentType.ReadWrite<LocalToWorld>()
            },
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
            var fishPos = new NativeArray<float3>(fishCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var fishForwards = new NativeArray<float3>(fishCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var hashMap = new NativeMultiHashMap<int, int>(fishCount, Allocator.TempJob);

            var posAndForwardsCopyJob = new CopyPosAndHeadingsInBuffer
            {
                fishPos = fishPos,
                fishForwards = fishForwards
            };
            JobHandle posAndForwardsCopyJobHandle = posAndForwardsCopyJob.Schedule(fishGroup, inputDeps);

            quaternion randHashRot = quaternion.Euler(UnityEngine.Random.Range(-360.0f, 360.0f),
                                                      UnityEngine.Random.Range(-360.0f, 360.0f),
                                                      UnityEngine.Random.Range(-360.0f, 360.0f));

            float offsetRange = fishController.fishPerceptionRadius / 2.0f;
            float3 randHashOffset = new float3(UnityEngine.Random.Range(-offsetRange, offsetRange),
                                               UnityEngine.Random.Range(-offsetRange, offsetRange),
                                               UnityEngine.Random.Range(-offsetRange, offsetRange));

            var hashPosJob = new HashPosToHashMap
            {
                hashMap = hashMap.AsParallelWriter(),
                cellRotVary = randHashRot,
                posOffsetVary = randHashOffset,
                cellRadius = fishController.fishPerceptionRadius
            };
            JobHandle hashPosJobHandle = hashPosJob.Schedule(fishGroup, inputDeps);

            // Proceed when the posAndForwardsCopyJob and hasPosJob jobs are finished
            JobHandle copyAndHashJobHandle = JobHandle.CombineDependencies
            (
                posAndForwardsCopyJobHandle,
                hashPosJobHandle
            );

            var mergeCellsJob = new MergeCellsJob
            {
                indicesOfCells = cellIndices,
                cellPos = fishPos,
                cellForwards = fishForwards,
                cellCount = cellFishCount
            };
            JobHandle mergeCellsJobHandle = mergeCellsJob.Schedule(hashMap, 64, copyAndHashJobHandle);

            var moveJob = new MoveFishes
            {
                deltaTime = Time.DeltaTime,
                fishSpeed = fishController.speed,

                separationWeight = fishController.seperationWeight,
                alignmentWeight = fishController.alignmentWeight,
                cohesionWeight = fishController.cohesionWeight,

                boundarySize = fishController.boundarySize,
                boundaryAvoidDist = fishController.avoidBoundaryTurnDist,
                boundaryAvoidWeight = fishController.avoidBoundaryWeight,

                cellSize = fishController.fishPerceptionRadius,
                cellIndices = cellIndices,
                posSumsOfCells = fishPos,
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